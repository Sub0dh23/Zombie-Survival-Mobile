using UnityEngine;

namespace DeadDawn.Enemy
{
    /// <summary>
    /// Overhead world-space billboard health bar.
    /// Built using lightweight procedural quads with unlit vertex colors.
    /// Works with zero external UI/TMP canvas dependencies.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [Header("Position & Dimensions")]
        [SerializeField] private float heightOffset = 2.1f;
        [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.14f);
        [SerializeField] private bool hideWhenFull = false;

        [Header("Colors")]
        [SerializeField] private Color fillColor = new Color(0.9f, 0.2f, 0.2f, 0.95f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        private Transform cameraTransform;
        private GameObject barRoot;
        private Transform fillBarTransform;
        private MeshRenderer fillRenderer;
        private MeshRenderer bgRenderer;

        private float targetHealthPercent = 1f;
        private float currentDisplayPercent = 1f;

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            BuildBarVisuals();
        }

        private void BuildBarVisuals()
        {
            barRoot = new GameObject("WorldHealthBar_Root");
            barRoot.transform.SetParent(transform);
            barRoot.transform.localPosition = new Vector3(0f, heightOffset, 0f);

            // 1. Background quad
            GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgObj.name = "HealthBar_Background";
            bgObj.transform.SetParent(barRoot.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.identity;
            bgObj.transform.localScale = new Vector3(barSize.x + 0.06f, barSize.y + 0.04f, 1f);

            // Remove default collider
            Collider bgCol = bgObj.GetComponent<Collider>();
            if (bgCol != null) Destroy(bgCol);

            bgRenderer = bgObj.GetComponent<MeshRenderer>();
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            bgMat.color = backgroundColor;
            bgRenderer.material = bgMat;
            bgRenderer.sortingOrder = 40;

            // 2. Foreground Fill Quad
            GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillObj.name = "HealthBar_Fill";
            fillObj.transform.SetParent(barRoot.transform);
            // Anchor to left by shifting initial local position
            fillObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fillObj.transform.localRotation = Quaternion.identity;
            fillObj.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);

            Collider fillCol = fillObj.GetComponent<Collider>();
            if (fillCol != null) Destroy(fillCol);

            fillRenderer = fillObj.GetComponent<MeshRenderer>();
            Material fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            fillMat.color = fillColor;
            fillRenderer.material = fillMat;
            fillRenderer.sortingOrder = 41;

            fillBarTransform = fillObj.transform;

            if (hideWhenFull)
            {
                barRoot.SetActive(false);
            }
        }

        public void SetFillColor(Color newColor)
        {
            fillColor = newColor;
            if (fillRenderer != null && fillRenderer.material != null)
            {
                fillRenderer.material.color = fillColor;
            }
        }

        public void SetHealth(float current, float max)
        {
            targetHealthPercent = Mathf.Clamp01(max > 0f ? current / max : 0f);

            if (barRoot != null)
            {
                if (hideWhenFull && targetHealthPercent >= 0.999f)
                {
                    barRoot.SetActive(false);
                }
                else
                {
                    barRoot.SetActive(true);
                }
            }
        }

        private void LateUpdate()
        {
            if (barRoot == null || !barRoot.activeSelf) return;

            // Face isometric camera
            if (cameraTransform != null)
            {
                barRoot.transform.rotation = cameraTransform.rotation;
            }

            // Smooth interpolation
            currentDisplayPercent = Mathf.Lerp(currentDisplayPercent, targetHealthPercent, Time.deltaTime * 12f);

            // Scale and anchor fill to the left side
            if (fillBarTransform != null)
            {
                float currentWidth = barSize.x * currentDisplayPercent;
                fillBarTransform.localScale = new Vector3(currentWidth, barSize.y, 1f);

                // Pivot to left: offset x by -half original width + half current width
                float offsetX = (-barSize.x * 0.5f) + (currentWidth * 0.5f);
                fillBarTransform.localPosition = new Vector3(offsetX, 0f, -0.01f);
            }
        }

        private void OnDestroy()
        {
            if (barRoot != null)
            {
                Destroy(barRoot);
            }
        }
    }
}
