using UnityEngine;
using DeadDawn.Data;
using DeadDawn.Player;

namespace DeadDawn.ResourceSystems
{
    public class ResourceNode : MonoBehaviour
    {
        [Header("Node Configuration")]
        [SerializeField] private SO_Resource resourceData;
        [SerializeField] private int harvestYield = 3;
        [SerializeField] private float harvestRadius = 2.2f;

        [Header("Juice Feedback")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.12f;

        private Vector3 basePosition;
        private bool isHarvested = false;

        private void Start()
        {
            basePosition = transform.position;
        }

        private void Update()
        {
            if (isHarvested) return;

            // Subtle hovering bob animation
            float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isHarvested) return;

            if (other.CompareTag("Player") && other.TryGetComponent<PlayerInventory>(out var inventory))
            {
                Harvest(inventory);
            }
        }

        public void Harvest(PlayerInventory inventory)
        {
            if (isHarvested) return;
            isHarvested = true;

            if (resourceData != null)
            {
                inventory.AddResource(resourceData.type, harvestYield);
            }

            // Clean destruction of node
            Destroy(gameObject);
        }
    }
}
