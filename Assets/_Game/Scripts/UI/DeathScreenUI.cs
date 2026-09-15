using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DeadDawn.Core;
using DeadDawn.Enemy;
using DeadDawn.Player;

namespace DeadDawn.UI
{
    /// <summary>
    /// Displays a high-contrast, AAA death screen using Figma-exported UI components.
    /// Features deep cinematic vignette, high-impact modal layout, run statistics, and instant retry actions.
    /// </summary>
    public class DeathScreenUI : MonoBehaviour
    {
        public static DeathScreenUI Instance { get; private set; }
        public static bool IsGameOver { get; private set; } = false;

        private int totalZombiesKilled = 0;
        private int daysSurvived = 1;
        private int baseLevelReached = 1;

        private int woodCollected = 0;
        private int scrapCollected = 0;
        private int powderCollected = 0;

        [Header("Exported Figma Death Screen Sprites")]
        [SerializeField] private Texture2D modalBgTex;
        [SerializeField] private Texture2D plateBgTex;
        [SerializeField] private Texture2D respawnBtnTex;
        [SerializeField] private Texture2D menuBtnTex;

        private Texture2D darkOverlayTexture;
        private Texture2D hoverOverlayTex;
        private Texture2D shadowTex;

        private GUIStyle modalBgStyle;
        private GUIStyle plateBgStyle;
        private GUIStyle shadowStyle;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueDaysStyle;
        private GUIStyle statValueKillsStyle;
        private GUIStyle statValueSalvageStyle;
        private GUIStyle statValueTierStyle;
        private GUIStyle emptyBtnStyle;

        private static Font uiFont;
        private bool stylesInitialized = false;
        private float lastScale = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            IsGameOver = false;

            // Generate 1x1 dark blood vignette texture
            darkOverlayTexture = new Texture2D(1, 1);
            darkOverlayTexture.SetPixel(0, 0, new Color(0.04f, 0.01f, 0.01f, 0.90f));
            darkOverlayTexture.Apply();

            // Hover overlay texture (subtle lighten)
            hoverOverlayTex = new Texture2D(1, 1);
            hoverOverlayTex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.16f));
            hoverOverlayTex.Apply();

            LoadFigmaTextures();
        }

        private void LoadFigmaTextures()
        {
            if (modalBgTex == null) modalBgTex = LoadSprite("Slice_Workbench_Modal_BG.png");
            if (plateBgTex == null) plateBgTex = LoadSprite("Slice_Materials_Plate.png");
            if (respawnBtnTex == null) respawnBtnTex = LoadSprite("Button_Respawn.png");
            if (menuBtnTex == null) menuBtnTex = LoadSprite("Button_MainMenu.png");
        }

        private Texture2D LoadSprite(string filename)
        {
            try
            {
                string path = Path.Combine(Application.dataPath, "_Game/UI/Sprites", filename);
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DeathScreenUI] Could not load sprite {filename}: {ex.Message}");
            }
            return null;
        }

        private static Texture2D Create9SliceRoundedRect(int size, int radius, Color fillColor, Color borderColor, float borderWidth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[size * size];

            float r = radius;
            float innerR = Mathf.Max(0f, radius - borderWidth);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    if (x < r) dx = r - x;
                    else if (x > size - 1 - r) dx = x - (size - 1 - r);

                    float dy = 0f;
                    if (y < r) dy = r - y;
                    else if (y > size - 1 - r) dy = y - (size - 1 - r);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float edgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));

                    Color pixel;
                    if (dist > r + 0.5f)
                    {
                        pixel = Color.clear;
                    }
                    else if (dist > r - 0.5f)
                    {
                        float alpha = Mathf.Clamp01(r + 0.5f - dist);
                        pixel = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha);
                    }
                    else if (dist >= innerR || edgeDist < borderWidth)
                    {
                        pixel = borderColor;
                    }
                    else
                    {
                        pixel = fillColor;
                    }

                    colors[y * size + x] = pixel;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static GUIStyle Create9SliceStyle(Texture2D tex, int border)
        {
            GUIStyle st = new GUIStyle();
            st.normal.background = tex;
            st.border = new RectOffset(border, border, border, border);
            return st;
        }

        private static void DrawStyle(GUIStyle style, Rect rect)
        {
            if (style != null && Event.current.type == EventType.Repaint)
            {
                style.Draw(rect, false, false, false, false);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<ZombieKilledEvent>(OnZombieKilled);
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<ZombieKilledEvent>(OnZombieKilled);
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnZombieKilled(ZombieKilledEvent evt)
        {
            totalZombiesKilled++;
        }

        private void OnInventoryChanged(InventoryChangedEvent evt)
        {
            woodCollected = Mathf.Max(woodCollected, evt.Wood);
            scrapCollected = Mathf.Max(scrapCollected, evt.Scrap);
            powderCollected = Mathf.Max(powderCollected, evt.Gunpowder);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.GameOver)
            {
                TriggerDeathScreen(evt.DayNumber);
            }
        }

        public void TriggerDeathScreen(int day)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            daysSurvived = day;

            if (BaseManager.Instance != null)
            {
                baseLevelReached = BaseManager.Instance.BaseLevel;
            }

            // Disable player movement & shooting
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                if (controller != null) controller.enabled = false;

                var shooting = player.GetComponent<PlayerShooting>();
                if (shooting != null) shooting.enabled = false;
            }

            StartCoroutine(SlowdownAndFreezeRoutine());
        }

        private IEnumerator SlowdownAndFreezeRoutine()
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(0.75f);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!IsGameOver) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)
                {
                    RestartGame();
                }
            }
        }

        private void InitStyles(float scale)
        {
            if (uiFont == null)
            {
#if UNITY_EDITOR
                uiFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
#endif
                if (uiFont == null)
                {
                    uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }

            if (modalBgTex == null)
            {
                modalBgTex = Create9SliceRoundedRect(64, 12, new Color(0.06f, 0.08f, 0.11f, 0.98f), new Color(0.70f, 0.20f, 0.20f, 0.9f), 1.5f);
            }
            if (plateBgTex == null)
            {
                plateBgTex = Create9SliceRoundedRect(48, 8, new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.16f, 0.19f, 0.24f, 1.0f), 1f);
            }
            if (shadowTex == null)
            {
                shadowTex = Create9SliceRoundedRect(64, 16, new Color(0f, 0f, 0f, 0.65f), Color.clear, 0f);
            }

            modalBgStyle = Create9SliceStyle(modalBgTex, 12);
            plateBgStyle = Create9SliceStyle(plateBgTex, 8);
            shadowStyle = Create9SliceStyle(shadowTex, 16);

            titleStyle = new GUIStyle();
            if (uiFont != null) titleStyle.font = uiFont;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = Mathf.Clamp((int)(22 * scale), 16, 28);
            titleStyle.normal.textColor = new Color(0.97f, 0.32f, 0.29f); // #F85149 crimson red
            titleStyle.alignment = TextAnchor.MiddleLeft;

            subtitleStyle = new GUIStyle();
            if (uiFont != null) subtitleStyle.font = uiFont;
            subtitleStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 15);
            subtitleStyle.normal.textColor = new Color(0.60f, 0.64f, 0.70f); // #8B949E muted tactical grey
            subtitleStyle.alignment = TextAnchor.MiddleLeft;

            statLabelStyle = new GUIStyle();
            if (uiFont != null) statLabelStyle.font = uiFont;
            statLabelStyle.fontStyle = FontStyle.Bold;
            statLabelStyle.fontSize = Mathf.Clamp((int)(10 * scale), 9, 13);
            statLabelStyle.normal.textColor = new Color(0.55f, 0.58f, 0.63f);
            statLabelStyle.alignment = TextAnchor.MiddleLeft;

            statValueDaysStyle = new GUIStyle();
            if (uiFont != null) statValueDaysStyle.font = uiFont;
            statValueDaysStyle.fontStyle = FontStyle.Bold;
            statValueDaysStyle.fontSize = Mathf.Clamp((int)(18 * scale), 14, 24);
            statValueDaysStyle.normal.textColor = Color.white;
            statValueDaysStyle.alignment = TextAnchor.MiddleLeft;

            statValueKillsStyle = new GUIStyle(statValueDaysStyle);
            statValueKillsStyle.normal.textColor = new Color(0.97f, 0.32f, 0.29f); // Crimson #F85149

            statValueSalvageStyle = new GUIStyle(statValueDaysStyle);
            statValueSalvageStyle.normal.textColor = new Color(0.89f, 0.70f, 0.25f); // Amber gold #E3B341

            statValueTierStyle = new GUIStyle(statValueDaysStyle);
            statValueTierStyle.normal.textColor = new Color(0.35f, 0.65f, 1.00f); // Cyan #58A6FF

            emptyBtnStyle = new GUIStyle();

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!IsGameOver) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            // 1. Fullscreen dark blood tinted backdrop
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTexture, ScaleMode.StretchToFill);

            // 2. Central Death Modal Box (880 x 256)
            float modalW = 880f * scale;
            float modalH = 256f * scale;
            float modalX = (screenW - modalW) * 0.5f;
            float modalY = (screenH - modalH) * 0.5f;

            // Crisp drop shadow underneath modal
            Rect shadowRect = new Rect(modalX - 6f * scale, modalY + 6f * scale, modalW + 12f * scale, modalH + 12f * scale);
            DrawStyle(shadowStyle, shadowRect);

            // Modal background plate
            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);
            DrawStyle(modalBgStyle, modalRect);

            // 3. Modal Header
            float headerX = modalX + 32f * scale;
            float headerY = modalY + 22f * scale;

            Rect titleRect = new Rect(headerX, headerY, modalW - 64f * scale, 28f * scale);
            GUI.Label(titleRect, "YOU WERE OVERRUN", titleStyle);

            Rect subRect = new Rect(headerX, headerY + 28f * scale, modalW - 64f * scale, 18f * scale);
            GUI.Label(subRect, "The outbreak claimed your outpost. Your survival log has been preserved.", subtitleStyle);

            // 4. Dynamic Stat Cards (4 columns)
            float cardsY = headerY + 54f * scale;
            float cardSpacing = 14f * scale;
            float totalCardsWidth = modalW - 64f * scale;
            float cardW = (totalCardsWidth - cardSpacing * 3f) / 4f;
            float cardH = 76f * scale;

            int totalSalvage = woodCollected + scrapCollected + powderCollected;

            // Stat definitions
            string[] statLabels = { "DAYS SURVIVED", "ZOMBIES ELIMINATED", "SALVAGE HARVESTED", "BASE LEVEL REACHED" };
            string[] statValues = { $"{daysSurvived} DAYS", $"{totalZombiesKilled} KILLS", $"{totalSalvage} UNITS", $"OUTPOST TIER {baseLevelReached}" };
            GUIStyle[] statStyles = { statValueDaysStyle, statValueKillsStyle, statValueSalvageStyle, statValueTierStyle };

            for (int i = 0; i < 4; i++)
            {
                float cardX = headerX + i * (cardW + cardSpacing);
                Rect cardRect = new Rect(cardX, cardsY, cardW, cardH);

                // Draw card background plate
                DrawStyle(plateBgStyle, cardRect);

                // Draw label
                Rect labelRect = new Rect(cardX + 14f * scale, cardsY + 10f * scale, cardW - 28f * scale, 16f * scale);
                GUI.Label(labelRect, statLabels[i], statLabelStyle);

                // Draw value
                Rect valueRect = new Rect(cardX + 14f * scale, cardsY + 30f * scale, cardW - 28f * scale, 34f * scale);
                GUI.Label(valueRect, statValues[i], statStyles[i]);
            }

            // 5. Interactive Buttons on the bottom bar
            float btnY = cardsY + cardH + 18f * scale;
            float btnH = 44f * scale;
            float respawnW = 270f * scale;
            float menuW = 210f * scale;
            float btnSpacing = 16f * scale;

            // Respawn Button
            Rect respawnRect = new Rect(headerX, btnY, respawnW, btnH);
            bool respawnHovered = respawnRect.Contains(Event.current.mousePosition);

            if (respawnBtnTex != null)
            {
                GUI.DrawTexture(respawnRect, respawnBtnTex, ScaleMode.StretchToFill);
                if (respawnHovered && hoverOverlayTex != null)
                {
                    GUI.DrawTexture(respawnRect, hoverOverlayTex, ScaleMode.StretchToFill);
                }
            }

            if (GUI.Button(respawnRect, GUIContent.none, emptyBtnStyle))
            {
                RestartGame();
            }

            // Main Menu Button
            Rect menuRect = new Rect(headerX + respawnW + btnSpacing, btnY, menuW, btnH);
            bool menuHovered = menuRect.Contains(Event.current.mousePosition);

            if (menuBtnTex != null)
            {
                GUI.DrawTexture(menuRect, menuBtnTex, ScaleMode.StretchToFill);
                if (menuHovered && hoverOverlayTex != null)
                {
                    GUI.DrawTexture(menuRect, hoverOverlayTex, ScaleMode.StretchToFill);
                }
            }

            if (GUI.Button(menuRect, GUIContent.none, emptyBtnStyle))
            {
                RestartGame();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDestroy()
        {
            if (darkOverlayTexture != null) Destroy(darkOverlayTexture);
            if (hoverOverlayTex != null) Destroy(hoverOverlayTex);
            if (shadowTex != null) Destroy(shadowTex);
        }
    }
}
