using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeadDawn.Core;
using DeadDawn.Player;
using DeadDawn.Crafting;

namespace DeadDawn.UI
{
    /// <summary>
    /// Core UGUI HUD Controller for DeadDawn.
    /// Manages real-time data binding between game systems (health, ammo, resources, waves)
    /// and the high-fidelity UI Canvas built from Figma design assets.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Status Displays")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider healthSlider;

        [Header("Vitals Visuals")]
        [SerializeField] private Image vitalsCardImage;
        [SerializeField] private Sprite normalVitalsSprite;
        [SerializeField] private Sprite criticalVitalsSprite;

        [Header("Resource Counters")]
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI scrapText;
        [SerializeField] private TextMeshProUGUI gunpowderText;
        [SerializeField] private TextMeshProUGUI barricadeText;

        [Header("Wave & Horde Displays")]
        [SerializeField] private TextMeshProUGUI waveTitleText;
        [SerializeField] private TextMeshProUGUI waveSubText;
        private float currentHealth = 100f;
        private float maxHealth = 100f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // Initialize default values if player exists
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp != null)
                {
                    currentHealth = hp.CurrentHealth;
                    maxHealth = hp.MaxHealth;
                    UpdateHealthDisplay(currentHealth, maxHealth);
                }

                var shooting = player.GetComponent<PlayerShooting>();
                if (shooting != null)
                {
                    if (ammoText != null) ammoText.text = $"{shooting.CurrentAmmo:D2} / 120";
                }
            }
        }

        private void Update()
        {
            UpdateWaveDisplay();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerAmmoChangedEvent>(OnAmmoChanged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnHealthChanged);
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerAmmoChangedEvent>(OnAmmoChanged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnAmmoChanged(PlayerAmmoChangedEvent evt)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{evt.CurrentAmmo:D2} / {evt.MaxReserveAmmo}";
            }
        }

        private void OnHealthChanged(PlayerDamagedEvent evt)
        {
            currentHealth = evt.CurrentHealth;
            maxHealth = evt.MaxHealth;
            UpdateHealthDisplay(currentHealth, maxHealth);
        }

        private void UpdateHealthDisplay(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            bool isCritical = (current / Mathf.Max(1f, max)) <= 0.25f;

            if (healthText != null)
            {
                healthText.text = isCritical 
                    ? $"⚠️ CRITICAL: {(int)current} / {(int)max}" 
                    : $"HEALTH: {(int)current} / {(int)max}";
                healthText.color = isCritical ? new Color(0.97f, 0.32f, 0.29f) : Color.white;
            }

            if (vitalsCardImage != null)
            {
                if (isCritical && criticalVitalsSprite != null)
                {
                    vitalsCardImage.sprite = criticalVitalsSprite;
                }
                else if (!isCritical && normalVitalsSprite != null)
                {
                    vitalsCardImage.sprite = normalVitalsSprite;
                }
            }
        }

        private void OnInventoryChanged(InventoryChangedEvent evt)
        {
            if (woodText != null) woodText.text = $"{evt.Wood}";
            if (scrapText != null) scrapText.text = $"{evt.Scrap}";
            if (gunpowderText != null) gunpowderText.text = $"{evt.Gunpowder}";
            if (barricadeText != null) barricadeText.text = $"{evt.BarricadeCount}";
        }

        private void UpdateWaveDisplay()
        {
            var wm = WaveManager.Instance;
            if (wm == null) return;

            if (waveTitleText != null)
            {
                switch (wm.CurrentState)
                {
                    case GameState.Scavenge:
                        waveTitleText.text = $"DAY {wm.CurrentDay} // SCAVENGE PHASE";
                        if (waveSubText != null)
                        {
                            waveSubText.text = $"SUNLIGHT SECURE  •  {Mathf.CeilToInt(wm.StateTimer)}s REMAINING";
                            waveSubText.color = new Color(0.94f, 0.75f, 0.25f);
                        }
                        break;
                    case GameState.Warning:
                        waveTitleText.text = $"DAY {wm.CurrentDay} // HORDE ALERT";
                        if (waveSubText != null)
                        {
                            waveSubText.text = $"⚠️ INCOMING SURGE IN {Mathf.CeilToInt(wm.StateTimer)}s";
                            waveSubText.color = new Color(1f, 0.45f, 0.2f);
                        }
                        break;
                    case GameState.Horde:
                        waveTitleText.text = $"DAY {wm.CurrentDay} // MIDNIGHT HORDE";
                        if (waveSubText != null)
                        {
                            waveSubText.text = $"💀 {wm.RemainingHordeZombies} ZOMBIES ACTIVE";
                            waveSubText.color = new Color(0.97f, 0.32f, 0.29f);
                        }
                        break;
                    case GameState.Dawn:
                        waveTitleText.text = $"DAY {wm.CurrentDay} // DAWN BREAK";
                        if (waveSubText != null)
                        {
                            waveSubText.text = "WAVE SURVIVED  •  BASE REPAIRED";
                            waveSubText.color = new Color(0.24f, 0.72f, 0.31f);
                        }
                        break;
                }
            }
        }
    }
}
