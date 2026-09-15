using System.Collections.Generic;
using UnityEngine;
using DeadDawn.Player;
using DeadDawn.Crafting;

namespace DeadDawn.Core
{
    /// <summary>
    /// Coordinates base progression, tracks fixed section upgrades, and handles base leveling (Level 1 -> Level 2).
    /// </summary>
    public class BaseManager : MonoBehaviour
    {
        public static BaseManager Instance { get; private set; }

        [Header("Base Progression")]
        [SerializeField] private int baseLevel = 1;

        [Header("Base Level 2 Upgrade Costs")]
        [SerializeField] private int level2WoodCost = 15;
        [SerializeField] private int level2ScrapCost = 10;

        [Header("Registered Base Sections")]
        [SerializeField] private List<BaseBuildingSection> sections = new List<BaseBuildingSection>();

        [Header("Level 2 Base Expansions")]
        [SerializeField] private BaseBuildingSection watchtowerSection;

        private PlayerInventory playerInventory;
        private PlayerController playerController;

        public int BaseLevel => baseLevel;
        public int TotalSections => sections.Count;
        public int BuiltSectionsCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < sections.Count; i++)
                {
                    if (sections[i] != null && sections[i].IsBuilt) count++;
                }
                return count;
            }
        }

        public bool AllSectionsBuilt => TotalSections > 0 && BuiltSectionsCount >= TotalSections;
        public bool AllPerimeterWallsBuilt
        {
            get
            {
                int built = 0;
                int total = 0;
                for (int i = 0; i < sections.Count; i++)
                {
                    if (sections[i] != null && sections[i] != watchtowerSection)
                    {
                        total++;
                        if (sections[i].IsBuilt) built++;
                    }
                }
                return total > 0 && built >= total;
            }
        }

        public bool IsMouseOverUpgradePrompt(Vector2 mouseGUI)
        {
            if (baseLevel != 1 || !AllPerimeterWallsBuilt) return false;
            if (CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen || DeadDawn.UI.DeathScreenUI.IsGameOver) return false;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            float barH = 34f * scale;
            float barY = 114f * scale;
            float upgW = Mathf.Clamp(360f * scale, 280f, 440f);
            float upgH = 42f * scale;
            float upgX = (screenW - upgW) * 0.5f;
            float upgY = barY + barH + 8f * scale;
            Rect upgRect = new Rect(upgX, upgY, upgW, upgH);

            return upgRect.Contains(mouseGUI);
        }

        public static bool IsUpgradePromptHovered(Vector2 mouseGUI)
        {
            return Instance != null && Instance.IsMouseOverUpgradePrompt(mouseGUI);
        }

        private Texture2D barTex;
        private Texture2D upgBtnTex;
        private Texture2D upgBtnLockedTex;
        private GUIStyle barStyle;
        private GUIStyle upgBtnReadyStyle;
        private GUIStyle upgBtnLockedStyle;
        private GUIStyle barTextStyle;
        private GUIStyle upgBtnTextStyle;
        private GUIStyle emptyBtnStyle;
        private Font uiFont;
        private bool stylesInitialized = false;
        private float lastScale = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            FindPlayer();
            AutoRegisterSections();

            if (watchtowerSection != null)
            {
                watchtowerSection.gameObject.SetActive(baseLevel >= 2);
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
                playerController = player.GetComponent<PlayerController>();
            }
        }

        public void RegisterSection(BaseBuildingSection section)
        {
            if (section != null && !sections.Contains(section))
            {
                sections.Add(section);
            }
        }

        public void AutoRegisterSections()
        {
            BaseBuildingSection[] found = FindObjectsByType<BaseBuildingSection>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                RegisterSection(found[i]);
            }
        }

        public void OnSectionUpdated(BaseBuildingSection section)
        {
            // Called whenever a section is built, upgraded, or damaged
        }

        public bool TryUpgradeBaseToLevel2()
        {
            if (baseLevel >= 2) return false;
            if (!AllPerimeterWallsBuilt) return false;

            if (playerInventory == null) FindPlayer();
            if (playerInventory == null) return false;

            if (playerInventory.TryConsumeResources(level2WoodCost, level2ScrapCost, 0))
            {
                UIInteractionBlocker.NotifyInteractionConsumed();
                baseLevel = 2;

                // Expand player boundaries if applicable
                if (playerController != null)
                {
                    playerController.ExpandBoundary(6f);
                }
                var isoCam = FindFirstObjectByType<DeadDawn.CameraControl.IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.ExpandMapBounds(6f);
                }
                if (DeadDawn.Fog.VisibilityBoundaryController.Instance != null)
                {
                    DeadDawn.Fog.VisibilityBoundaryController.Instance.ExpandVision(0.04f);
                }

                // Unlock Watchtower section
                if (watchtowerSection != null)
                {
                    watchtowerSection.gameObject.SetActive(true);
                    RegisterSection(watchtowerSection);
                }

                return true;
            }

            return false;
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
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BaseManager] Could not load sprite {filename}: {ex.Message}");
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

            if (barTex == null) barTex = LoadSprite("Slice_Base_Fortification_Bar.png");
            if (upgBtnTex == null) upgBtnTex = LoadSprite("Slice_Base_Upgrade_Button.png");
            if (upgBtnLockedTex == null) upgBtnLockedTex = LoadSprite("Slice_Button_Locked.png");

            barStyle = Create9SliceStyle(barTex, 10);
            upgBtnReadyStyle = Create9SliceStyle(upgBtnTex, 10);
            upgBtnLockedStyle = Create9SliceStyle(upgBtnLockedTex, 10);

            barTextStyle = new GUIStyle();
            if (uiFont != null) barTextStyle.font = uiFont;
            barTextStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 18);
            barTextStyle.fontStyle = FontStyle.Bold;
            barTextStyle.alignment = TextAnchor.MiddleCenter;
            barTextStyle.normal.textColor = Color.white;
            barTextStyle.richText = true;

            upgBtnTextStyle = new GUIStyle();
            if (uiFont != null) upgBtnTextStyle.font = uiFont;
            upgBtnTextStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 18);
            upgBtnTextStyle.fontStyle = FontStyle.Bold;
            upgBtnTextStyle.alignment = TextAnchor.MiddleCenter;
            upgBtnTextStyle.normal.textColor = Color.white;
            upgBtnTextStyle.richText = true;

            emptyBtnStyle = new GUIStyle();
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen || DeadDawn.UI.DeathScreenUI.IsGameOver) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            // Status Bar: Positioned cleanly below Wave Tracker (Wave Tracker ends at 104 * scale)
            float barW = Mathf.Clamp(380f * scale, 300f, 460f);
            float barH = 34f * scale;
            float barX = (screenW - barW) * 0.5f;
            float barY = 114f * scale;

            Rect barRect = new Rect(barX, barY, barW, barH);
            DrawStyle(barStyle, barRect);

            string fortColor = BuiltSectionsCount >= TotalSections && TotalSections > 0 ? "#3FB950" : (BuiltSectionsCount > 0 ? "#E6EDF3" : "#8B949E");
            string status = $"<color=#E3B341>BASE LVL {baseLevel}</color>  <color=#484F58>•</color>  <color={fortColor}>FORTIFICATIONS: {BuiltSectionsCount} / {TotalSections}</color>";
            GUI.Label(barRect, status, barTextStyle);

            // If Level 1 is maxed out -> Prompt Base Level 2 Upgrade!
            if (baseLevel == 1 && AllPerimeterWallsBuilt)
            {
                if (playerInventory == null) FindPlayer();
                int wood = playerInventory != null ? playerInventory.Wood : 0;
                int scrap = playerInventory != null ? playerInventory.Scrap : 0;

                bool canAfford = wood >= level2WoodCost && scrap >= level2ScrapCost;
                float upgW = Mathf.Clamp(360f * scale, 280f, 440f);
                float upgH = 42f * scale;
                float upgX = (screenW - upgW) * 0.5f;
                float upgY = barY + barH + 8f * scale;
                Rect upgRect = new Rect(upgX, upgY, upgW, upgH);
                UIInteractionBlocker.RegisterInteractiveRect(upgRect);

                DrawStyle(canAfford ? upgBtnReadyStyle : upgBtnLockedStyle, upgRect);

                string costColor = canAfford ? "#3FB950" : "#F85149";
                string btnText = $"<color=#FFFFFF>UPGRADE BASE // LEVEL 2</color>\n<size={(int)(10 * scale)}><color={costColor}>[Cost: {level2WoodCost} Wood • {level2ScrapCost} Scrap]</color></size>";
                GUI.Label(upgRect, btnText, upgBtnTextStyle);

                if (canAfford && GUI.Button(upgRect, GUIContent.none, emptyBtnStyle))
                {
                    TryUpgradeBaseToLevel2();
                }
            }
        }
    }
}
