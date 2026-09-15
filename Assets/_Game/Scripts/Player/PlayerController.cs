using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadDawn.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Stats")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float sprintMultiplier = 1.4f;
        [SerializeField] private float gravity = -20f;

        [Header("Aiming Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform weaponPivot;

        [Header("Movement Restriction Boundary")]
        [SerializeField] private bool restrictToBoundary = true;
        [SerializeField] private Vector2 boundaryX = new Vector2(-38f, 38f);
        [SerializeField] private Vector2 boundaryZ = new Vector2(-38f, 38f);

        [Header("Screen Touch Drag Settings")]
        [SerializeField] private float screenDeadzonePixels = 12f;
        [SerializeField] private float screenMaxDragPixels = 75f;

        private CharacterController controller;
        private Camera mainCamera;
        private Vector2 moveInput;
        private bool isSprinting;
        private Vector3 verticalVelocity;

        // Screen-space floating touch anchor for LEFT side movement
        private bool isTouching = false;
        private int activeTouchFingerId = -1;
        private Vector2 touchStartScreenPos;
        private Vector2 currentTouchScreenPos;

        public Transform WeaponPivot => weaponPivot;
        public Vector2 BoundaryX => boundaryX;
        public Vector2 BoundaryZ => boundaryZ;
        public bool RestrictToBoundary => restrictToBoundary;

        public event System.Action<Vector2, Vector2> OnBoundaryChanged;

        public void ExpandBoundary(float amount)
        {
            boundaryX.x -= amount;
            boundaryX.y += amount;
            boundaryZ.x -= amount;
            boundaryZ.y += amount;
            OnBoundaryChanged?.Invoke(boundaryX, boundaryZ);
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground", "Default");
            }
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public void OnSprint(InputValue value)
        {
            isSprinting = value.isPressed;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen) return;

            PollKeyboardInput();
            PollScreenTouchMovement();
            HandleMovement();
            EnforceBoundary();
        }

        private void PollKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;

                Vector2 directInput = new Vector2(x, y);
                if (directInput.sqrMagnitude > 0.01f)
                {
                    moveInput = directInput.normalized;
                }
                else if (!isTouching)
                {
                    if (Gamepad.current == null || Gamepad.current.leftStick.ReadValue().sqrMagnitude < 0.01f)
                    {
                        moveInput = Vector2.zero;
                    }
                }

                isSprinting = keyboard.leftShiftKey.isPressed;
            }
        }

        /// <summary>
        /// Dedicated touch tracker on the LEFT side of the screen (0% to 50% screen width).
        /// Right side is reserved for Tap-To-Shoot!
        /// </summary>
        private void PollScreenTouchMovement()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                if (isTouching)
                {
                    // Check if current tracked touch is still active
                    bool found = false;
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var touch = touchscreen.touches[i];
                        if (touch.touchId.ReadValue() == activeTouchFingerId && touch.press.isPressed)
                        {
                            currentTouchScreenPos = touch.position.ReadValue();
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        isTouching = false;
                        activeTouchFingerId = -1;
                    }
                }
                else
                {
                    // Look for new touch starting on the LEFT half of the screen
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var touch = touchscreen.touches[i];
                        if (touch.press.wasPressedThisFrame)
                        {
                            Vector2 pos = touch.position.ReadValue();
                            if (pos.x < Screen.width * 0.5f)
                            {
                                activeTouchFingerId = touch.touchId.ReadValue();
                                touchStartScreenPos = pos;
                                currentTouchScreenPos = pos;
                                isTouching = true;
                                break;
                            }
                        }
                    }
                }
            }
            // PC Mouse fallback drag on left half of screen
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (!isTouching && mousePos.x < Screen.width * 0.5f)
                {
                    touchStartScreenPos = mousePos;
                    currentTouchScreenPos = mousePos;
                    isTouching = true;
                }
                else if (isTouching)
                {
                    currentTouchScreenPos = mousePos;
                }
            }
            else
            {
                isTouching = false;
            }
        }

        private void HandleMovement()
        {
            Vector3 finalMoveVector = Vector3.zero;
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

            // 1. WASD / Gamepad
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 rawDirection = new Vector3(moveInput.x, 0f, moveInput.y);
                Vector3 isoDirection = Quaternion.Euler(0f, 45f, 0f) * rawDirection;
                finalMoveVector = isoDirection.normalized * (currentSpeed * moveInput.magnitude);

                Quaternion targetRot = Quaternion.LookRotation(isoDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 20f * Time.deltaTime);
            }
            // 2. Left-hand Mobile Touch Drag
            else if (isTouching)
            {
                Vector2 screenDelta = currentTouchScreenPos - touchStartScreenPos;
                float dragDistance = screenDelta.magnitude;

                if (dragDistance > screenDeadzonePixels)
                {
                    float normalizedIntensity = Mathf.Clamp01((dragDistance - screenDeadzonePixels) / (screenMaxDragPixels - screenDeadzonePixels));
                    Vector2 dragDir = screenDelta.normalized;

                    Vector3 screenVector3 = new Vector3(dragDir.x, 0f, dragDir.y);
                    Vector3 isoMoveDir = Quaternion.Euler(0f, 45f, 0f) * screenVector3;

                    finalMoveVector = isoMoveDir.normalized * (currentSpeed * normalizedIntensity);

                    Quaternion targetRot = Quaternion.LookRotation(isoMoveDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 20f * Time.deltaTime);
                }
            }

            // Gravity
            if (controller.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }

            finalMoveVector.y = verticalVelocity.y;
            controller.Move(finalMoveVector * Time.deltaTime);
        }

        private void EnforceBoundary()
        {
            if (!restrictToBoundary) return;

            Vector3 currentPos = transform.position;
            float clampedX = Mathf.Clamp(currentPos.x, boundaryX.x, boundaryX.y);
            float clampedZ = Mathf.Clamp(currentPos.z, boundaryZ.x, boundaryZ.y);

            if (clampedX != currentPos.x || clampedZ != currentPos.z)
            {
                controller.enabled = false;
                transform.position = new Vector3(clampedX, currentPos.y, clampedZ);
                controller.enabled = true;
            }
        }
    }
}
