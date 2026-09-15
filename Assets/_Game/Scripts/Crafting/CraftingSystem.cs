using System.Collections.Generic;
using UnityEngine;
using DeadDawn.Core;
using DeadDawn.Data;
using DeadDawn.Player;

namespace DeadDawn.Crafting
{
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Available Recipes")]
        [SerializeField] private SO_CraftingRecipe pistolAmmoRecipe;
        [SerializeField] private SO_CraftingRecipe woodBarricadeRecipe;
        [SerializeField] private SO_CraftingRecipe repairKitRecipe;

        public SO_CraftingRecipe PistolAmmoRecipe => pistolAmmoRecipe;
        public SO_CraftingRecipe WoodBarricadeRecipe => woodBarricadeRecipe;
        public SO_CraftingRecipe RepairKitRecipe => repairKitRecipe;

        public List<SO_CraftingRecipe> GetAvailableRecipes()
        {
            var list = new List<SO_CraftingRecipe>();
            if (pistolAmmoRecipe != null) list.Add(pistolAmmoRecipe);
            if (woodBarricadeRecipe != null) list.Add(woodBarricadeRecipe);
            if (repairKitRecipe != null) list.Add(repairKitRecipe);
            return list;
        }

        public bool CanCraft(SO_CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            if (playerInventory == null) FindPlayerReferences();
            if (playerInventory == null) return false;

            return playerInventory.Wood >= recipe.woodCost
                && playerInventory.Scrap >= recipe.scrapCost
                && playerInventory.Gunpowder >= recipe.gunpowderCost;
        }

        private PlayerInventory playerInventory;
        private PlayerShooting playerShooting;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
                playerShooting = player.GetComponent<PlayerShooting>();
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        public bool CraftRecipe(SO_CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            if (playerInventory == null) FindPlayerReferences();
            if (playerInventory == null) return false;

            if (playerInventory.TryConsumeResources(recipe.woodCost, recipe.scrapCost, recipe.gunpowderCost))
            {
                ApplyCraftResult(recipe);
                return true;
            }

            return false;
        }

        public bool CraftPistolAmmo()
        {
            return CraftRecipe(pistolAmmoRecipe);
        }

        public bool CraftWoodBarricade()
        {
            return CraftRecipe(woodBarricadeRecipe);
        }

        public bool CraftRepairKit()
        {
            return CraftRecipe(repairKitRecipe);
        }

        private void ApplyCraftResult(SO_CraftingRecipe recipe)
        {
            switch (recipe.resultType)
            {
                case CraftResultType.Ammo:
                    if (playerShooting != null)
                    {
                        playerShooting.AddAmmo(recipe.resultQuantity);
                    }
                    break;

                case CraftResultType.Barricade:
                    if (playerInventory != null)
                    {
                        playerInventory.AddBarricades(recipe.resultQuantity);
                    }
                    break;

                case CraftResultType.RepairKit:
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(30f);
                    }
                    break;
            }
        }
    }
}
