using UnityEngine;
using DeadDawn.Core;

namespace DeadDawn.CameraControl
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(-12f, 16f, -12f);
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Camera Projection")]
        [SerializeField] private bool useOrthographic = true;
        [SerializeField] private float orthographicSize = 9f;

        [Header("Map Boundary Clamping")]
        [SerializeField] private bool clampToMapBounds = true;
        [SerializeField] private Vector2 xBounds = new Vector2(-30f, 30f);
        [SerializeField] private Vector2 zBounds = new Vector2(-30f, 30f);

        private Camera cam;
        private Vector3 currentShakeOffset = Vector3.zero;
        private float shakeTimer = 0f;
        private float shakeDuration = 0f;
        private float shakeIntensity = 0f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ExpandMapBounds(float extraRadius)
        {
            xBounds.x -= extraRadius;
            xBounds.y += extraRadius;
            zBounds.x -= extraRadius;
            zBounds.y += extraRadius;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = useOrthographic;
                if (useOrthographic)
                {
                    cam.orthographicSize = orthographicSize;
                }
            }

            transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ScreenShakeEvent>(OnScreenShake);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScreenShakeEvent>(OnScreenShake);
        }

        private void OnScreenShake(ScreenShakeEvent evt)
        {
            shakeIntensity = evt.Intensity;
            shakeDuration = evt.Duration;
            shakeTimer = evt.Duration;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredTargetPos = target.position;

            if (clampToMapBounds)
            {
                desiredTargetPos.x = Mathf.Clamp(desiredTargetPos.x, xBounds.x, xBounds.y);
                desiredTargetPos.z = Mathf.Clamp(desiredTargetPos.z, zBounds.x, zBounds.y);
            }

            // Calculate clean shake offset that doesn't fight camera tracking
            if (shakeTimer > 0f)
            {
                float factor = shakeTimer / shakeDuration;
                currentShakeOffset = Random.insideUnitSphere * (shakeIntensity * factor);
                currentShakeOffset.z = 0f;
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                currentShakeOffset = Vector3.zero;
            }

            Vector3 targetCamPosition = desiredTargetPos + offset + currentShakeOffset;
            transform.position = Vector3.Lerp(transform.position, targetCamPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
