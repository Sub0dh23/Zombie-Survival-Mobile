using UnityEngine;

namespace DeadDawn.Data
{
    public enum ResourceType
    {
        Wood,
        Scrap,
        Gunpowder
    }

    [CreateAssetMenu(fileName = "Resource", menuName = "DeadDawn/Resource")]
    public class SO_Resource : ScriptableObject
    {
        public string resourceName;
        public ResourceType type;
        public Color nodeColor = Color.white;
        public int defaultHarvestAmount = 3;
    }
}
