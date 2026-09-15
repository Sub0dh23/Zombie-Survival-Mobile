using UnityEngine;
using DeadDawn.Player;

namespace DeadDawn.Core
{
    /// <summary>
    /// Base Resource Stash chest located inside the shelter.
    /// Provides safe inventory deposit and storage that is protected from death loss.
    /// Freezes gameplay time while interacting with the storage chest.
    /// Uses Figma tactical dark UI 9-slice sprites.
    /// </summary>
    public class ResourceStash : MonoBehaviour
    {
        public static ResourceStash Instance { get; private set; }
        public static bool IsStashMenuOpen => isOpen;

        private static float closeTimestamp = -10f;
        public static bool WasRecentlyClosed => Time.unscaledTime < closeTimestamp + 0.35f;

        public bool IsMouseOverPrompt(Vector2 mouseGUI)
        {
            if (!isPlayerInRange || isOpen || DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen || DeadDawn.UI.DeathScreenUI.IsGameOver) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Vector3 worldPos = transform.position + Vector3.up * 1.5f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0f) return false;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            float promptW = Mathf.Clamp(270f * scale, 230f, 320f);
            float promptH = 58f * scale;
            float x = screenPoint.x - promptW * 0.5f;
            float y = screenH - screenPoint.y - promptH * 0.5f;

            Rect promptRect = new Rect(Mathf.Clamp(x, 20f, screenW - promptW - 20f),
                                       Mathf.Clamp(y, 60f, screenH - promptH - 60f),
                                       promptW, promptH);

            return promptRect.Contains(mouseGUI);
        }

        public static bool IsStashPromptHovered(Vector2 mouseGUI)
        {
            return Instance != null && Instance.IsMouseOverPrompt(mouseGUI);
        }

        [Header("Stash Inventory")]
        [SerializeField] private int stashWood = 0;
        [SerializeField] private int stashScrap = 0;
        [SerializeField] private int stashGunpowder = 0;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 2.5f;

        private static bool isOpen = false;
        private Transform playerTransform;
        private PlayerInventory playerInventory;
        private bool isPlayerInRange = false;

        public int StashWood => stashWood;
        public int StashScrap => stashScrap;
        public int StashGunpowder => stashGunpowder;
        public int TotalItemsStored => stashWood + stashScrap + stashGunpowder;

        // Visual Assets & 9-Slice Textures
        private Texture2D darkOverlayTex;
        private Texture2D modalBgTex;
        private Texture2D plateBgTex;
        private Texture2D rowBgTex;
        private Texture2D headerBadgeTex;
        private Texture2D btnGreenTex;
        private Texture2D btnGreenHoverTex;
        private Texture2D btnCyanTex;
        private Texture2D btnCyanHoverTex;
        private Texture2D btnMiniTex;
        private Texture2D btnMiniHoverTex;
        private Texture2D btnLockedTex;
        private Texture2D promptReadyTex;
        private Texture2D promptHoverTex;
        private Texture2D promptKeyPillTex;

        // GUIStyles
        private GUIStyle modalBgStyle;
        private GUIStyle plateBgStyle;
        private GUIStyle rowBgStyle;
        private GUIStyle headerBadgeStyle;
        private GUIStyle btnGreenStyle;
        private GUIStyle btnGreenHoverStyle;
        private GUIStyle btnCyanStyle;
        private GUIStyle btnCyanHoverStyle;
        private GUIStyle btnMiniStyle;
        private GUIStyle btnMiniHoverStyle;
        private GUIStyle btnLockedStyle;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptBoxHoverStyle;
        private GUIStyle promptKeyPillStyle;

        // Typography Styles
        private Font uiFont;
        private GUIStyle modalTitleStyle;
        private GUIStyle modalSubStyle;
        private GUIStyle badgeTextStyle;
        private GUIStyle colHeaderStyle;
        private GUIStyle colCountStyle;
        private GUIStyle rowNameStyle;
        private GUIStyle rowAmountStyle;
        private GUIStyle btnLabelStyle;
        private GUIStyle miniBtnLabelStyle;
        private GUIStyle promptTitleStyle;
        private GUIStyle promptSubStyle;
        private GUIStyle promptKeyStyle;
        private GUIStyle emptyBtnStyle;

        private bool stylesInitialized = false;
        private float lastScale = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitTextures();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }

        private void InitTextures()
        {
            darkOverlayTex = new Texture2D(1, 1);
            darkOverlayTex.SetPixel(0, 0, new Color(0.03f, 0.05f, 0.08f, 0.88f));
            darkOverlayTex.Apply();

            modalBgTex = LoadFigmaSprite("Slice_Workbench_Modal_BG.png");
            plateBgTex = LoadFigmaSprite("Slice_Materials_Plate.png");
            rowBgTex = LoadFigmaSprite("Slice_Stash_Row_BG.png");
            headerBadgeTex = LoadFigmaSprite("Slice_Header_Badge.png");
            btnGreenTex = LoadFigmaSprite("Slice_Button_Green.png");
            btnGreenHoverTex = LoadFigmaSprite("Slice_Button_Green_Hover.png");
            btnCyanTex = LoadFigmaSprite("Slice_Button_Cyan.png");
            btnCyanHoverTex = LoadFigmaSprite("Slice_Button_Cyan_Hover.png");
            btnMiniTex = LoadFigmaSprite("Slice_Button_Mini.png");
            btnMiniHoverTex = LoadFigmaSprite("Slice_Button_Mini_Hover.png");
            btnLockedTex = LoadFigmaSprite("Slice_Button_Locked.png");
            promptReadyTex = LoadFigmaSprite("Slice_Prompt_Ready_BG.png");
            promptHoverTex = LoadFigmaSprite("Slice_Prompt_Hover_BG.png");
            promptKeyPillTex = LoadFigmaSprite("Slice_Prompt_KeyPill.png");

            // Styles
            modalBgStyle = Create9SliceStyle(modalBgTex ?? Create9SliceRoundedRect(64, 10, new Color(0.06f, 0.08f, 0.11f, 0.98f), new Color(0.18f, 0.22f, 0.27f, 1.0f), 1.5f), 12);
            plateBgStyle = Create9SliceStyle(plateBgTex ?? Create9SliceRoundedRect(48, 8, new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.15f, 0.18f, 0.23f, 1.0f), 1f), 10);
            rowBgStyle = Create9SliceStyle(rowBgTex ?? Create9SliceRoundedRect(40, 6, new Color(0.09f, 0.12f, 0.16f, 0.95f), new Color(0.18f, 0.22f, 0.27f, 0.8f), 1f), 8);
            headerBadgeStyle = Create9SliceStyle(headerBadgeTex ?? Create9SliceRoundedRect(36, 6, new Color(0.16f, 0.07f, 0.08f, 0.95f), new Color(0.85f, 0.21f, 0.20f, 0.85f), 1.5f), 8);

            btnGreenStyle = Create9SliceStyle(btnGreenTex ?? Create9SliceRoundedRect(40, 8, new Color(0.14f, 0.44f, 0.22f, 1.0f), new Color(0.24f, 0.65f, 0.35f, 1.0f), 1.5f), 10);
            btnGreenHoverStyle = Create9SliceStyle(btnGreenHoverTex ?? Create9SliceRoundedRect(40, 8, new Color(0.18f, 0.55f, 0.28f, 1.0f), new Color(0.35f, 0.80f, 0.45f, 1.0f), 1.5f), 10);

            btnCyanStyle = Create9SliceStyle(btnCyanTex ?? Create9SliceRoundedRect(40, 8, new Color(0.11f, 0.35f, 0.46f, 1.0f), new Color(0.22f, 0.58f, 0.74f, 1.0f), 1.5f), 10);
            btnCyanHoverStyle = Create9SliceStyle(btnCyanHoverTex ?? Create9SliceRoundedRect(40, 8, new Color(0.14f, 0.45f, 0.58f, 1.0f), new Color(0.35f, 0.72f, 0.88f, 1.0f), 1.5f), 10);

            btnMiniStyle = Create9SliceStyle(btnMiniTex ?? Create9SliceRoundedRect(32, 6, new Color(0.13f, 0.17f, 0.23f, 1.0f), new Color(0.25f, 0.31f, 0.38f, 1.0f), 1f), 6);
            btnMiniHoverStyle = Create9SliceStyle(btnMiniHoverTex ?? Create9SliceRoundedRect(32, 6, new Color(0.18f, 0.23f, 0.30f, 1.0f), new Color(0.35f, 0.65f, 1.0f, 1.0f), 1f), 6);

            btnLockedStyle = Create9SliceStyle(btnLockedTex ?? Create9SliceRoundedRect(36, 6, new Color(0.13f, 0.15f, 0.18f, 1.0f), new Color(0.19f, 0.22f, 0.26f, 1.0f), 1.5f), 10);

            promptBoxStyle = Create9SliceStyle(promptReadyTex ?? Create9SliceRoundedRect(40, 8, new Color(0.09f, 0.11f, 0.14f, 0.96f), new Color(0.18f, 0.63f, 0.26f, 1.0f), 2f), 10);
            promptBoxHoverStyle = Create9SliceStyle(promptHoverTex ?? Create9SliceRoundedRect(40, 8, new Color(0.12f, 0.15f, 0.20f, 0.98f), new Color(0.35f, 0.65f, 1.0f, 1.0f), 2f), 10);
            promptKeyPillStyle = Create9SliceStyle(promptKeyPillTex ?? Create9SliceRoundedRect(24, 4, new Color(0.10f, 0.20f, 0.14f, 0.95f), new Color(0.18f, 0.63f, 0.26f, 1.0f), 1f), 8);
        }

        private static Texture2D LoadFigmaSprite(string filename)
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
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ResourceStash] Failed to load sprite {filename}: {ex.Message}");
            }
            return null;
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

        private static Texture2D Create9SliceRoundedRect(int size, int cornerRadius, Color fillColor, Color borderColor, float borderWidth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[size * size];

            float r = cornerRadius;
            float innerR = Mathf.Max(0.1f, r - borderWidth);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = x < r ? r : (x >= size - r ? size - 1 - r : x);
                    float cy = y < r ? r : (y >= size - r ? size - 1 - r : y);
                    float dx = x - cx;
                    float dy = y - cy;

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

            modalTitleStyle = CreateTextStyle((int)(18 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            modalSubStyle = CreateTextStyle((int)(11 * scale), FontStyle.Normal, new Color(0.55f, 0.58f, 0.63f), TextAnchor.MiddleLeft);
            badgeTextStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.97f, 0.32f, 0.29f), TextAnchor.MiddleCenter);

            colHeaderStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            colCountStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.35f, 0.65f, 1.0f), TextAnchor.MiddleRight);

            rowNameStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, new Color(0.9f, 0.92f, 0.95f), TextAnchor.MiddleLeft);
            rowAmountStyle = CreateTextStyle((int)(14 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

            btnLabelStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            miniBtnLabelStyle = CreateTextStyle((int)(10 * scale), FontStyle.Bold, new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleCenter);

            promptTitleStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            promptSubStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleLeft);
            promptKeyStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleCenter);

            emptyBtnStyle = new GUIStyle();
            stylesInitialized = true;
        }

        private GUIStyle CreateTextStyle(int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GUIStyle st = new GUIStyle();
            if (uiFont != null) st.font = uiFont;
            st.fontSize = Mathf.Clamp(fontSize, 9, 32);
            st.fontStyle = style;
            st.normal.textColor = color;
            st.alignment = align;
            return st;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            if (DeadDawn.UI.DeathScreenUI.IsGameOver)
            {
                if (isOpen) CloseStash();
                return;
            }

            if (DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen)
            {
                if (isOpen) CloseStash();
                return;
            }

            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0f;
            isPlayerInRange = diff.magnitude <= interactionDistance;

            if (!isPlayerInRange && isOpen)
            {
                CloseStash();
            }

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                // [F] key toggles stash when in range and not blocked by workbench
                if (isPlayerInRange && !DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen && keyboard.fKey.wasPressedThisFrame)
                {
                    if (isOpen) CloseStash();
                    else OpenStash();
                }
                else if (isOpen && (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
                {
                    CloseStash();
                }
            }
        }

        public void OpenStash()
        {
            UIInteractionBlocker.NotifyInteractionConsumed();
            isOpen = true;
            Time.timeScale = 0f;
        }

        public void CloseStash()
        {
            UIInteractionBlocker.NotifyInteractionConsumed();
            isOpen = false;
            closeTimestamp = Time.unscaledTime;
            Time.timeScale = 1f;
        }

        public void DepositAll()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory == null) return;

            int pWood = playerInventory.Wood;
            int pScrap = playerInventory.Scrap;
            int pPowder = playerInventory.Gunpowder;

            if (pWood > 0 || pScrap > 0 || pPowder > 0)
            {
                if (playerInventory.TryConsumeResources(pWood, pScrap, pPowder))
                {
                    stashWood += pWood;
                    stashScrap += pScrap;
                    stashGunpowder += pPowder;
                }
            }
        }

        public void WithdrawAll()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory == null) return;

            if (stashWood > 0)
            {
                playerInventory.AddResource(DeadDawn.Data.ResourceType.Wood, stashWood);
                stashWood = 0;
            }
            if (stashScrap > 0)
            {
                playerInventory.AddResource(DeadDawn.Data.ResourceType.Scrap, stashScrap);
                stashScrap = 0;
            }
            if (stashGunpowder > 0)
            {
                playerInventory.AddResource(DeadDawn.Data.ResourceType.Gunpowder, stashGunpowder);
                stashGunpowder = 0;
            }
        }

        public void TransferWoodToStash(int amount)
        {
            if (playerInventory == null) FindPlayer();
            int toTake = Mathf.Min(amount, playerInventory != null ? playerInventory.Wood : 0);
            if (toTake > 0 && playerInventory.TryConsumeResources(toTake, 0, 0))
            {
                stashWood += toTake;
            }
        }

        public void TransferAllWoodToStash()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory != null)
            {
                int count = playerInventory.Wood;
                if (count > 0 && playerInventory.TryConsumeResources(count, 0, 0))
                {
                    stashWood += count;
                }
            }
        }

        public void TransferWoodToPlayer(int amount)
        {
            int toTake = Mathf.Min(amount, stashWood);
            if (toTake > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    stashWood -= toTake;
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Wood, toTake);
                }
            }
        }

        public void TransferAllWoodToPlayer()
        {
            if (stashWood > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Wood, stashWood);
                    stashWood = 0;
                }
            }
        }

        public void TransferScrapToStash(int amount)
        {
            if (playerInventory == null) FindPlayer();
            int toTake = Mathf.Min(amount, playerInventory != null ? playerInventory.Scrap : 0);
            if (toTake > 0 && playerInventory.TryConsumeResources(0, toTake, 0))
            {
                stashScrap += toTake;
            }
        }

        public void TransferAllScrapToStash()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory != null)
            {
                int count = playerInventory.Scrap;
                if (count > 0 && playerInventory.TryConsumeResources(0, count, 0))
                {
                    stashScrap += count;
                }
            }
        }

        public void TransferScrapToPlayer(int amount)
        {
            int toTake = Mathf.Min(amount, stashScrap);
            if (toTake > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    stashScrap -= toTake;
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Scrap, toTake);
                }
            }
        }

        public void TransferAllScrapToPlayer()
        {
            if (stashScrap > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Scrap, stashScrap);
                    stashScrap = 0;
                }
            }
        }

        public void TransferPowderToStash(int amount)
        {
            if (playerInventory == null) FindPlayer();
            int toTake = Mathf.Min(amount, playerInventory != null ? playerInventory.Gunpowder : 0);
            if (toTake > 0 && playerInventory.TryConsumeResources(0, 0, toTake))
            {
                stashGunpowder += toTake;
            }
        }

        public void TransferAllPowderToStash()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory != null)
            {
                int count = playerInventory.Gunpowder;
                if (count > 0 && playerInventory.TryConsumeResources(0, 0, count))
                {
                    stashGunpowder += count;
                }
            }
        }

        public void TransferPowderToPlayer(int amount)
        {
            int toTake = Mathf.Min(amount, stashGunpowder);
            if (toTake > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    stashGunpowder -= toTake;
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Gunpowder, toTake);
                }
            }
        }

        public void TransferAllPowderToPlayer()
        {
            if (stashGunpowder > 0)
            {
                if (playerInventory == null) FindPlayer();
                if (playerInventory != null)
                {
                    playerInventory.AddResource(DeadDawn.Data.ResourceType.Gunpowder, stashGunpowder);
                    stashGunpowder = 0;
                }
            }
        }

        private void OnGUI()
        {
            if (DeadDawn.UI.DeathScreenUI.IsGameOver) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            if (!isOpen)
            {
                // World Prompt when player is in range and crafting bench is not open
                if (isPlayerInRange && !DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen)
                {
                    DrawStashInteractionPrompt(screenW, screenH, scale);
                }
                return;
            }

            // Full Modal Stash Interface
            DrawStashModal(screenW, screenH, scale);
        }

        private void DrawStashInteractionPrompt(float screenW, float screenH, float scale)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 1.5f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0f) return;

            float promptW = Mathf.Clamp(270f * scale, 230f, 320f);
            float promptH = 58f * scale;
            float x = screenPoint.x - promptW * 0.5f;
            float y = screenH - screenPoint.y - promptH * 0.5f;

            Rect promptRect = new Rect(Mathf.Clamp(x, 20f, screenW - promptW - 20f),
                                       Mathf.Clamp(y, 60f, screenH - promptH - 60f),
                                       promptW, promptH);
            UIInteractionBlocker.RegisterInteractiveRect(promptRect);

            Vector2 mousePos = Event.current.mousePosition;
            bool isHover = promptRect.Contains(mousePos);

            // Draw Prompt BG
            DrawStyle(isHover ? promptBoxHoverStyle : promptBoxStyle, promptRect);

            // Left Key Pill: [F]
            float pillW = 34f * scale;
            float pillH = 34f * scale;
            Rect pillRect = new Rect(promptRect.x + 12f * scale, promptRect.y + (promptH - pillH) * 0.5f, pillW, pillH);
            DrawStyle(promptKeyPillStyle, pillRect);
            GUI.Label(pillRect, "F", promptKeyStyle);

            // Label Container
            float textX = pillRect.xMax + 10f * scale;
            float textW = promptRect.xMax - textX - 8f * scale;
            Rect titleRect = new Rect(textX, promptRect.y + 10f * scale, textW, 18f * scale);
            Rect subRect = new Rect(textX, promptRect.y + 28f * scale, textW, 16f * scale);

            GUI.Label(titleRect, "BASE RESOURCE STASH", promptTitleStyle);
            GUI.Label(subRect, "OPEN SECURE STORAGE", promptSubStyle);

            // Proximity click target
            if (GUI.Button(promptRect, GUIContent.none, emptyBtnStyle))
            {
                OpenStash();
            }
        }

        private void DrawStashModal(float screenW, float screenH, float scale)
        {
            // Dark Backdrop Overlay
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float modalW = Mathf.Clamp(760f * scale, 640f, 880f);
            float modalH = Mathf.Clamp(540f * scale, 460f, 620f);
            float modalX = (screenW - modalW) * 0.5f;
            float modalY = (screenH - modalH) * 0.5f;
            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);

            // Modal Background
            DrawStyle(modalBgStyle, modalRect);

            // Header Section
            Rect titleRect = new Rect(modalX + 26f * scale, modalY + 20f * scale, modalW - 220f * scale, 24f * scale);
            Rect subRect = new Rect(modalX + 26f * scale, modalY + 44f * scale, modalW - 220f * scale, 18f * scale);
            GUI.Label(titleRect, "BASE RESOURCE STASH", modalTitleStyle);
            GUI.Label(subRect, "SECURE INVENTORY VAULT  •  ITEMS STORED HERE ARE PROTECTED ON DEATH", modalSubStyle);

            // Time-Frozen Badge
            float badgeW = 146f * scale;
            float badgeH = 32f * scale;
            Rect badgeRect = new Rect(modalX + modalW - badgeW - 26f * scale, modalY + 22f * scale, badgeW, badgeH);
            DrawStyle(headerBadgeStyle, badgeRect);
            GUI.Label(badgeRect, "TIME FROZEN", badgeTextStyle);

            if (playerInventory == null) FindPlayer();
            int pWood = playerInventory != null ? playerInventory.Wood : 0;
            int pScrap = playerInventory != null ? playerInventory.Scrap : 0;
            int pPowder = playerInventory != null ? playerInventory.Gunpowder : 0;
            int pTotal = pWood + pScrap + pPowder;

            // Two tactical columns
            float colPad = 26f * scale;
            float colGap = 16f * scale;
            float colW = (modalW - (colPad * 2f) - colGap) * 0.5f;
            float leftColX = modalX + colPad;
            float rightColX = leftColX + colW + colGap;
            float colY = modalY + 76f * scale;
            float colH = 286f * scale;

            Rect leftColRect = new Rect(leftColX, colY, colW, colH);
            Rect rightColRect = new Rect(rightColX, colY, colW, colH);

            // Column Background Plates
            DrawStyle(plateBgStyle, leftColRect);
            DrawStyle(plateBgStyle, rightColRect);

            // Column Headers
            float headerPad = 14f * scale;
            float headerH = 28f * scale;
            Rect leftHeaderRect = new Rect(leftColX + headerPad, colY + 12f * scale, colW - (headerPad * 2f), headerH);
            Rect rightHeaderRect = new Rect(rightColX + headerPad, colY + 12f * scale, colW - (headerPad * 2f), headerH);

            GUI.Label(leftHeaderRect, "BACKPACK (ON HAND)", colHeaderStyle);
            GUI.Label(leftHeaderRect, $"[ {pTotal} ITEMS ]", colCountStyle);

            GUI.Label(rightHeaderRect, "BASE STASH (VAULT)", colHeaderStyle);
            GUI.Label(rightHeaderRect, $"[ {TotalItemsStored} ITEMS ]", colCountStyle);

            // Resource Rows in Left Column (Player Backpack)
            float rowStartY = colY + 48f * scale;
            float rowH = 68f * scale;
            float rowSpacing = 8f * scale;
            float rowInnerPad = 12f * scale;
            float rowW = colW - (rowInnerPad * 2f);

            // Backpack Rows (Wood, Scrap, Gunpowder)
            DrawTransferRow(leftColX + rowInnerPad, rowStartY, rowW, rowH, scale,
                "WOOD", pWood, new Color(0.88f, 0.62f, 0.24f),
                "+5", () => TransferWoodToStash(5),
                "ALL", () => TransferAllWoodToStash());

            DrawTransferRow(leftColX + rowInnerPad, rowStartY + (rowH + rowSpacing), rowW, rowH, scale,
                "SCRAP METAL", pScrap, new Color(0.35f, 0.65f, 1.0f),
                "+5", () => TransferScrapToStash(5),
                "ALL", () => TransferAllScrapToStash());

            DrawTransferRow(leftColX + rowInnerPad, rowStartY + (rowH + rowSpacing) * 2f, rowW, rowH, scale,
                "GUNPOWDER", pPowder, new Color(0.94f, 0.53f, 0.24f),
                "+2", () => TransferPowderToStash(2),
                "ALL", () => TransferAllPowderToStash());

            // Stash Rows (Wood, Scrap, Gunpowder)
            DrawTransferRow(rightColX + rowInnerPad, rowStartY, rowW, rowH, scale,
                "WOOD", stashWood, new Color(0.88f, 0.62f, 0.24f),
                "TAKE 5", () => TransferWoodToPlayer(5),
                "ALL", () => TransferAllWoodToPlayer());

            DrawTransferRow(rightColX + rowInnerPad, rowStartY + (rowH + rowSpacing), rowW, rowH, scale,
                "SCRAP METAL", stashScrap, new Color(0.35f, 0.65f, 1.0f),
                "TAKE 5", () => TransferScrapToPlayer(5),
                "ALL", () => TransferAllScrapToPlayer());

            DrawTransferRow(rightColX + rowInnerPad, rowStartY + (rowH + rowSpacing) * 2f, rowW, rowH, scale,
                "GUNPOWDER", stashGunpowder, new Color(0.94f, 0.53f, 0.24f),
                "TAKE 2", () => TransferPowderToPlayer(2),
                "ALL", () => TransferAllPowderToPlayer());

            // Bulk Actions Footer Bar
            float bulkY = colY + colH + 14f * scale;
            float bulkH = 46f * scale;
            Rect depositAllRect = new Rect(leftColX, bulkY, colW, bulkH);
            Rect withdrawAllRect = new Rect(rightColX, bulkY, colW, bulkH);

            // Deposit All Button (Green)
            Vector2 mousePos = Event.current.mousePosition;
            bool canDeposit = pTotal > 0;
            bool depHover = depositAllRect.Contains(mousePos) && canDeposit;
            DrawStyle(canDeposit ? (depHover ? btnGreenHoverStyle : btnGreenStyle) : btnLockedStyle, depositAllRect);
            GUI.Label(depositAllRect, "DEPOSIT ALL INVENTORY", btnLabelStyle);
            if (canDeposit && GUI.Button(depositAllRect, GUIContent.none, emptyBtnStyle))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                DepositAll();
            }

            // Withdraw All Button (Cyan)
            bool canWithdraw = TotalItemsStored > 0;
            bool wdrHover = withdrawAllRect.Contains(mousePos) && canWithdraw;
            DrawStyle(canWithdraw ? (wdrHover ? btnCyanHoverStyle : btnCyanStyle) : btnLockedStyle, withdrawAllRect);
            GUI.Label(withdrawAllRect, "WITHDRAW ALL TO BACKPACK", btnLabelStyle);
            if (canWithdraw && GUI.Button(withdrawAllRect, GUIContent.none, emptyBtnStyle))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                WithdrawAll();
            }

            // Resume Game Button
            float closeY = bulkY + bulkH + 12f * scale;
            float closeW = modalW - (colPad * 2f);
            float closeH = 38f * scale;
            Rect closeRect = new Rect(leftColX, closeY, closeW, closeH);

            bool closeHover = closeRect.Contains(mousePos);
            DrawStyle(closeHover ? btnMiniHoverStyle : btnLockedStyle, closeRect);
            GUI.Label(closeRect, "RESUME GAME  [ ESC / F ]", btnLabelStyle);
            if (GUI.Button(closeRect, GUIContent.none, emptyBtnStyle))
            {
                CloseStash();
            }
        }

        private void DrawTransferRow(float x, float y, float w, float h, float scale,
            string itemName, int count, Color accentColor,
            string btn1Text, System.Action onBtn1,
            string btn2Text, System.Action onBtn2)
        {
            Rect rowRect = new Rect(x, y, w, h);
            DrawStyle(rowBgStyle, rowRect);

            // Item Name
            Rect nameRect = new Rect(x + 14f * scale, y + 10f * scale, 140f * scale, 18f * scale);
            GUI.Label(nameRect, itemName, rowNameStyle);

            // Count with colored quantity
            Rect amountRect = new Rect(x + 14f * scale, y + 32f * scale, 140f * scale, 24f * scale);
            Color oldColor = rowAmountStyle.normal.textColor;
            rowAmountStyle.normal.textColor = count > 0 ? accentColor : new Color(0.4f, 0.44f, 0.5f);
            GUI.Label(amountRect, $"x {count}", rowAmountStyle);
            rowAmountStyle.normal.textColor = oldColor;

            // Mini Action Buttons
            bool hasItems = count > 0;
            Vector2 mousePos = Event.current.mousePosition;

            float btnW = 56f * scale;
            float btnH = 28f * scale;
            float btnY = y + (h - btnH) * 0.5f;

            // Button 1 (e.g. +5 or TAKE 5)
            float btn1X = x + w - (btnW * 2f) - 18f * scale;
            Rect btn1Rect = new Rect(btn1X, btnY, btnW, btnH);
            bool btn1Hover = btn1Rect.Contains(mousePos) && hasItems;

            DrawStyle(hasItems ? (btn1Hover ? btnMiniHoverStyle : btnMiniStyle) : btnLockedStyle, btn1Rect);
            GUI.Label(btn1Rect, btn1Text, miniBtnLabelStyle);
            if (hasItems && GUI.Button(btn1Rect, GUIContent.none, emptyBtnStyle))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                onBtn1?.Invoke();
            }

            // Button 2 (ALL)
            float btn2X = btn1X + btnW + 6f * scale;
            Rect btn2Rect = new Rect(btn2X, btnY, btnW, btnH);
            bool btn2Hover = btn2Rect.Contains(mousePos) && hasItems;

            DrawStyle(hasItems ? (btn2Hover ? btnMiniHoverStyle : btnMiniStyle) : btnLockedStyle, btn2Rect);
            GUI.Label(btn2Rect, btn2Text, miniBtnLabelStyle);
            if (hasItems && GUI.Button(btn2Rect, GUIContent.none, emptyBtnStyle))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                onBtn2?.Invoke();
            }
        }
    }
}
