using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DeadDawn.Data;
using DeadDawn.Player;
using DeadDawn.Core;

namespace DeadDawn.Crafting
{
    /// <summary>
    /// Interactive crafting bench inside the base station.
    /// Freezes game time and presents an ultra-crisp 9-sliced tactical Figma-style
    /// Crafting Recipe Modal with real-time requirement validation and zero pixel distortion.
    /// </summary>
    public class CraftingBench : MonoBehaviour
    {
        public static CraftingBench Instance { get; private set; }
        public static bool IsCraftingMenuOpen { get; private set; } = false;

        private static float closeTimestamp = -10f;
        public static bool WasRecentlyClosed => Time.unscaledTime < closeTimestamp + 0.35f;

        public bool IsPlayerNearby => isPlayerInRange;

        public bool IsMouseOverPrompt(Vector2 mouseGUI)
        {
            if (!isPlayerInRange || IsCraftingMenuOpen || DeadDawn.Core.ResourceStash.IsStashMenuOpen || DeadDawn.UI.DeathScreenUI.IsGameOver) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Vector3 worldPos = (benchCenter != null ? benchCenter.position : transform.position) + Vector3.up * 1.5f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0) return false;

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

        public static bool IsBenchPromptHovered(Vector2 mouseGUI)
        {
            return Instance != null && Instance.IsMouseOverPrompt(mouseGUI);
        }

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 2.8f;
        [SerializeField] private Transform benchCenter;

        [Header("Font Settings")]
        [SerializeField] private Font uiFont;

        private Transform playerTransform;
        private PlayerInventory playerInventory;
        private PlayerHealth playerHealth;
        private PlayerShooting playerShooting;

        private bool isPlayerInRange = false;
        private string statusMessage = "";
        private float statusMessageClearTime = 0f;

        // 9-Slice Textures
        private Texture2D darkOverlayTex;
        private Texture2D modalBgTex;
        private Texture2D cardReadyTex;
        private Texture2D cardLockedTex;
        private Texture2D cardHoverTex;
        private Texture2D innerPlateTex;
        private Texture2D btnReadyTex;
        private Texture2D btnReadyHoverTex;
        private Texture2D btnLockedTex;
        private Texture2D headerBadgeTex;
        private Texture2D inventoryBarTex;
        private Texture2D statusPillReadyTex;
        private Texture2D statusPillLockedTex;
        private Texture2D closeBtnTex;
        private Texture2D closeBtnHoverTex;
        private Texture2D promptBoxTex;

        // 9-Slice GUIStyles (Pixel-perfect border slicing, zero stretching)
        private GUIStyle modalBoxStyle;
        private GUIStyle cardReadyStyle;
        private GUIStyle cardLockedStyle;
        private GUIStyle cardHoverStyle;
        private GUIStyle innerPlateStyle;
        private GUIStyle btnReadyStyle;
        private GUIStyle btnReadyHoverStyle;
        private GUIStyle btnLockedStyle;
        private GUIStyle headerBadgeStyle;
        private GUIStyle inventoryBarStyle;
        private GUIStyle statusPillReadyStyle;
        private GUIStyle statusPillLockedStyle;
        private GUIStyle closeBtnBoxStyle;
        private GUIStyle closeBtnBoxHoverStyle;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptBoxHoverStyle;
        private GUIStyle promptKeyPillStyle;
        private GUIStyle promptPillTextStyle;

        // Text Styles
        private GUIStyle headerTitleStyle;
        private GUIStyle pauseBadgeTextStyle;
        private GUIStyle inventoryItemStyle;
        private GUIStyle cardStatusReadyTextStyle;
        private GUIStyle cardStatusLockedTextStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardSubtitleStyle;
        private GUIStyle reqHeaderStyle;
        private GUIStyle reqItemPassStyle;
        private GUIStyle reqItemFailStyle;
        private GUIStyle btnTextReadyStyle;
        private GUIStyle btnTextLockedStyle;
        private GUIStyle statusMsgStyle;
        private GUIStyle closeBtnTextStyle;
        private GUIStyle promptTitleStyle;
        private GUIStyle promptSubStyle;
        private GUIStyle emptyBtnStyle;
        private bool stylesInitialized = false;
        private float lastScale = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (benchCenter == null) benchCenter = transform;

            Init9SliceTextures();
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
                playerHealth = player.GetComponent<PlayerHealth>();
                playerShooting = player.GetComponent<PlayerShooting>();
            }
        }

        private void Init9SliceTextures()
        {
            darkOverlayTex = new Texture2D(1, 1);
            darkOverlayTex.SetPixel(0, 0, new Color(0.03f, 0.05f, 0.08f, 0.88f));
            darkOverlayTex.Apply();

            // Load Exported Figma Sprites from Assets/_Game/UI/Sprites/
            Texture2D figmaModalBg = LoadFigmaSprite("Slice_Workbench_Modal_BG.png");
            Texture2D figmaCardReady = LoadFigmaSprite("Slice_RecipeCard_Ready_BG.png");
            Texture2D figmaCardHover = LoadFigmaSprite("Slice_RecipeCard_Hover_BG.png");
            Texture2D figmaCardLocked = LoadFigmaSprite("Slice_RecipeCard_Locked_BG.png");
            Texture2D figmaMatPlate = LoadFigmaSprite("Slice_Materials_Plate.png");
            Texture2D figmaBtnReady = LoadFigmaSprite("Slice_Button_Green.png");
            Texture2D figmaBtnHover = LoadFigmaSprite("Slice_Button_Green_Hover.png");
            Texture2D figmaBtnLocked = LoadFigmaSprite("Slice_Button_Locked.png");
            Texture2D figmaPillReady = LoadFigmaSprite("Slice_StatusPill_Ready.png");
            Texture2D figmaPillLocked = LoadFigmaSprite("Slice_StatusPill_Locked.png");
            Texture2D figmaHeaderBadge = LoadFigmaSprite("Slice_Header_Badge.png");
            Texture2D figmaInvBar = LoadFigmaSprite("Slice_Inventory_Bar.png");

            // Modal Frame Style: 9-slice border 18px (160x160 with 16px radius)
            modalBgTex = figmaModalBg != null ? figmaModalBg : Create9SliceRoundedRect(64, 14,
                new Color(0.05f, 0.07f, 0.10f, 0.98f),
                new Color(0.19f, 0.23f, 0.28f, 0.95f), 2f);
            modalBoxStyle = Create9SliceStyle(modalBgTex, figmaModalBg != null ? 18 : 16);

            // Card Ready Style: 9-slice border 14px (128x128 with 12px radius)
            cardReadyTex = figmaCardReady != null ? figmaCardReady : Create9SliceRoundedRect(48, 10,
                new Color(0.09f, 0.11f, 0.14f, 0.98f),
                new Color(0.18f, 0.63f, 0.26f, 1.0f), 2f);
            cardReadyStyle = Create9SliceStyle(cardReadyTex, figmaCardReady != null ? 14 : 12);

            // Card Locked Style: 9-slice border 14px
            cardLockedTex = figmaCardLocked != null ? figmaCardLocked : Create9SliceRoundedRect(48, 10,
                new Color(0.05f, 0.07f, 0.09f, 0.98f),
                new Color(0.19f, 0.22f, 0.26f, 0.9f), 1.5f);
            cardLockedStyle = Create9SliceStyle(cardLockedTex, figmaCardLocked != null ? 14 : 12);

            // Card Hover Style: 9-slice border 14px
            cardHoverTex = figmaCardHover != null ? figmaCardHover : Create9SliceRoundedRect(48, 10,
                new Color(0.11f, 0.14f, 0.19f, 0.98f),
                new Color(0.35f, 0.65f, 1.0f, 1.0f), 2f);
            cardHoverStyle = Create9SliceStyle(cardHoverTex, figmaCardHover != null ? 14 : 12);

            // Inner Requirements Plate: 9-slice border 12px
            innerPlateTex = figmaMatPlate != null ? figmaMatPlate : Create9SliceRoundedRect(36, 6,
                new Color(0.04f, 0.05f, 0.07f, 0.98f),
                new Color(0.13f, 0.15f, 0.18f, 1.0f), 1f);
            innerPlateStyle = Create9SliceStyle(innerPlateTex, figmaMatPlate != null ? 12 : 10);

            // Action Button Ready: 9-slice border 10px
            btnReadyTex = figmaBtnReady != null ? figmaBtnReady : Create9SliceRoundedRect(36, 6,
                new Color(0.11f, 0.24f, 0.16f, 1.0f),
                new Color(0.18f, 0.63f, 0.26f, 1.0f), 1.5f);
            btnReadyStyle = Create9SliceStyle(btnReadyTex, figmaBtnReady != null ? 10 : 8);

            // Action Button Hover: 9-slice border 10px
            btnReadyHoverTex = figmaBtnHover != null ? figmaBtnHover : Create9SliceRoundedRect(36, 6,
                new Color(0.14f, 0.53f, 0.21f, 1.0f),
                new Color(0.25f, 0.73f, 0.31f, 1.0f), 2f);
            btnReadyHoverStyle = Create9SliceStyle(btnReadyHoverTex, figmaBtnHover != null ? 10 : 8);

            // Action Button Locked: 9-slice border 10px
            btnLockedTex = figmaBtnLocked != null ? figmaBtnLocked : Create9SliceRoundedRect(36, 6,
                new Color(0.07f, 0.09f, 0.12f, 0.85f),
                new Color(0.13f, 0.15f, 0.18f, 0.8f), 1f);
            btnLockedStyle = Create9SliceStyle(btnLockedTex, figmaBtnLocked != null ? 10 : 8);

            // Header Badge: 9-slice border 8px
            headerBadgeTex = figmaHeaderBadge != null ? figmaHeaderBadge : Create9SliceRoundedRect(36, 6,
                new Color(0.16f, 0.07f, 0.08f, 0.95f),
                new Color(0.85f, 0.21f, 0.20f, 0.85f), 1.5f);
            headerBadgeStyle = Create9SliceStyle(headerBadgeTex, figmaHeaderBadge != null ? 8 : 6);

            // Inventory Strip: 9-slice border 8px
            inventoryBarTex = figmaInvBar != null ? figmaInvBar : Create9SliceRoundedRect(36, 6,
                new Color(0.07f, 0.09f, 0.12f, 0.95f),
                new Color(0.13f, 0.15f, 0.18f, 0.9f), 1f);
            inventoryBarStyle = Create9SliceStyle(inventoryBarTex, figmaInvBar != null ? 8 : 6);

            // Status Pills: 9-slice border 8px
            statusPillReadyTex = figmaPillReady != null ? figmaPillReady : Create9SliceRoundedRect(24, 4,
                new Color(0.10f, 0.20f, 0.14f, 0.95f),
                new Color(0.18f, 0.63f, 0.26f, 1.0f), 1f);
            statusPillReadyStyle = Create9SliceStyle(statusPillReadyTex, figmaPillReady != null ? 8 : 6);

            statusPillLockedTex = figmaPillLocked != null ? figmaPillLocked : Create9SliceRoundedRect(24, 4,
                new Color(0.07f, 0.09f, 0.12f, 0.8f),
                new Color(0.19f, 0.22f, 0.26f, 0.8f), 1f);
            statusPillLockedStyle = Create9SliceStyle(statusPillLockedTex, figmaPillLocked != null ? 8 : 6);

            // Close Buttons
            closeBtnTex = figmaBtnLocked != null ? figmaBtnLocked : Create9SliceRoundedRect(36, 6,
                new Color(0.13f, 0.15f, 0.18f, 1.0f),
                new Color(0.19f, 0.22f, 0.26f, 1.0f), 1.5f);
            closeBtnBoxStyle = Create9SliceStyle(closeBtnTex, figmaBtnLocked != null ? 10 : 8);

            closeBtnHoverTex = Create9SliceRoundedRect(36, 6,
                new Color(0.19f, 0.22f, 0.26f, 1.0f),
                new Color(0.55f, 0.58f, 0.63f, 1.0f), 1.5f);
            closeBtnBoxHoverStyle = Create9SliceStyle(closeBtnHoverTex, 8);

            // Proximity Prompt: Figma 9-Slice Prompt Plates
            Texture2D figmaPromptReady = LoadFigmaSprite("Slice_Prompt_Ready_BG.png");
            Texture2D figmaPromptHover = LoadFigmaSprite("Slice_Prompt_Hover_BG.png");
            Texture2D figmaPromptKeyPill = LoadFigmaSprite("Slice_Prompt_KeyPill.png");

            promptBoxTex = figmaPromptReady != null ? figmaPromptReady : Create9SliceRoundedRect(40, 8,
                new Color(0.09f, 0.11f, 0.14f, 0.96f),
                new Color(0.18f, 0.63f, 0.26f, 1.0f), 2f);
            promptBoxStyle = Create9SliceStyle(promptBoxTex, figmaPromptReady != null ? 10 : 8);

            Texture2D promptBoxHoverTex = figmaPromptHover != null ? figmaPromptHover : Create9SliceRoundedRect(40, 8,
                new Color(0.12f, 0.15f, 0.20f, 0.98f),
                new Color(0.35f, 0.65f, 1.0f, 1.0f), 2f);
            promptBoxHoverStyle = Create9SliceStyle(promptBoxHoverTex, figmaPromptHover != null ? 10 : 8);

            Texture2D promptKeyPillTex = figmaPromptKeyPill != null ? figmaPromptKeyPill : Create9SliceRoundedRect(24, 4,
                new Color(0.10f, 0.20f, 0.14f, 0.95f),
                new Color(0.18f, 0.63f, 0.26f, 1.0f), 1f);
            promptKeyPillStyle = Create9SliceStyle(promptKeyPillTex, figmaPromptKeyPill != null ? 8 : 6);
        }

        private static void DrawStyle(GUIStyle style, Rect rect)
        {
            if (style != null && Event.current.type == EventType.Repaint)
            {
                style.Draw(rect, false, false, false, false);
            }
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
                Debug.LogWarning($"[CraftingBench] Could not load Figma sprite {filename}: {ex.Message}");
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

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 centerPos = benchCenter != null ? benchCenter.position : transform.position;
            Vector3 diff = playerTransform.position - centerPos;
            diff.y = 0f;
            float dist = diff.magnitude;
            isPlayerInRange = dist <= interactionDistance;

            // Auto close if player moves away
            if (!isPlayerInRange && IsCraftingMenuOpen)
            {
                CloseCraftingMenu();
            }

            // Keyboard Shortcuts
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (isPlayerInRange && !IsCraftingMenuOpen && !DeadDawn.Core.ResourceStash.IsStashMenuOpen)
                {
                    if (keyboard.eKey.wasPressedThisFrame)
                    {
                        OpenCraftingMenu();
                    }
                }
                else if (IsCraftingMenuOpen)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                    {
                        CloseCraftingMenu();
                    }
                }
            }

            // Status message timeout (unscaled time)
            if (!string.IsNullOrEmpty(statusMessage) && Time.unscaledTime > statusMessageClearTime)
            {
                statusMessage = "";
            }
        }

        private void QuickCraftSlot(int index)
        {
            var recipes = GetRecipes();
            if (recipes != null && index >= 0 && index < recipes.Count)
            {
                var r = recipes[index];
                if (CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(r))
                {
                    ExecuteCraft(r);
                }
            }
        }

        public void OpenCraftingMenu()
        {
            if (IsCraftingMenuOpen) return;

            UIInteractionBlocker.NotifyInteractionConsumed();
            IsCraftingMenuOpen = true;
            Time.timeScale = 0f; // Freeze game time: stops zombies, horde progression, world timers
            statusMessage = "";
        }

        public void CloseCraftingMenu()
        {
            if (!IsCraftingMenuOpen) return;

            UIInteractionBlocker.NotifyInteractionConsumed();
            IsCraftingMenuOpen = false;
            closeTimestamp = Time.unscaledTime;
            Time.timeScale = 1f; // Resume game time
        }

        private void OnDisable()
        {
            if (IsCraftingMenuOpen)
            {
                CloseCraftingMenu();
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

            headerTitleStyle = CreateTextStyle((int)(20 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            pauseBadgeTextStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.97f, 0.32f, 0.29f), TextAnchor.MiddleCenter);
            inventoryItemStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            cardStatusReadyTextStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleCenter);
            cardStatusLockedTextStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.55f, 0.58f, 0.63f), TextAnchor.MiddleCenter);

            cardTitleStyle = CreateTextStyle((int)(16 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            cardSubtitleStyle = CreateTextStyle((int)(11 * scale), FontStyle.Normal, new Color(0.55f, 0.58f, 0.63f), TextAnchor.MiddleCenter);

            reqHeaderStyle = CreateTextStyle((int)(10 * scale), FontStyle.Bold, new Color(0.55f, 0.58f, 0.63f), TextAnchor.MiddleLeft);
            reqItemPassStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleLeft);
            reqItemFailStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, new Color(0.97f, 0.32f, 0.29f), TextAnchor.MiddleLeft);

            btnTextReadyStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            btnTextLockedStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.55f, 0.58f, 0.63f), TextAnchor.MiddleCenter);

            statusMsgStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleCenter);
            closeBtnTextStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleCenter);

            promptTitleStyle = CreateTextStyle((int)(13 * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            promptSubStyle = CreateTextStyle((int)(11 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleLeft);
            promptPillTextStyle = CreateTextStyle((int)(12 * scale), FontStyle.Bold, new Color(0.25f, 0.73f, 0.31f), TextAnchor.MiddleCenter);

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

        private void OnGUI()
        {
            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);
            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            // 1. If player is near bench and menu is closed -> Show Tactical Button Pop-up
            if (isPlayerInRange && !IsCraftingMenuOpen && !DeadDawn.Core.ResourceStash.IsStashMenuOpen)
            {
                DrawBenchInteractionPrompt(screenW, screenH, scale);
            }

            // 2. If Crafting Menu is Open -> Draw Fullscreen Modal UI
            if (IsCraftingMenuOpen)
            {
                DrawCraftingMenu(screenW, screenH, scale);
            }
        }

        private void DrawBenchInteractionPrompt(float screenW, float screenH, float scale)
        {
            Camera cam = Camera.main;
            Vector3 worldPos = (benchCenter != null ? benchCenter.position : transform.position) + Vector3.up * 1.5f;

            float promptW = Mathf.Clamp(270f * scale, 230f, 320f);
            float promptH = 58f * scale;
            Rect promptRect;

            if (cam != null)
            {
                Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
                if (screenPoint.z > 0)
                {
                    float x = screenPoint.x - promptW * 0.5f;
                    float y = screenH - screenPoint.y - promptH * 0.5f;
                    promptRect = new Rect(Mathf.Clamp(x, 20f, screenW - promptW - 20f),
                                          Mathf.Clamp(y, 60f, screenH - promptH - 60f),
                                          promptW, promptH);
                }
                else
                {
                    promptRect = new Rect((screenW - promptW) * 0.5f, screenH - promptH - 40f, promptW, promptH);
                }
            }
            else
            {
                promptRect = new Rect((screenW - promptW) * 0.5f, screenH - promptH - 40f, promptW, promptH);
            }

            UIInteractionBlocker.RegisterInteractiveRect(promptRect);

            bool isHovered = promptRect.Contains(Event.current.mousePosition);
            GUIStyle bgStyle = (isHovered && promptBoxHoverStyle != null) ? promptBoxHoverStyle : promptBoxStyle;

            // 9-Sliced Background frame (Zero stretching)
            DrawStyle(bgStyle, promptRect);

            // Key pill
            float pillSize = 34f * scale;
            float pillX = promptRect.x + 12f * scale;
            float pillY = promptRect.y + (promptH - pillSize) * 0.5f;
            Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);
            DrawStyle(promptKeyPillStyle, pillRect);
            GUI.Label(pillRect, "[E]", promptPillTextStyle);

            // Text block
            float textX = pillX + pillSize + 10f * scale;
            float textW = promptW - (textX - promptRect.x) - 12f * scale;
            float titleY = promptRect.y + 10f * scale;
            float titleH = 18f * scale;
            float subY = titleY + titleH;
            float subH = 16f * scale;

            GUI.Label(new Rect(textX, titleY, textW, titleH), "BASE WORKBENCH", promptTitleStyle);
            GUI.Label(new Rect(textX, subY, textW, subH), "OPEN CRAFTING STATION", promptSubStyle);

            if (GUI.Button(promptRect, GUIContent.none, emptyBtnStyle))
            {
                OpenCraftingMenu();
            }
        }

        private void DrawCraftingMenu(float screenW, float screenH, float scale)
        {
            // Fullscreen Dimmer Overlay
            if (darkOverlayTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex, ScaleMode.StretchToFill);
            }

            // Recipe List
            List<SO_CraftingRecipe> recipes = GetRecipes();
            int recipeCount = recipes != null ? recipes.Count : 0;

            // Geometry calculations
            float cardW = Mathf.Clamp(270f * scale, 230f, 300f);
            float cardH = 340f * scale;
            float cardSpacing = 16f * scale;
            float sidePadding = 24f * scale;

            float totalCardsW = recipeCount > 0 ? (recipeCount * cardW + (recipeCount - 1) * cardSpacing) : 400f * scale;
            float winW = Mathf.Clamp(totalCardsW + sidePadding * 2f, 820f * scale, screenW - 32f);
            float winH = Mathf.Clamp(564f * scale, 480f, screenH - 32f);

            float winX = (screenW - winW) * 0.5f;
            float winY = (screenH - winH) * 0.5f;

            Rect modalRect = new Rect(winX, winY, winW, winH);
            DrawStyle(modalBoxStyle, modalRect);

            // Header Section: Title
            float headerY = winY + 18f * scale;
            GUI.Label(new Rect(winX, headerY, winW, 28f * scale), "BASE STATION WORKBENCH", headerTitleStyle);

            // Header Section: Time Frozen Badge (9-Sliced)
            float badgeW = Mathf.Clamp(420f * scale, 300f, winW - 60f);
            float badgeH = 24f * scale;
            float badgeX = winX + (winW - badgeW) * 0.5f;
            float badgeY = headerY + 30f * scale;
            Rect badgeRect = new Rect(badgeX, badgeY, badgeW, badgeH);

            DrawStyle(headerBadgeStyle, badgeRect);
            GUI.Label(badgeRect, "GAME TIME FROZEN - HOARD & ZOMBIES STOPPED", pauseBadgeTextStyle);

            // Inventory Summary Bar (9-Sliced)
            if (playerInventory == null) FindPlayer();
            int wood = playerInventory != null ? playerInventory.Wood : 0;
            int scrap = playerInventory != null ? playerInventory.Scrap : 0;
            int powder = playerInventory != null ? playerInventory.Gunpowder : 0;
            int barricades = playerInventory != null ? playerInventory.BarricadeCount : 0;
            int ammo = playerShooting != null ? playerShooting.CurrentAmmo : 0;
            float hp = playerHealth != null ? playerHealth.CurrentHealth : 100f;

            float invY = badgeY + badgeH + 10f * scale;
            float invH = 34f * scale;
            float invW = winW - 48f * scale;
            float invX = winX + 24f * scale;
            Rect invRect = new Rect(invX, invY, invW, invH);

            DrawStyle(inventoryBarStyle, invRect);

            // Evenly partitioned inventory columns (No emojis, no font-fallback kerning artifacts)
            float invItemW = invW / 6f;
            DrawInventoryItem(new Rect(invX, invY, invItemW, invH), "WOOD", wood, new Color(0.95f, 0.78f, 0.52f));
            DrawInventoryItem(new Rect(invX + invItemW, invY, invItemW, invH), "SCRAP", scrap, Color.white);
            DrawInventoryItem(new Rect(invX + invItemW * 2, invY, invItemW, invH), "POWDER", powder, new Color(1f, 0.65f, 0.2f));
            DrawInventoryItem(new Rect(invX + invItemW * 3, invY, invItemW, invH), "BARRICADES", barricades, new Color(0.45f, 0.75f, 1f));
            DrawInventoryItem(new Rect(invX + invItemW * 4, invY, invItemW, invH), "AMMO", ammo, new Color(0.94f, 0.85f, 0.3f));
            DrawInventoryItem(new Rect(invX + invItemW * 5, invY, invItemW, invH), "HEALTH", (int)hp, new Color(0.24f, 0.82f, 0.36f));

            // Recipe List Area (Side-by-Side Responsive Tactical Cards)
            float cardsAreaY = invY + invH + 14f * scale;

            if (recipeCount > 0)
            {
                float cardsStartX = winX + (winW - totalCardsW) * 0.5f;

                for (int i = 0; i < recipeCount; i++)
                {
                    float cx = cardsStartX + i * (cardW + cardSpacing);
                    DrawRecipeCard(recipes[i], cx, cardsAreaY, cardW, cardH, wood, scrap, powder, scale);
                }
            }
            else
            {
                GUI.Label(new Rect(winX, cardsAreaY + 40f * scale, winW, 30f * scale), "No crafting recipes available at this station.", cardSubtitleStyle);
            }

            // Footer Section: Status Message & Resume Button
            float footerY = cardsAreaY + cardH + 10f * scale;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUI.Label(new Rect(winX, footerY, winW, 20f * scale), statusMessage, statusMsgStyle);
            }

            float closeW = Mathf.Clamp(280f * scale, 220f, 360f);
            float closeH = 36f * scale;
            float closeX = winX + (winW - closeW) * 0.5f;
            float closeY = footerY + 22f * scale;
            Rect closeRect = new Rect(closeX, closeY, closeW, closeH);

            bool isCloseHover = closeRect.Contains(Event.current.mousePosition);
            GUIStyle closeStyleToUse = (isCloseHover && closeBtnBoxHoverStyle != null) ? closeBtnBoxHoverStyle : closeBtnBoxStyle;
            DrawStyle(closeStyleToUse, closeRect);

            GUI.Label(closeRect, "RESUME GAME  [ESC / E]", closeBtnTextStyle);

            if (GUI.Button(closeRect, GUIContent.none, emptyBtnStyle))
            {
                CloseCraftingMenu();
            }
        }

        private void DrawInventoryItem(Rect r, string label, int val, Color valColor)
        {
            GUIStyle st = new GUIStyle(inventoryItemStyle);
            st.normal.textColor = valColor;
            st.alignment = TextAnchor.MiddleCenter;
            GUI.Label(r, $"{label}: {val}", st);
        }

        private void DrawRecipeCard(SO_CraftingRecipe recipe, float x, float y, float w, float h,
                                    int currentWood, int currentScrap, int currentPowder, float scale)
        {
            if (recipe == null) return;

            bool hasWood = currentWood >= recipe.woodCost;
            bool hasScrap = currentScrap >= recipe.scrapCost;
            bool hasPowder = currentPowder >= recipe.gunpowderCost;
            bool canAfford = hasWood && hasScrap && hasPowder;

            Rect cardRect = new Rect(x, y, w, h);
            bool isCardHovered = cardRect.Contains(Event.current.mousePosition);

            // 9-Sliced Card Background (Zero stretching)
            GUIStyle cardStyleToUse = canAfford 
                ? (isCardHovered && cardHoverStyle != null ? cardHoverStyle : cardReadyStyle) 
                : cardLockedStyle;

            DrawStyle(cardStyleToUse, cardRect);

            float padX = 14f * scale;
            float contentW = w - padX * 2f;

            // 1. Status Pill Badge at Top (9-Sliced)
            float pillH = 26f * scale;
            float pillY = y + 14f * scale;
            Rect pillRect = new Rect(x + padX, pillY, contentW, pillH);

            GUIStyle pillStyleToUse = canAfford ? statusPillReadyStyle : statusPillLockedStyle;
            DrawStyle(pillStyleToUse, pillRect);

            string statusStr = canAfford ? "READY TO CRAFT" : "INSUFFICIENT MATERIALS";
            GUIStyle statusTextStyle = canAfford ? cardStatusReadyTextStyle : cardStatusLockedTextStyle;
            GUI.Label(pillRect, statusStr, statusTextStyle);

            // 2. Clean Recipe Title (Stripped of parenthetical clutter for AAA look)
            float titleY = pillY + pillH + 8f * scale;
            float titleH = 24f * scale;
            string cleanTitle = GetCleanRecipeTitle(recipe.recipeName);
            GUI.Label(new Rect(x + padX, titleY, contentW, titleH), cleanTitle, cardTitleStyle);

            // 3. Subtitle / Output Specs
            float subY = titleY + titleH + 2f * scale;
            float subH = 18f * scale;
            string subStr = GetRecipeSubtitle(recipe);
            GUI.Label(new Rect(x + padX, subY, contentW, subH), subStr, cardSubtitleStyle);

            // 4. Recessed Requirements Plate (9-Sliced)
            float reqY = subY + subH + 10f * scale;
            float reqH = 138f * scale;
            Rect reqRect = new Rect(x + padX, reqY, contentW, reqH);

            DrawStyle(innerPlateStyle, reqRect);

            float innerPadX = 12f * scale;
            float reqHeaderY = reqY + 10f * scale;
            GUI.Label(new Rect(x + padX + innerPadX, reqHeaderY, contentW - innerPadX * 2f, 16f * scale), "REQUIRED MATERIALS:", reqHeaderStyle);

            float lineY = reqHeaderY + 22f * scale;
            float lineH = 26f * scale;

            bool hasRequirements = false;

            if (recipe.woodCost > 0)
            {
                hasRequirements = true;
                string mark = hasWood ? "[OK]" : "[NEED]";
                GUIStyle st = hasWood ? reqItemPassStyle : reqItemFailStyle;
                GUI.Label(new Rect(x + padX + innerPadX, lineY, contentW - innerPadX * 2f, lineH), $"{mark} Wood: {currentWood} / {recipe.woodCost}", st);
                lineY += lineH;
            }

            if (recipe.scrapCost > 0)
            {
                hasRequirements = true;
                string mark = hasScrap ? "[OK]" : "[NEED]";
                GUIStyle st = hasScrap ? reqItemPassStyle : reqItemFailStyle;
                GUI.Label(new Rect(x + padX + innerPadX, lineY, contentW - innerPadX * 2f, lineH), $"{mark} Scrap: {currentScrap} / {recipe.scrapCost}", st);
                lineY += lineH;
            }

            if (recipe.gunpowderCost > 0)
            {
                hasRequirements = true;
                string mark = hasPowder ? "[OK]" : "[NEED]";
                GUIStyle st = hasPowder ? reqItemPassStyle : reqItemFailStyle;
                GUI.Label(new Rect(x + padX + innerPadX, lineY, contentW - innerPadX * 2f, lineH), $"{mark} Gunpowder: {currentPowder} / {recipe.gunpowderCost}", st);
                lineY += lineH;
            }

            if (!hasRequirements)
            {
                GUI.Label(new Rect(x + padX + innerPadX, lineY, contentW - innerPadX * 2f, lineH), "[OK] Free Assembly", reqItemPassStyle);
            }

            // 5. Action Button at Card Bottom (9-Sliced)
            float btnH = 44f * scale;
            float btnY = y + h - btnH - 14f * scale;
            Rect btnRect = new Rect(x + padX, btnY, contentW, btnH);

            bool isBtnHovered = btnRect.Contains(Event.current.mousePosition);
            GUIStyle btnBoxStyle = canAfford 
                ? (isBtnHovered && btnReadyHoverStyle != null ? btnReadyHoverStyle : btnReadyStyle) 
                : btnLockedStyle;

            DrawStyle(btnBoxStyle, btnRect);

            string btnLabel;
            GUIStyle btnTextStyle;

            if (canAfford)
            {
                btnLabel = $"CRAFT (+{recipe.resultQuantity})";
                btnTextStyle = btnTextReadyStyle;
            }
            else
            {
                if (!hasWood)
                {
                    btnLabel = $"NEED {recipe.woodCost - currentWood} WOOD";
                }
                else if (!hasScrap)
                {
                    btnLabel = $"NEED {recipe.scrapCost - currentScrap} SCRAP";
                }
                else if (!hasPowder)
                {
                    btnLabel = $"NEED {recipe.gunpowderCost - currentPowder} POWDER";
                }
                else
                {
                    btnLabel = "LACK MATERIALS";
                }
                btnTextStyle = btnTextLockedStyle;
            }

            GUI.Label(btnRect, btnLabel, btnTextStyle);

            // Button Click handling
            if (canAfford && GUI.Button(btnRect, GUIContent.none, emptyBtnStyle))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                ExecuteCraft(recipe);
            }
        }

        private string GetCleanRecipeTitle(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "ITEM";
            int idx = rawName.IndexOf('(');
            if (idx > 0)
            {
                return rawName.Substring(0, idx).Trim().ToUpper();
            }
            return rawName.Trim().ToUpper();
        }

        private string GetRecipeSubtitle(SO_CraftingRecipe recipe)
        {
            if (recipe == null) return "";
            switch (recipe.resultType)
            {
                case CraftResultType.Ammo:
                    return $"+{recipe.resultQuantity} Rounds (Pistol Ammo)";
                case CraftResultType.Barricade:
                    return $"+{recipe.resultQuantity} Fortified Barricade";
                case CraftResultType.RepairKit:
                    return "+30 Health Restoration";
                default:
                    return $"+{recipe.resultQuantity} Assembled Output";
            }
        }

        private void ExecuteCraft(SO_CraftingRecipe recipe)
        {
            if (CraftingSystem.Instance != null)
            {
                bool success = CraftingSystem.Instance.CraftRecipe(recipe);
                if (success)
                {
                    statusMessage = $"Successfully assembled {recipe.recipeName}!";
                    statusMessageClearTime = Time.unscaledTime + 3f;
                }
                else
                {
                    statusMessage = "Craft failed! Not enough resources.";
                    statusMessageClearTime = Time.unscaledTime + 3f;
                }
            }
        }

        private List<SO_CraftingRecipe> GetRecipes()
        {
            if (CraftingSystem.Instance != null)
            {
                return CraftingSystem.Instance.GetAvailableRecipes();
            }
            return new List<SO_CraftingRecipe>();
        }

        private void OnDestroy()
        {
            DestroyTexture(ref darkOverlayTex);
            DestroyTexture(ref modalBgTex);
            DestroyTexture(ref cardReadyTex);
            DestroyTexture(ref cardLockedTex);
            DestroyTexture(ref cardHoverTex);
            DestroyTexture(ref innerPlateTex);
            DestroyTexture(ref btnReadyTex);
            DestroyTexture(ref btnReadyHoverTex);
            DestroyTexture(ref btnLockedTex);
            DestroyTexture(ref headerBadgeTex);
            DestroyTexture(ref inventoryBarTex);
            DestroyTexture(ref statusPillReadyTex);
            DestroyTexture(ref statusPillLockedTex);
            DestroyTexture(ref closeBtnTex);
            DestroyTexture(ref closeBtnHoverTex);
            DestroyTexture(ref promptBoxTex);
        }

        private void DestroyTexture(ref Texture2D tex)
        {
            if (tex != null)
            {
                Destroy(tex);
                tex = null;
            }
        }
    }
}
