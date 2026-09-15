using UnityEngine;
using DeadDawn.Core;
using DeadDawn.Combat;
using DeadDawn.Enemy;

namespace DeadDawn.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        private float currentHealth;
        private float lastDamageTime;
        private WorldHealthBar healthBar;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            healthBar = GetComponent<WorldHealthBar>();
            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<WorldHealthBar>();
            }
            healthBar.SetFillColor(new Color(0.2f, 0.9f, 0.3f));
        }

        private void Start()
        {
            PublishHealthEvent();
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth, maxHealth);
            }
        }

        public void TakeDamage(float amount)
        {
            if (Time.time < lastDamageTime + invulnerabilityDuration) return;
            if (currentHealth <= 0) return;

            lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0f, currentHealth - amount);

            // Overhead floating damage indicator
            DamagePopup.Spawn(transform.position, amount, new Color(1f, 0.25f, 0.25f));

            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth, maxHealth);
            }

            PublishHealthEvent();
            EventBus.Publish(new ScreenShakeEvent(0.25f, 0.15f));

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth, maxHealth);
            }
            PublishHealthEvent();
        }

        private void Die()
        {
            int day = WaveManager.Instance != null ? WaveManager.Instance.CurrentDay : 1;
            EventBus.Publish(new GameStateChangedEvent(GameState.Horde, GameState.GameOver, day));
        }

        private void PublishHealthEvent()
        {
            EventBus.Publish(new PlayerDamagedEvent(currentHealth, maxHealth));
        }
    }
}
