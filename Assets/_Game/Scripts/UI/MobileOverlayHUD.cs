using UnityEngine;
using DeadDawn.Core;
using DeadDawn.Player;
using DeadDawn.Crafting;

namespace DeadDawn.UI
{
    /// <summary>
    /// High-fidelity game HUD powered by Figma-exported UI components.
    /// Replaces the legacy raw UI with pixel-perfect responsive panels, live vitality bars,
    /// dynamic weapon stations, scavenger inventory counters, and quick-crafting triggers.
    /// </summary>
    public class MobileOverlayHUD : MonoBehaviour
    {
        private int currentAmmo = 30;
        private int maxAmmo = 120;
        private float currentHealth = 100f;
        private float maxHealth = 100f;

        private int wood = 10;
        private int scrap = 5;
        private int gunpowder = 2;
        private int barricades = 2;

        [Header("Exported Figma HUD Sprites")]
        [SerializeField] private Texture2D waveTrackerTex;
        [SerializeField] private Texture2D salvageTex;
        [SerializeField] private Texture2D vitalsTex;
        [SerializeField] private Texture2D vitalsCritTex;
        [SerializeField] private Texture2D weaponStationTex;

        // Cached solid texture for dynamic health bar fills
        private Texture2D greenFillTex;
        private Texture2D redFillTex;
        private Texture2D staminaFillTex;

        private GUIStyle waveTitleStyle;
        private GUIStyle waveSubStyle;
        private GUIStyle statValueStyle;
        private GUIStyle bigAmmoStyle;
        private GUIStyle ammoReserveStyle;
        private GUIStyle weaponTitleStyle;
        private GUIStyle vitalsHpStyle;
        private GUIStyle emptyBtnStyle;
        private bool stylesInitialized = false;

        private void Awake()
        {
            LoadFigmaTextures();
            CreateFillTextures();
        }

        private void LoadFigmaTextures()
        {
            if (waveTrackerTex == null) waveTrackerTex = LoadSprite("HUD_Wave_Tracker.png");
            if (salvageTex == null) salvageTex = LoadSprite("HUD_Resource_Salvage.png");
            if (vitalsTex == null) vitalsTex = LoadSprite("HUD_Player_Vitals.png");
            if (vitalsCritTex == null) vitalsCritTex = LoadSprite("Variant_Critical_Low_Health.png");
            if (weaponStationTex == null) weaponStationTex = LoadSprite("HUD_Weapon_Station.png");
        }

        private Texture2D LoadSprite(string filename)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath, "_Game/UI/Sprites", filename);
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        return tex;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobileOverlayHUD] Could not load sprite {filename}: {ex.Message}");
            }
            return null;
        }

        private void CreateFillTextures()
        {
            greenFillTex = new Texture2D(1, 1);
            greenFillTex.SetPixel(0, 0, new Color(0.24f, 0.72f, 0.31f, 0.95f));
            greenFillTex.Apply();

            redFillTex = new Texture2D(1, 1);
            redFillTex.SetPixel(0, 0, new Color(0.97f, 0.32f, 0.29f, 0.95f));
            redFillTex.Apply();

            staminaFillTex = new Texture2D(1, 1);
            staminaFillTex.SetPixel(0, 0, new Color(0.94f, 0.53f, 0.24f, 0.95f));
            staminaFillTex.Apply();
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
            currentAmmo = evt.CurrentAmmo;
            maxAmmo = evt.MaxReserveAmmo;
        }

        private void OnHealthChanged(PlayerDamagedEvent evt)
        {
            currentHealth = evt.CurrentHealth;
            maxHealth = evt.MaxHealth;
        }

        private void OnInventoryChanged(InventoryChangedEvent evt)
        {
            wood = evt.Wood;
            scrap = evt.Scrap;
            gunpowder = evt.Gunpowder;
            barricades = evt.BarricadeCount;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            int screenH = Screen.height;

            waveTitleStyle = new GUIStyle();
            waveTitleStyle.fontStyle = FontStyle.Bold;
            waveTitleStyle.fontSize = Mathf.Clamp(screenH / 54, 13, 17);
            waveTitleStyle.normal.textColor = Color.white;
            waveTitleStyle.alignment = TextAnchor.MiddleCenter;

            waveSubStyle = new GUIStyle();
            waveSubStyle.fontStyle = FontStyle.Bold;
            waveSubStyle.fontSize = Mathf.Clamp(screenH / 68, 10, 13);
            waveSubStyle.normal.textColor = new Color(0.97f, 0.32f, 0.29f);
            waveSubStyle.alignment = TextAnchor.MiddleCenter;

            statValueStyle = new GUIStyle();
            statValueStyle.fontStyle = FontStyle.Bold;
            statValueStyle.fontSize = Mathf.Clamp(screenH / 64, 11, 14);
            statValueStyle.normal.textColor = Color.white;
            statValueStyle.alignment = TextAnchor.MiddleRight;

            bigAmmoStyle = new GUIStyle();
            bigAmmoStyle.fontStyle = FontStyle.Bold;
            bigAmmoStyle.fontSize = Mathf.Clamp(screenH / 22, 28, 48);
            bigAmmoStyle.normal.textColor = Color.white;
            bigAmmoStyle.alignment = TextAnchor.MiddleRight;

            ammoReserveStyle = new GUIStyle();
            ammoReserveStyle.fontStyle = FontStyle.Bold;
            ammoReserveStyle.fontSize = Mathf.Clamp(screenH / 46, 14, 20);
            ammoReserveStyle.normal.textColor = new Color(0.55f, 0.58f, 0.63f);
            ammoReserveStyle.alignment = TextAnchor.MiddleLeft;

            weaponTitleStyle = new GUIStyle();
            weaponTitleStyle.fontStyle = FontStyle.Bold;
            weaponTitleStyle.fontSize = Mathf.Clamp(screenH / 52, 12, 16);
            weaponTitleStyle.normal.textColor = Color.white;
            weaponTitleStyle.alignment = TextAnchor.MiddleLeft;

            vitalsHpStyle = new GUIStyle();
            vitalsHpStyle.fontStyle = FontStyle.Bold;
            vitalsHpStyle.fontSize = Mathf.Clamp(screenH / 60, 11, 14);
            vitalsHpStyle.normal.textColor = Color.white;
            vitalsHpStyle.alignment = TextAnchor.MiddleLeft;

            emptyBtnStyle = new GUIStyle();

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen || DeathScreenUI.IsGameOver) return;

            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;
            float uiScale = Mathf.Clamp(screenH / 1080f, 0.65f, 1.25f);

            // 1. TOP CENTER: HORDE WAVE BANNER
            DrawWaveBanner(screenW, screenH, uiScale);

            // 2. TOP RIGHT: SALVAGE INVENTORY
            DrawSalvageInventory(screenW, screenH, uiScale);

            // 3. BOTTOM LEFT: PLAYER VITALS & HEALTH BAR
            DrawVitalsPod(screenW, screenH, uiScale);

            // 4. BOTTOM RIGHT: WEAPON STATION & AMMO
            DrawWeaponStation(screenW, screenH, uiScale);
        }

        private void DrawWaveBanner(float screenW, float screenH, float scale)
        {
            float w = 480f * scale;
            float h = 82f * scale;
            float x = (screenW - w) * 0.5f;
            float y = 16f * scale;

            Rect bannerRect = new Rect(x, y, w, h);
            if (waveTrackerTex != null)
            {
                GUI.DrawTexture(bannerRect, waveTrackerTex, ScaleMode.StretchToFill);
            }

            var wm = WaveManager.Instance;
            string topText = "DAY 14 // MIDNIGHT HORDE";
            string subText = "CRITICAL SURGE  •  💀 23 REMAINING";

            if (wm != null)
            {
                switch (wm.CurrentState)
                {
                    case GameState.Scavenge:
                        topText = $"DAY {wm.CurrentDay} // SCAVENGE PHASE";
                        subText = $"SUNLIGHT SECURE  •  {Mathf.CeilToInt(wm.StateTimer)}s REMAINING";
                        break;
                    case GameState.Warning:
                        topText = $"DAY {wm.CurrentDay} // HORDE ALERT";
                        subText = $"⚠️ INCOMING SURGE IN {Mathf.CeilToInt(wm.StateTimer)}s";
                        break;
                    case GameState.Horde:
                        topText = $"DAY {wm.CurrentDay} // MIDNIGHT HORDE";
                        subText = $"💀 {wm.RemainingHordeZombies} ZOMBIES ACTIVE";
                        break;
                    case GameState.Dawn:
                        topText = $"DAY {wm.CurrentDay} // DAWN BREAK";
                        subText = "WAVE SURVIVED  •  BASE REPAIRED";
                        break;
                }
            }

            GUI.Label(new Rect(x, y + 16f * scale, w, 24f * scale), topText, waveTitleStyle);
            GUI.Label(new Rect(x, y + 44f * scale, w, 22f * scale), subText, waveSubStyle);
        }

        private void DrawSalvageInventory(float screenW, float screenH, float scale)
        {
            float w = 280f * scale;
            float h = 135f * scale;
            float x = screenW - w - 24f * scale;
            float y = 20f * scale;

            Rect cardRect = new Rect(x, y, w, h);
            if (salvageTex != null)
            {
                GUI.DrawTexture(cardRect, salvageTex, ScaleMode.StretchToFill);
            }

            // Overlay dynamic numeric values matching Figma layout rows
            float rowX = x + 160f * scale;
            float valW = 96f * scale;
            float startY = y + 36f * scale;
            float rowSpacing = 24f * scale;

            GUI.Label(new Rect(rowX, startY, valW, 20f * scale), $"{wood}", statValueStyle);
            GUI.Label(new Rect(rowX, startY + rowSpacing, valW, 20f * scale), $"{scrap}", statValueStyle);
            GUI.Label(new Rect(rowX, startY + rowSpacing * 2f, valW, 20f * scale), $"{gunpowder}", statValueStyle);
            GUI.Label(new Rect(rowX, startY + rowSpacing * 3f, valW, 20f * scale), $"{barricades}", statValueStyle);
        }

        private void DrawVitalsPod(float screenW, float screenH, float scale)
        {
            float w = 380f * scale;
            float h = 140f * scale;
            float x = 24f * scale;
            float y = screenH - h - 24f * scale;

            bool isCritical = (currentHealth / Mathf.Max(1f, maxHealth)) <= 0.25f;
            Texture2D podTex = (isCritical && vitalsCritTex != null) ? vitalsCritTex : vitalsTex;

            Rect podRect = new Rect(x, y, w, h);
            if (podTex != null)
            {
                GUI.DrawTexture(podRect, podTex, ScaleMode.StretchToFill);
            }

            // Dynamic Health Bar fill
            float barX = x + 20f * scale;
            float barY = y + 38f * scale;
            float barMaxW = 340f * scale;
            float barH = 24f * scale;

            float hpRatio = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            Texture2D fill = isCritical ? redFillTex : greenFillTex;
            if (fill != null && hpRatio > 0f)
            {
                GUI.DrawTexture(new Rect(barX, barY, barMaxW * hpRatio, barH), fill, ScaleMode.StretchToFill);
            }

            // Dynamic Health Text
            string hpString = isCritical ? $"⚠️ CRITICAL: {(int)currentHealth}/{(int)maxHealth}" : $"HEALTH: {(int)currentHealth} / {(int)maxHealth}";
            GUI.Label(new Rect(barX + 10f * scale, barY + 2f * scale, barMaxW, barH), hpString, vitalsHpStyle);

            // Dynamic Stamina Bar fill
            float stBarY = y + 70f * scale;
            float stBarH = 12f * scale;
            if (staminaFillTex != null)
            {
                GUI.DrawTexture(new Rect(barX, stBarY, barMaxW * 0.85f, stBarH), staminaFillTex, ScaleMode.StretchToFill);
            }
        }

        private void DrawWeaponStation(float screenW, float screenH, float scale)
        {
            float w = 350f * scale;
            float h = 140f * scale;
            float x = screenW - w - 24f * scale;
            float y = screenH - h - 24f * scale;

            Rect stationRect = new Rect(x, y, w, h);
            if (weaponStationTex != null)
            {
                GUI.DrawTexture(stationRect, weaponStationTex, ScaleMode.StretchToFill);
            }

            // Dynamic Ammo Counts
            float ammoNumX = x + 160f * scale;
            float ammoNumY = y + 16f * scale;

            GUI.Label(new Rect(ammoNumX, ammoNumY, 80f * scale, 50f * scale), $"{currentAmmo:D2}", bigAmmoStyle);
            GUI.Label(new Rect(ammoNumX + 86f * scale, ammoNumY + 16f * scale, 70f * scale, 30f * scale), $"/ {maxAmmo}", ammoReserveStyle);
        }

        private void OnDestroy()
        {
            if (greenFillTex != null) Destroy(greenFillTex);
            if (redFillTex != null) Destroy(redFillTex);
            if (staminaFillTex != null) Destroy(staminaFillTex);
        }
    }
}
