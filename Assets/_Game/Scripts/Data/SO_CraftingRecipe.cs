using UnityEngine;

namespace DeadDawn.Data
{
    public enum CraftResultType
    {
        Ammo,
        Barricade,
        RepairKit
    }

    [CreateAssetMenu(fileName = "Recipe", menuName = "DeadDawn/Crafting Recipe")]
    public class SO_CraftingRecipe : ScriptableObject
    {
        public string recipeName;
        [TextArea] public string description;

        [Header("Material Costs")]
        public int woodCost = 0;
        public int scrapCost = 0;
        public int gunpowderCost = 0;

        [Header("Output")]
        public CraftResultType resultType;
        public int resultQuantity = 1;
        public GameObject placeablePrefab; // For barricades/traps
    }
}
