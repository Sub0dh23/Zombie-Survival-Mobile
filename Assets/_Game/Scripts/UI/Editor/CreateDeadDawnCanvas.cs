#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DeadDawn.UI;
using UnityEditor.SceneManagement;

namespace DeadDawn.Editor
{
    public static class CreateDeadDawnCanvas
    {
        private const string SPRITES_PATH = "Assets/_Game/UI/Sprites/";
        private const string PREFAB_PATH = "Assets/_Game/UI/Prefabs/DeadDawn_HUD_Canvas.prefab";

        [MenuItem("Tools/DeadDawn/Build HUD Canvas from Figma Assets")]
        public static void BuildHUDCanvas()
        {
            // 1. Ensure EventSystem exists with new Input System module
            var existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            }
            else
            {
                var legacyModule = existingEventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacyModule != null)
                {
                    Undo.DestroyObjectImmediate(legacyModule);
                }
                if (existingEventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    existingEventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            // 2. Remove any previous DeadDawn_HUD_Canvas in scene
            var existingCanvas = GameObject.Find("DeadDawn_HUD_Canvas");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // Remove legacy HUD_Manager if present
            var legacyHud = GameObject.Find("HUD_Manager");
            if (legacyHud != null)
            {
                Undo.DestroyObjectImmediate(legacyHud);
            }

            // 3. Create Root Canvas
            var canvasGo = new GameObject("DeadDawn_HUD_Canvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create DeadDawn_HUD_Canvas");

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var hudManager = canvasGo.AddComponent<HUDManager>();

            // Load Sprites
            var waveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES_PATH + "HUD_Wave_Tracker.png");
            var salvageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES_PATH + "HUD_Resource_Salvage.png");
            var vitalsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES_PATH + "HUD_Player_Vitals.png");
            var vitalsCritSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES_PATH + "Variant_Critical_Low_Health.png");
            var weaponSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES_PATH + "HUD_Weapon_Station.png");

            // --- PANEL 1: Wave & Horde Tracker (Top Center) ---
            var waveGo = CreateImagePanel("Panel_WaveTracker", canvasGo.transform, waveSprite, new Vector2(520, 84));
            SetAnchors(waveGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20));

            var waveTitle = CreateTextMesh("Text_WaveTitle", waveGo.transform, "DAY 1 // SCAVENGE PHASE", 19, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(waveTitle.gameObject, new Vector2(0, 8), new Vector2(480, 26));

            var waveSub = CreateTextMesh("Text_WaveSub", waveGo.transform, "SUNLIGHT SECURE  •  78s REMAINING", 13, FontStyles.Bold, new Color(1f, 0.78f, 0.28f), TextAlignmentOptions.Center);
            SetRect(waveSub.gameObject, new Vector2(0, -18), new Vector2(480, 22));

            // --- PANEL 2: Salvage & Resources (Top Right) ---
            var salvageGo = CreateImagePanel("Panel_ResourceSalvage", canvasGo.transform, salvageSprite, new Vector2(340, 170));
            SetAnchors(salvageGo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -20));

            var woodText = CreateTextMesh("Text_Wood", salvageGo.transform, "140", 14, FontStyles.Bold, new Color(0.95f, 0.78f, 0.52f), TextAlignmentOptions.Center);
            SetRect(woodText.gameObject, new Vector2(69, 29), new Vector2(90, 20));

            var scrapText = CreateTextMesh("Text_Scrap", salvageGo.transform, "85", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(scrapText.gameObject, new Vector2(69, -1), new Vector2(90, 20));

            var powderText = CreateTextMesh("Text_Powder", salvageGo.transform, "24", 14, FontStyles.Bold, new Color(1f, 0.65f, 0.2f), TextAlignmentOptions.Center);
            SetRect(powderText.gameObject, new Vector2(69, -31), new Vector2(90, 20));

            var barricadeText = CreateTextMesh("Text_Barricades", salvageGo.transform, "4", 14, FontStyles.Bold, new Color(0.45f, 0.75f, 1f), TextAlignmentOptions.Center);
            SetRect(barricadeText.gameObject, new Vector2(69, -61), new Vector2(90, 20));

            // --- PANEL 3: Player Vitals & Health Bar (Bottom Left) ---
            var vitalsGo = CreateImagePanel("Panel_PlayerVitals", canvasGo.transform, vitalsSprite, new Vector2(440, 160));
            SetAnchors(vitalsGo, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 24));
            var vitalsImg = vitalsGo.GetComponent<Image>();

            // Health Slider
            var sliderGo = new GameObject("Slider_Health", typeof(RectTransform));
            sliderGo.transform.SetParent(vitalsGo.transform, false);
            var slider = sliderGo.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;
            SetRect(sliderGo, new Vector2(0, 20), new Vector2(386, 28));

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.20f, 0.82f, 0.36f, 0.95f);
            slider.fillRect = fillRt;

            var hpText = CreateTextMesh("Text_Health", vitalsGo.transform, "HEALTH: 100 / 100", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(hpText.gameObject, new Vector2(0, 20), new Vector2(360, 24));

            // --- PANEL 4: Weapon & Ammunition Station (Bottom Right) ---
            var weaponGo = CreateImagePanel("Panel_WeaponStation", canvasGo.transform, weaponSprite, new Vector2(400, 160));
            SetAnchors(weaponGo, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 24));

            var ammoText = CreateTextMesh("Text_AmmoNumbers", weaponGo.transform, "30 / 120", 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(ammoText.gameObject, new Vector2(91, 26), new Vector2(160, 56));

            // 4. Bind Serialized Fields on HUDManager via SerializedObject
            var so = new SerializedObject(hudManager);
            so.FindProperty("ammoText").objectReferenceValue = ammoText;
            so.FindProperty("healthText").objectReferenceValue = hpText;
            so.FindProperty("healthSlider").objectReferenceValue = slider;
            so.FindProperty("vitalsCardImage").objectReferenceValue = vitalsImg;
            so.FindProperty("normalVitalsSprite").objectReferenceValue = vitalsSprite;
            so.FindProperty("criticalVitalsSprite").objectReferenceValue = vitalsCritSprite;

            so.FindProperty("woodText").objectReferenceValue = woodText;
            so.FindProperty("scrapText").objectReferenceValue = scrapText;
            so.FindProperty("gunpowderText").objectReferenceValue = powderText;
            so.FindProperty("barricadeText").objectReferenceValue = barricadeText;

            so.FindProperty("waveTitleText").objectReferenceValue = waveTitle;
            so.FindProperty("waveSubText").objectReferenceValue = waveSub;
            so.ApplyModifiedProperties();

            // 5. Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGo, PREFAB_PATH);
            Debug.Log($"[DeadDawn] Saved clean HUD Canvas prefab to: {PREFAB_PATH}");

            // 6. Save active scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[DeadDawn] Clean HUD Canvas rebuilt and scene saved successfully!");
        }

        private static GameObject CreateImagePanel(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return go;
        }

        private static TextMeshProUGUI CreateTextMesh(string name, Transform parent, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
        }

        private static void SetRect(GameObject go, Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
#endif
