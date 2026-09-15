using System.Collections.Generic;
using UnityEngine;
using DeadDawn.Core;
using DeadDawn.Data;

namespace DeadDawn.Player
{
    public struct InventoryChangedEvent
    {
        public int Wood;
        public int Scrap;
        public int Gunpowder;
        public int BarricadeCount;

        public InventoryChangedEvent(int wood, int scrap, int gunpowder, int barricades)
        {
            Wood = wood;
            Scrap = scrap;
            Gunpowder = gunpowder;
            BarricadeCount = barricades;
        }
    }

    public class PlayerInventory : MonoBehaviour
    {
        [Header("Starting Resources")]
        [SerializeField] private int startingWood = 10;
        [SerializeField] private int startingScrap = 5;
        [SerializeField] private int startingGunpowder = 2;
        [SerializeField] private int startingBarricades = 2;

        private int wood;
        private int scrap;
        private int gunpowder;
        private int barricadeCount;

        public int Wood => wood;
        public int Scrap => scrap;
        public int Gunpowder => gunpowder;
        public int BarricadeCount => barricadeCount;

        private void Awake()
        {
            wood = startingWood;
            scrap = startingScrap;
            gunpowder = startingGunpowder;
            barricadeCount = startingBarricades;
        }

        private void Start()
        {
            PublishInventory();
        }

        public void AddResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    wood += amount;
                    break;
                case ResourceType.Scrap:
                    scrap += amount;
                    break;
                case ResourceType.Gunpowder:
                    gunpowder += amount;
                    break;
            }
            PublishInventory();
        }

        public bool TryConsumeResources(int woodCost, int scrapCost, int gunpowderCost)
        {
            if (wood >= woodCost && scrap >= scrapCost && gunpowder >= gunpowderCost)
            {
                wood -= woodCost;
                scrap -= scrapCost;
                gunpowder -= gunpowderCost;
                PublishInventory();
                return true;
            }
            return false;
        }

        public void AddBarricades(int count)
        {
            barricadeCount += count;
            PublishInventory();
        }

        public bool TryConsumeBarricade(int count = 1)
        {
            if (barricadeCount >= count)
            {
                barricadeCount -= count;
                PublishInventory();
                return true;
            }
            return false;
        }

        public void PublishInventory()
        {
            EventBus.Publish(new InventoryChangedEvent(wood, scrap, gunpowder, barricadeCount));
        }
    }
}
