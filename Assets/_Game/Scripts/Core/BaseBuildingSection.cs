using System.Collections.Generic;
using UnityEngine;
using DeadDawn.Combat;
using DeadDawn.Player;
using DeadDawn.Crafting;
using UnityEngine.InputSystem;

namespace DeadDawn.Core
{
    public enum SectionState
    {
        Unbuilt,
        Built,
        Damaged
    }

    /// <summary>
    /// Represents a discrete, upgradeable structure section in the base (e.g. North Wall, Watchtower).
    /// Follows a mobile-friendly Gardenscapes-style fixed-plot upgrade model.
    /// </summary>
    public class BaseBuildingSection : MonoBehaviour, IDamageable
    {
        [Header("Section Identification")]
        [SerializeField] private string sectionId = "wall_north";
        [SerializeField] private string sectionName = "North Perimeter Wall";

        [Header("Upgrade Costs (Level 1)")]
        [SerializeField] private int woodCost = 5;
        [SerializeField] private int scrapCost = 0;
        [SerializeField] private int repairWoodCost = 2;

        [Header("Health & Durability")]
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float currentHealth;

        [Header("Visual Models")]
        [SerializeField] private GameObject unbuiltVisual;
        [SerializeField] private GameObject builtVisual;

        [Header("Interaction")]
        [SerializeField] private float interactionDistance = 2.6f;
        [SerializeField] private Transform interactionAnchor;

        private int currentLevel = 0;
        private Transform playerTransform;
        private PlayerInventory playerInventory;
        private bool isPlayerInRange = false;

        public string SectionId => sectionId;
        public string SectionName => sectionName;
        public int CurrentLevel => currentLevel;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsBuilt => currentLevel > 0;
        public bool IsDamaged => IsBuilt && currentHealth < maxHealth;

        public static readonly List<BaseBuildingSection> ActiveSections = new List<BaseBuildingSection>();

        private void OnEnable()
        {
            if (!ActiveSections.Contains(this)) ActiveSections.Add(this);
        }

        private void OnDisable()
        {
            ActiveSections.Remove(this);
        }

        public bool IsMouseOverPrompt(Vector2 mouseGUI)
        {
            if (!isPlayerInRange || Time.timeScale <= 0f || DeadDawn.UI.DeathScreenUI.IsGameOver) return false;
            if (DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Vector3 worldPos = (interactionAnchor != null ? interactionAnchor.position : transform.position) + Vector3.up * 1.6f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0) return false;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            float promptW = Mathf.Clamp(270f * scale, 230f, 320f);
            float promptH = 58f * scale;
            float x = Mathf.Clamp(screenPoint.x - promptW * 0.5f, 20f, screenW - promptW - 20f);
            float y = Mathf.Clamp(screenH - screenPoint.y - promptH * 0.5f, 50f, screenH - promptH - 50f);
            Rect promptRect = new Rect(x, y, promptW, promptH);

            return promptRect.Contains(mouseGUI);
        }

        public static bool IsAnySectionPromptHovered(Vector2 mouseGUI)
        {
            for (int i = 0; i < ActiveSections.Count; i++)
            {
                if (ActiveSections[i] != null && ActiveSections[i].IsMouseOverPrompt(mouseGUI))
                    return true;
            }
            return false;
        }

        private static Texture2D promptDefaultTex;
        private static Texture2D promptHoverTex;
        private static Texture2D promptReadyTex;
        private static Texture2D promptKeyPillTex;

        private static GUIStyle promptDefaultStyle;
        private static GUIStyle promptHoverStyle;
        private static GUIStyle promptReadyStyle;
        private static GUIStyle promptKeyPillStyle;

        private static GUIStyle promptTitleStyle;
        private static GUIStyle promptSubPassStyle;
        private static GUIStyle promptSubFailStyle;
        private static GUIStyle keyPillTextStyle;
        private static GUIStyle emptyBtnStyle;
        private static Font uiFont;
        private static bool stylesInitialized = false;
        private static float lastScale = -1f;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (interactionAnchor == null) interactionAnchor = transform;
            ApplyVisualState();
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

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Don't interact while crafting bench or stash menu is open
            if (CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen)
            {
                isPlayerInRange = false;
                return;
            }

            Vector3 anchorPos = interactionAnchor != null ? interactionAnchor.position : transform.position;
            Vector3 diff = playerTransform.position - anchorPos;
            diff.y = 0f;
            isPlayerInRange = diff.magnitude <= interactionDistance;

            if (isPlayerInRange)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (!IsBuilt)
                    {
                        TryBuildOrUpgrade();
                    }
                    else if (IsDamaged)
                    {
                        TryRepair();
                    }
                }
            }
        }

        public void SetLevel(int level)
        {
            currentLevel = level;
            currentHealth = maxHealth;
            ApplyVisualState();
        }

        public bool TryBuildOrUpgrade()
        {
            if (playerInventory == null) FindPlayer();
            if (playerInventory == null) return false;

            if (playerInventory.TryConsumeResources(woodCost, scrapCost, 0))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                currentLevel++;
                currentHealth = maxHealth;
                ApplyVisualState();

                // Trigger small squash & bounce animation
                StartCoroutine(BuildBounceAnimation());

                if (BaseManager.Instance != null)
                {
                    BaseManager.Instance.OnSectionUpdated(this);
                }

                return true;
            }

            return false;
        }

        public bool TryRepair()
        {
            if (!IsDamaged) return false;
            if (playerInventory == null) FindPlayer();
            if (playerInventory == null) return false;

            if (playerInventory.TryConsumeResources(repairWoodCost, 0, 0))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                currentHealth = maxHealth;
                ApplyVisualState();
                return true;
            }

            return false;
        }

        public void TakeDamage(float amount)
        {
            // Perimeter walls cannot be brought down; only the entrance door can be attacked and breached
            return;
        }

        private void ApplyVisualState()
        {
            if (unbuiltVisual != null) unbuiltVisual.SetActive(currentLevel == 0);
            if (builtVisual != null) builtVisual.SetActive(currentLevel > 0);
        }

        private System.Collections.IEnumerator BuildBounceAnimation()
        {
            if (builtVisual == null) yield break;

            Vector3 origScale = builtVisual.transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 4f;
                float scaleMod = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
                builtVisual.transform.localScale = origScale * scaleMod;
                yield return null;
            }
            builtVisual.transform.localScale = origScale;
        }

        private static Texture2D LoadSprite(string filename)
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
                Debug.LogWarning($"[BaseBuildingSection] Could not load sprite {filename}: {ex.Message}");
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

        private static void InitStyles(float scale)
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

            if (promptDefaultTex == null) promptDefaultTex = LoadSprite("Slice_Prompt_Default_BG.png");
            if (promptHoverTex == null) promptHoverTex = LoadSprite("Slice_Prompt_Hover_BG.png");
            if (promptReadyTex == null) promptReadyTex = LoadSprite("Slice_Prompt_Ready_BG.png");
            if (promptKeyPillTex == null) promptKeyPillTex = LoadSprite("Slice_Prompt_KeyPill.png");

            promptDefaultStyle = Create9SliceStyle(promptDefaultTex, 10);
            promptHoverStyle = Create9SliceStyle(promptHoverTex, 10);
            promptReadyStyle = Create9SliceStyle(promptReadyTex, 10);
            promptKeyPillStyle = Create9SliceStyle(promptKeyPillTex, 8);

            promptTitleStyle = new GUIStyle();
            if (uiFont != null) promptTitleStyle.font = uiFont;
            promptTitleStyle.fontSize = Mathf.Clamp((int)(13 * scale), 11, 18);
            promptTitleStyle.fontStyle = FontStyle.Bold;
            promptTitleStyle.normal.textColor = Color.white;
            promptTitleStyle.alignment = TextAnchor.MiddleLeft;

            promptSubPassStyle = new GUIStyle();
            if (uiFont != null) promptSubPassStyle.font = uiFont;
            promptSubPassStyle.fontSize = Mathf.Clamp((int)(11 * scale), 9, 16);
            promptSubPassStyle.fontStyle = FontStyle.Bold;
            promptSubPassStyle.normal.textColor = new Color(0.25f, 0.73f, 0.31f); // #3FB950
            promptSubPassStyle.alignment = TextAnchor.MiddleLeft;

            promptSubFailStyle = new GUIStyle();
            if (uiFont != null) promptSubFailStyle.font = uiFont;
            promptSubFailStyle.fontSize = Mathf.Clamp((int)(11 * scale), 9, 16);
            promptSubFailStyle.fontStyle = FontStyle.Bold;
            promptSubFailStyle.normal.textColor = new Color(0.97f, 0.32f, 0.29f); // #F85149
            promptSubFailStyle.alignment = TextAnchor.MiddleLeft;

            keyPillTextStyle = new GUIStyle();
            if (uiFont != null) keyPillTextStyle.font = uiFont;
            keyPillTextStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 16);
            keyPillTextStyle.fontStyle = FontStyle.Bold;
            keyPillTextStyle.normal.textColor = new Color(0.25f, 0.73f, 0.31f);
            keyPillTextStyle.alignment = TextAnchor.MiddleCenter;

            emptyBtnStyle = new GUIStyle();
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isPlayerInRange || CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen || DeadDawn.UI.DeathScreenUI.IsGameOver) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = (interactionAnchor != null ? interactionAnchor.position : transform.position) + Vector3.up * 1.6f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            float promptW = Mathf.Clamp(270f * scale, 230f, 320f);
            float promptH = 58f * scale;
            float x = Mathf.Clamp(screenPoint.x - promptW * 0.5f, 20f, screenW - promptW - 20f);
            float y = Mathf.Clamp(screenH - screenPoint.y - promptH * 0.5f, 50f, screenH - promptH - 50f);
            Rect promptRect = new Rect(x, y, promptW, promptH);
            UIInteractionBlocker.RegisterInteractiveRect(promptRect);

            if (playerInventory == null) FindPlayer();
            int currentWood = playerInventory != null ? playerInventory.Wood : 0;
            int currentScrap = playerInventory != null ? playerInventory.Scrap : 0;

            bool isHovered = promptRect.Contains(Event.current.mousePosition);

            // State 1: Unbuilt -> Show Build Prompt
            if (!IsBuilt)
            {
                bool canAfford = currentWood >= woodCost && currentScrap >= scrapCost;
                GUIStyle bgStyle = canAfford ? (isHovered && promptHoverStyle != null ? promptHoverStyle : promptReadyStyle) : promptDefaultStyle;
                DrawStyle(bgStyle, promptRect);

                // Key pill
                float pillSize = 34f * scale;
                float pillX = x + 12f * scale;
                float pillY = y + (promptH - pillSize) * 0.5f;
                Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);
                DrawStyle(promptKeyPillStyle, pillRect);
                GUI.Label(pillRect, "[E]", keyPillTextStyle);

                // Text block
                float textX = pillX + pillSize + 10f * scale;
                float textW = promptW - (textX - x) - 12f * scale;
                float titleY = y + 10f * scale;
                float titleH = 18f * scale;
                float subY = titleY + titleH;
                float subH = 16f * scale;

                GUI.Label(new Rect(textX, titleY, textW, titleH), $"BUILD {sectionName.ToUpper()}", promptTitleStyle);

                string costText = scrapCost > 0 ? $"{woodCost} WOOD • {scrapCost} SCRAP" : $"{woodCost} WOOD";
                string subText = canAfford ? $"COST: {costText}" : $"NEED: {costText}";
                GUIStyle subStyle = canAfford ? promptSubPassStyle : promptSubFailStyle;
                GUI.Label(new Rect(textX, subY, textW, subH), subText, subStyle);

                if (canAfford && GUI.Button(promptRect, GUIContent.none, emptyBtnStyle))
                {
                    TryBuildOrUpgrade();
                }
            }
            // State 2: Built & Damaged -> Show Repair Prompt
            else if (IsDamaged)
            {
                bool canAfford = currentWood >= repairWoodCost;
                GUIStyle bgStyle = canAfford ? (isHovered && promptHoverStyle != null ? promptHoverStyle : promptReadyStyle) : promptDefaultStyle;
                DrawStyle(bgStyle, promptRect);

                // Key pill
                float pillSize = 34f * scale;
                float pillX = x + 12f * scale;
                float pillY = y + (promptH - pillSize) * 0.5f;
                Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);
                DrawStyle(promptKeyPillStyle, pillRect);
                GUI.Label(pillRect, "[E]", keyPillTextStyle);

                // Text block
                float textX = pillX + pillSize + 10f * scale;
                float textW = promptW - (textX - x) - 12f * scale;
                float titleY = y + 10f * scale;
                float titleH = 18f * scale;
                float subY = titleY + titleH;
                float subH = 16f * scale;

                GUI.Label(new Rect(textX, titleY, textW, titleH), $"REPAIR {sectionName.ToUpper()}", promptTitleStyle);

                string subText = canAfford 
                    ? $"HP: {(int)currentHealth}/{(int)maxHealth}  •  {repairWoodCost} WOOD" 
                    : $"NEED {repairWoodCost} WOOD  (HP: {(int)currentHealth})";
                GUIStyle subStyle = canAfford ? promptSubPassStyle : promptSubFailStyle;
                GUI.Label(new Rect(textX, subY, textW, subH), subText, subStyle);

                if (canAfford && GUI.Button(promptRect, GUIContent.none, emptyBtnStyle))
                {
                    TryRepair();
                }
            }
            // State 3: Built & Intact -> Show Fortified Status (Gate section handled by BaseGate prompt)
            else
            {
                if (GetComponentInChildren<BaseGate>() != null) return;

                DrawStyle(promptDefaultStyle, promptRect);

                // Status pill
                float pillSize = 34f * scale;
                float pillX = x + 12f * scale;
                float pillY = y + (promptH - pillSize) * 0.5f;
                Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);
                DrawStyle(promptKeyPillStyle, pillRect);
                GUI.Label(pillRect, $"L{currentLevel}", keyPillTextStyle);

                // Text block
                float textX = pillX + pillSize + 10f * scale;
                float textW = promptW - (textX - x) - 12f * scale;
                float titleY = y + 10f * scale;
                float titleH = 18f * scale;
                float subY = titleY + titleH;
                float subH = 16f * scale;

                GUI.Label(new Rect(textX, titleY, textW, titleH), sectionName.ToUpper(), promptTitleStyle);
                GUI.Label(new Rect(textX, subY, textW, subH), $"FORTIFIED  •  HP: {(int)currentHealth}/{(int)maxHealth}", promptSubPassStyle);
            }
        }
    }
}
