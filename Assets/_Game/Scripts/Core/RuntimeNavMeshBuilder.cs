using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace DeadDawn.Core
{
    /// <summary>
    /// Builds or updates the NavMesh at runtime across the Ground and around base defenses.
    /// </summary>
    public class RuntimeNavMeshBuilder : MonoBehaviour
    {
        private NavMeshSurface navMeshSurface;

        private void Awake()
        {
            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }

            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.BuildNavMesh();
        }

        public void RebuildNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }
        }
    }
}
