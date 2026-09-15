using UnityEngine;
using DeadDawn.Core;
using DeadDawn.Combat;

namespace DeadDawn.Combat
{
    /// <summary>
    /// Defensive structure built by the player to block zombies and protect base perimeter.
    /// Implements IDamageable to take damage from attacking enemies.
    /// </summary>
    public class Barricade : MonoBehaviour, IDamageable
    {
        [Header("Durability")]
        [SerializeField] private float maxHealth = 150f;
        [SerializeField] private float currentHealth;

        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer[] woodRenderers;

        private Color originalColor = Color.white;
        private float flashTimer = 0f;
        private bool isDestroyed = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);

        private void Awake()
        {
            currentHealth = maxHealth;
            if (woodRenderers == null || woodRenderers.Length == 0)
            {
                woodRenderers = GetComponentsInChildren<MeshRenderer>();
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            flashTimer = 0.12f;

            if (currentHealth <= 0f)
            {
                DestroyBarricade();
            }
        }

        public bool Repair(float amount)
        {
            if (isDestroyed || currentHealth >= maxHealth) return false;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            return true;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
            }
        }

        private void DestroyBarricade()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            // Trigger slight camera rumble if near
            EventBus.Publish(new ScreenShakeEvent(0.2f, 0.15f));

            // Clean destruction
            Destroy(gameObject, 0.05f);
        }
    }
}
