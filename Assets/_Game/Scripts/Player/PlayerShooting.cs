using UnityEngine;
using UnityEngine.InputSystem;
using DeadDawn.Core;
using DeadDawn.Combat;

namespace DeadDawn.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private DeadDawn.Data.WeaponData currentWeapon;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;

        private int currentAmmo;
        private float lastFireTime;

        public DeadDawn.Data.WeaponData CurrentWeapon => currentWeapon;
        public int CurrentAmmo => currentAmmo;

        private void Awake()
        {
            if (currentWeapon != null)
            {
                currentAmmo = currentWeapon.defaultStartingAmmo;
            }
            else
            {
                currentAmmo = 30;
            }
        }

        private void Start()
        {
            PublishAmmoEvent();
        }

        private void Update()
        {
            HandleShootingInput();
        }

        private bool waitingForInputRelease = false;
        private float resumeCooldownEndTime = 0f;

        private void HandleShootingInput()
        {
            // Block shooting while game is paused, menus are open, or pointer is hovering/clicking interactive UI or prompts
            if (UIInteractionBlocker.IsPointerOverUI())
            {
                waitingForInputRelease = true;
                resumeCooldownEndTime = Time.unscaledTime + 0.35f;
                return;
            }

            // Post-interaction / post-resume debounce delay
            if (Time.unscaledTime < resumeCooldownEndTime)
            {
                waitingForInputRelease = true;
                return;
            }

            // Require complete release of any click/touch used to press the resume button
            if (waitingForInputRelease)
            {
                bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
                bool touchHeld = false;
                var ts = Touchscreen.current;
                if (ts != null)
                {
                    for (int i = 0; i < ts.touches.Count; i++)
                    {
                        if (ts.touches[i].press.isPressed)
                        {
                            touchHeld = true;
                            break;
                        }
                    }
                }

                if (mouseHeld || touchHeld)
                {
                    return; // Still holding down the input that closed the UI
                }

                waitingForInputRelease = false; // Successfully released, now allow normal shooting
            }

            if (currentWeapon == null || firePoint == null) return;
            if (Time.time < lastFireTime + currentWeapon.fireRate) return;

            // 1. Mobile Touch: Any tap on the RIGHT half of the screen fires in player facing direction
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (touch.press.wasPressedThisFrame)
                    {
                        Vector2 touchPos = touch.position.ReadValue();
                        if (touchPos.x >= Screen.width * 0.45f)
                        {
                            FireInFacingDirection();
                            return;
                        }
                    }
                }
            }

            // 2. PC Mouse Click / Space Key
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireInFacingDirection();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                FireInFacingDirection();
                return;
            }
        }

        private void FireInFacingDirection()
        {
            if (!TryConsumeAmmo(1))
            {
                return;
            }

            lastFireTime = Time.time;

            // Shoot strictly along the player's horizontal facing direction at standard torso height
            Vector3 fireDir = transform.forward;
            fireDir.y = 0f;
            fireDir.Normalize();
            Quaternion bulletRotation = Quaternion.LookRotation(fireDir);

            // Torso-aligned spawn height for guaranteed intersection with enemy colliders
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            spawnPos.y = 1.05f;

            GameObject bulletObj = null;
            if (ObjectPool.Instance != null)
            {
                bulletObj = ObjectPool.Instance.Get(spawnPos, bulletRotation);
            }
            else if (bulletPrefab != null)
            {
                bulletObj = Instantiate(bulletPrefab, spawnPos, bulletRotation);
            }

            if (bulletObj != null && bulletObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Configure(currentWeapon.damage, currentWeapon.bulletSpeed);
            }
        }

        public void EquipWeapon(DeadDawn.Data.WeaponData newWeapon)
        {
            currentWeapon = newWeapon;
            currentAmmo = Mathf.Min(currentAmmo, currentWeapon.maxAmmoCapacity);
            PublishAmmoEvent();
        }

        public void AddAmmo(int amount)
        {
            int max = currentWeapon != null ? currentWeapon.maxAmmoCapacity : 120;
            currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, max);
            PublishAmmoEvent();
        }

        public bool TryConsumeAmmo(int amount)
        {
            if (currentAmmo >= amount)
            {
                currentAmmo -= amount;
                PublishAmmoEvent();
                return true;
            }
            return false;
        }

        private void PublishAmmoEvent()
        {
            int max = currentWeapon != null ? currentWeapon.maxAmmoCapacity : 120;
            EventBus.Publish(new PlayerAmmoChangedEvent(currentAmmo, max));
        }
    }
}
