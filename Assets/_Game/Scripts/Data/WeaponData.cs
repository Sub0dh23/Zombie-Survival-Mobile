using UnityEngine;

namespace DeadDawn.Data
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Rifle,
        Sniper
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "DeadDawn/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "Pistol";
        public WeaponType weaponType = WeaponType.Pistol;

        [Header("Combat Ranges & Angles")]
        [Tooltip("Maximum engagement distance for auto-targeting and firing.")]
        public float effectiveRange = 8f;

        [Tooltip("Minimum distance from player before gun starts firing (if any).")]
        public float minimumRange = 0f;

        [Tooltip("Cone angle in front of player where enemies trigger auto-shooting.")]
        public float fieldOfFireAngle = 55f;

        [Header("Firing Parameters")]
        public float fireRate = 0.28f;
        public float damage = 25f;
        public float bulletSpeed = 30f;
        public int maxAmmoCapacity = 120;
        public int defaultStartingAmmo = 30;

        [Header("Juice Feedback")]
        public float screenShakeIntensity = 0.08f;
        public float screenShakeDuration = 0.06f;
    }
}
