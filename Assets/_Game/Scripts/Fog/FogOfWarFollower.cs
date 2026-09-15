using UnityEngine;

namespace DeadDawn.Fog
{
    public class FogOfWarFollower : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private float heightOffset = 0.05f;

        [Header("Fog Radius")]
        [SerializeField] private float currentRadius = 14f;

        public float CurrentRadius => currentRadius;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ExpandRadius(float amount)
        {
            currentRadius += amount;
            // Mesh is a Quad rotated (90, 0, 0), so local X and local Y form the horizontal ground plane
            transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, 1f);
        }

        private void Start()
        {
            // Mesh is a Quad rotated (90, 0, 0), so local X and local Y form the horizontal ground plane
            transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, 1f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Follow player's X and Z position smoothly, staying slightly above ground
            Vector3 pos = target.position;
            pos.y = heightOffset;
            transform.position = pos;
        }
    }
}
