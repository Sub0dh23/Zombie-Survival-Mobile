using System.Collections.Generic;
using UnityEngine;

namespace DeadDawn.ResourceSystems
{
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private GameObject[] resourcePrefabs; // Wood, Scrap, Gunpowder
        [SerializeField] private int initialSpawnCount = 32;
        [SerializeField] private float minSpawnRadius = 6f;
        [SerializeField] private float maxSpawnRadius = 35f;

        private readonly List<GameObject> activeNodes = new List<GameObject>();

        public int ActiveNodeCount => activeNodes.Count;

        private void Start()
        {
            SpawnDailyHarvestNodes();
        }

        public void SpawnDailyHarvestNodes()
        {
            // Clear any old lingering nodes
            for (int i = activeNodes.Count - 1; i >= 0; i--)
            {
                if (activeNodes[i] != null)
                {
                    Destroy(activeNodes[i]);
                }
            }
            activeNodes.Clear();

            if (resourcePrefabs == null || resourcePrefabs.Length == 0) return;

            for (int i = 0; i < initialSpawnCount; i++)
            {
                // Select random prefab (wood is more common, gunpowder is rare)
                int prefabIndex = ChooseWeightedResourceIndex();
                GameObject prefab = resourcePrefabs[prefabIndex];

                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 spawnPos = new Vector3(randomCircle.x, 0.5f, randomCircle.y);

                GameObject nodeObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                activeNodes.Add(nodeObj);
            }
        }

        private int ChooseWeightedResourceIndex()
        {
            // 55% Wood (index 0), 30% Scrap (index 1), 15% Gunpowder (index 2)
            float roll = Random.value;
            if (roll < 0.55f) return 0;
            if (roll < 0.85f && resourcePrefabs.Length > 1) return 1;
            return Mathf.Min(2, resourcePrefabs.Length - 1);
        }
    }
}
