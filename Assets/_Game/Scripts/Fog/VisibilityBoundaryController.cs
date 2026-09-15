using UnityEngine;

namespace DeadDawn.Fog
{
    /// <summary>
    /// Enforces a localized visibility boundary around the player's view so the player
    /// cannot see the entire map and only sees what is within their immediate exploration range.
    /// Tracks the player's screen position and adjusts the fog of war mask dynamically.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisibilityBoundaryController : MonoBehaviour
    {
        public static VisibilityBoundaryController Instance { get; private set; }

        [Header("Target Tracking")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera targetCamera;

        [Header("Vision Settings")]
        [SerializeField] private float clearVisionRadius = 0.26f; // Fraction of screen height
        [SerializeField] private float featherSoftness = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float fogDensity = 0.98f;
        [SerializeField] private Color fogColor = new Color(0.11f, 0.14f, 0.20f, 0.98f);

        [Header("Dynamic Vision Pulse")]
        [SerializeField] private bool enableSubtlePulse = true;
        [SerializeField] private float pulseFrequency = 0.8f;
        [SerializeField] private float pulseAmplitude = 0.012f;

        private Material fogMaterial;
        private MeshRenderer meshRenderer;

        public float ClearVisionRadius
        {
            get => clearVisionRadius;
            set => clearVisionRadius = value;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                fogMaterial = meshRenderer.material;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Start()
        {
            FindPlayer();
            SetupCameraAttachment();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        public void SetupCameraAttachment()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            // Parent to camera so the quad stays locked in front of camera lens
            transform.SetParent(targetCamera.transform, false);
            transform.localPosition = new Vector3(0f, 0f, 4f);
            transform.localRotation = Quaternion.identity;

            // Size quad to cover the orthographic/perspective camera viewport with margin
            float orthoSize = targetCamera.orthographic ? targetCamera.orthographicSize : 9f;
            float height = orthoSize * 2.2f;
            float width = height * 2.5f; // Generously wide for ultrawide monitors
            transform.localScale = new Vector3(width, height, 1f);
        }

        public void ExpandVision(float amount)
        {
            clearVisionRadius += amount;
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // Calculate player position in normalized screen viewport space (0..1)
            Vector3 viewportPos = targetCamera.WorldToViewportPoint(playerTransform.position);

            // Subtle breathing pulse for organic horror atmosphere
            float currentRadius = clearVisionRadius;
            if (enableSubtlePulse)
            {
                currentRadius += Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
            }

            if (fogMaterial != null)
            {
                fogMaterial.SetVector("_PlayerScreenPos", new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
                fogMaterial.SetFloat("_ClearRadius", currentRadius);
                fogMaterial.SetFloat("_Feather", featherSoftness);
                fogMaterial.SetFloat("_FogDensity", fogDensity);
                fogMaterial.SetColor("_FogColor", fogColor);
            }
        }
    }
}
