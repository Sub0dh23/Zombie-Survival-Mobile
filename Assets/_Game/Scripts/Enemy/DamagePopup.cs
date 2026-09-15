using UnityEngine;

namespace DeadDawn.Enemy
{
    /// <summary>
    /// Lightweight, zero-asset floating damage number popup.
    /// Billboarded to the isometric camera, floats upwards, scales with pop-punch, and fades out.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private TextMesh textMesh;
        private Color textColor;
        private float lifeTimer;
        private float maxLifetime = 0.85f;
        private Vector3 moveDirection;
        private Transform cameraTransform;

        public static DamagePopup Spawn(Vector3 worldPosition, float damageAmount, Color color, bool isCritical = false)
        {
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.transform.position = worldPosition + Vector3.up * 1.8f + Random.insideUnitSphere * 0.25f;

            DamagePopup popup = popupObj.AddComponent<DamagePopup>();
            popup.Setup(damageAmount, color, isCritical);
            return popup;
        }

        private void Setup(float damageAmount, Color color, bool isCritical)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = $"-{Mathf.RoundToInt(damageAmount)}";
            textMesh.fontSize = isCritical ? 36 : 28;
            textMesh.characterSize = isCritical ? 0.12f : 0.09f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;

            textColor = color;
            textMesh.color = textColor;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 50;
            }

            lifeTimer = maxLifetime;
            moveDirection = new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, Random.Range(-0.4f, 0.4f));

            transform.localScale = Vector3.one * (isCritical ? 1.4f : 1.0f);
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;

            // Face isometric camera
            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            // Float upwards smoothly
            transform.position += moveDirection * Time.deltaTime;
            moveDirection.y = Mathf.Lerp(moveDirection.y, 0.8f, Time.deltaTime * 3f);

            // Pop scale effect
            float progress = 1f - (lifeTimer / maxLifetime);
            if (progress < 0.2f)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, progress / 0.2f);
            }

            // Alpha fade out in the last 40% of duration
            if (progress > 0.6f)
            {
                float fadeProgress = (progress - 0.6f) / 0.4f;
                textColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                textMesh.color = textColor;
            }

            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
