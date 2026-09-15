using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using DeadDawn.Combat;
using DeadDawn.Enemy;

namespace DeadDawn.Core
{
    /// <summary>
    /// Interactive gate for the base perimeter.
    /// Can be opened/closed by the player. When closed, it blocks physics and NavMesh pathing,
    /// and can be attacked by zombies as an obstacle.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BaseGate : MonoBehaviour, IDamageable
    {
        [Header("Gate Settings")]
        [SerializeField] private float maxHealth = 250f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private float interactionDistance = 3.2f;
        [SerializeField] private Transform doorLeaf; // The swinging door mesh

        [Header("Door Rotation")]
        [SerializeField] private float closedAngle = 0f;
        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float swingDuration = 0.35f;

        private BoxCollider gateCollider;
        private NavMeshObstacle navObstacle;
        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private Coroutine swingCoroutine;

        public bool IsOpen => isOpen;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public static readonly List<BaseGate> ActiveGates = new List<BaseGate>();

        private void OnEnable()
        {
            if (!ActiveGates.Contains(this)) ActiveGates.Add(this);
        }

        private void OnDisable()
        {
            ActiveGates.Remove(this);
        }

        // Figma-style UI Textures and Styles
        private static Texture2D promptReadyTex;
        private static Texture2D promptHoverTex;
        private static Texture2D promptDefaultTex;
        private static Texture2D promptKeyPillTex;

        private static GUIStyle promptReadyStyle;
        private static GUIStyle promptHoverStyle;
        private static GUIStyle promptDefaultStyle;
        private static GUIStyle promptKeyPillStyle;

        private static GUIStyle promptTitleStyle;
        private static GUIStyle promptSubPassStyle;
        private static GUIStyle promptSubWarnStyle;
        private static GUIStyle promptKeyPillTextStyle;
        private static GUIStyle emptyBtnStyle;

        private static Font uiFont;
        private static bool stylesInitialized = false;
        private static float lastScale = -1f;

        private void Awake()
        {
            currentHealth = maxHealth;
            gateCollider = GetComponent<BoxCollider>();
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null)
            {
                navObstacle = gameObject.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
                navObstacle.shape = NavMeshObstacleShape.Box;
                if (gateCollider != null)
                {
                    navObstacle.size = gateCollider.size;
                    navObstacle.center = gateCollider.center;
                }
            }

            ApplyGateStateImmediate();
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
            }
        }

        public Vector3 GetGatewayCenter()
        {
            if (transform.parent != null)
            {
                return transform.parent.position;
            }
            return transform.position + transform.right * 1.22f;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            if (Time.timeScale <= 0f)
            {
                isPlayerInRange = false;
                return;
            }

            Vector3 gateCenter = GetGatewayCenter();
            Vector3 diff = playerTransform.position - gateCenter;
            diff.y = 0f;
            isPlayerInRange = diff.magnitude <= interactionDistance;

            // Keyboard shortcut [G] to toggle gate
            if (isPlayerInRange && (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame))
            {
                ToggleGate();
            }
        }

        public void ToggleGate()
        {
            UIInteractionBlocker.NotifyInteractionConsumed();
            SetGateState(!isOpen);
        }

        public bool IsMouseOverPrompt(Vector2 mouseGUI)
        {
            if (!isPlayerInRange || Time.timeScale <= 0f || DeadDawn.UI.DeathScreenUI.IsGameOver) return false;
            if (DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            float screenH = Screen.height;
            float screenW = Screen.width;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            Vector3 worldPos = GetGatewayCenter() + Vector3.up * 1.7f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0f) return false;

            float promptW = Mathf.Clamp(260f * scale, 220f, 310f);
            float promptH = 54f * scale;
            float x = Mathf.Clamp(screenPoint.x - promptW * 0.5f, 20f, screenW - promptW - 20f);
            float y = Mathf.Clamp(screenH - screenPoint.y - promptH * 0.5f, 50f, screenH - promptH - 50f);
            Rect promptRect = new Rect(x, y, promptW, promptH);

            return promptRect.Contains(mouseGUI);
        }

        public static bool IsAnyGatePromptHovered(Vector2 mouseGUI)
        {
            for (int i = 0; i < ActiveGates.Count; i++)
            {
                if (ActiveGates[i] != null && ActiveGates[i].IsMouseOverPrompt(mouseGUI))
                    return true;
            }
            return false;
        }

        public void SetGateState(bool open)
        {
            isOpen = open;

            if (!isOpen && currentHealth <= 0f)
            {
                currentHealth = maxHealth * 0.5f; // Re-securing a broken gate restores half HP
            }

            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(AnimateDoorSwing(isOpen ? openAngle : closedAngle));

            // When open, disable collision and path obstacle so player/zombies can pass
            if (gateCollider != null) gateCollider.enabled = !isOpen;
            if (navObstacle != null) navObstacle.enabled = !isOpen;
        }

        public void Repair(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        public void ApplyGateStateImmediate()
        {
            float targetAngle = isOpen ? openAngle : closedAngle;
            if (doorLeaf != null)
            {
                doorLeaf.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
            if (gateCollider != null) gateCollider.enabled = !isOpen;
            if (navObstacle != null) navObstacle.enabled = !isOpen;
        }

        private IEnumerator AnimateDoorSwing(float targetYAngle)
        {
            if (doorLeaf == null) yield break;

            Quaternion startRot = doorLeaf.localRotation;
            Quaternion targetRot = Quaternion.Euler(0f, targetYAngle, 0f);
            float elapsed = 0f;

            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / swingDuration);
                doorLeaf.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            doorLeaf.localRotation = targetRot;
        }

        public void TakeDamage(float amount)
        {
            if (isOpen) return; // Can't damage open gate

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Vector3 popupPos = GetGatewayCenter() + Vector3.up * 1.2f;
            DamagePopup.Spawn(popupPos, amount, new Color(1f, 0.6f, 0.2f));

            if (currentHealth <= 0f)
            {
                // Gate knocked open/broken by zombies
                SetGateState(true);
            }
        }

        private static Texture2D LoadSprite(string filename)
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
                Debug.LogWarning($"[BaseGate] Could not load sprite {filename}: {ex.Message}");
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

            if (promptReadyTex == null) promptReadyTex = LoadSprite("Slice_Prompt_Ready_BG.png");
            if (promptHoverTex == null) promptHoverTex = LoadSprite("Slice_Prompt_Hover_BG.png");
            if (promptDefaultTex == null) promptDefaultTex = LoadSprite("Slice_Prompt_Default_BG.png");
            if (promptKeyPillTex == null) promptKeyPillTex = LoadSprite("Slice_Prompt_KeyPill.png");

            // Fallbacks if sprites are missing
            if (promptReadyTex == null) promptReadyTex = Create9SliceRoundedRect(40, 8, new Color(0.09f, 0.11f, 0.14f, 0.96f), new Color(0.18f, 0.63f, 0.26f, 1.0f), 2f);
            if (promptHoverTex == null) promptHoverTex = Create9SliceRoundedRect(40, 8, new Color(0.12f, 0.15f, 0.20f, 0.98f), new Color(0.35f, 0.65f, 1.0f, 1.0f), 2f);
            if (promptDefaultTex == null) promptDefaultTex = Create9SliceRoundedRect(40, 8, new Color(0.09f, 0.11f, 0.14f, 0.96f), new Color(0.24f, 0.27f, 0.32f, 1.0f), 2f);
            if (promptKeyPillTex == null) promptKeyPillTex = Create9SliceRoundedRect(24, 4, new Color(0.10f, 0.20f, 0.14f, 0.95f), new Color(0.18f, 0.63f, 0.26f, 1.0f), 1f);

            promptReadyStyle = Create9SliceStyle(promptReadyTex, 10);
            promptHoverStyle = Create9SliceStyle(promptHoverTex, 10);
            promptDefaultStyle = Create9SliceStyle(promptDefaultTex, 10);
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

            promptSubWarnStyle = new GUIStyle();
            if (uiFont != null) promptSubWarnStyle.font = uiFont;
            promptSubWarnStyle.fontSize = Mathf.Clamp((int)(11 * scale), 9, 16);
            promptSubWarnStyle.fontStyle = FontStyle.Bold;
            promptSubWarnStyle.normal.textColor = new Color(0.97f, 0.60f, 0.20f); // Amber warn
            promptSubWarnStyle.alignment = TextAnchor.MiddleLeft;

            promptKeyPillTextStyle = new GUIStyle();
            if (uiFont != null) promptKeyPillTextStyle.font = uiFont;
            promptKeyPillTextStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 16);
            promptKeyPillTextStyle.fontStyle = FontStyle.Bold;
            promptKeyPillTextStyle.normal.textColor = new Color(0.25f, 0.73f, 0.31f);
            promptKeyPillTextStyle.alignment = TextAnchor.MiddleCenter;

            emptyBtnStyle = new GUIStyle();
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isPlayerInRange || Time.timeScale <= 0f || DeadDawn.UI.DeathScreenUI.IsGameOver) return;
            if (DeadDawn.Crafting.CraftingBench.IsCraftingMenuOpen || ResourceStash.IsStashMenuOpen) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            // Gateway center in world space
            Vector3 worldPos = GetGatewayCenter() + Vector3.up * 1.7f;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (screenPoint.z <= 0f) return;

            float promptW = Mathf.Clamp(260f * scale, 220f, 310f);
            float promptH = 54f * scale;
            float x = Mathf.Clamp(screenPoint.x - promptW * 0.5f, 20f, screenW - promptW - 20f);
            float y = Mathf.Clamp(screenH - screenPoint.y - promptH * 0.5f, 50f, screenH - promptH - 50f);
            Rect promptRect = new Rect(x, y, promptW, promptH);
            UIInteractionBlocker.RegisterInteractiveRect(promptRect);

            bool isHovered = promptRect.Contains(Event.current.mousePosition);

            // Choose background based on gate state & hover
            GUIStyle bgStyle;
            if (isHovered && promptHoverStyle != null)
            {
                bgStyle = promptHoverStyle;
            }
            else if (!isOpen && promptReadyStyle != null)
            {
                bgStyle = promptReadyStyle;
            }
            else
            {
                bgStyle = promptDefaultStyle ?? promptReadyStyle;
            }

            DrawStyle(bgStyle, promptRect);

            // Key pill [G]
            float pillSize = 34f * scale;
            float pillX = x + 10f * scale;
            float pillY = y + (promptH - pillSize) * 0.5f;
            Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);
            DrawStyle(promptKeyPillStyle, pillRect);

            if (promptKeyPillTextStyle != null)
            {
                promptKeyPillTextStyle.normal.textColor = isOpen ? new Color(0.97f, 0.60f, 0.20f) : new Color(0.25f, 0.73f, 0.31f);
                GUI.Label(pillRect, "[G]", promptKeyPillTextStyle);
            }

            // Text block
            float textX = pillX + pillSize + 10f * scale;
            float textW = promptW - (textX - x) - 10f * scale;
            float titleY = y + 8f * scale;
            float titleH = 18f * scale;
            float subY = titleY + titleH;
            float subH = 16f * scale;

            string titleText = isOpen ? "PERIMETER GATE // OPEN" : "PERIMETER GATE // SECURED";
            GUI.Label(new Rect(textX, titleY, textW, titleH), titleText, promptTitleStyle);

            string subText = isOpen
                ? "CLICK OR [G] TO CLOSE"
                : $"CLICK OR [G] TO OPEN  •  HP: {(int)currentHealth}/{(int)maxHealth}";
            GUIStyle subStyle = isOpen ? promptSubWarnStyle : promptSubPassStyle;
            GUI.Label(new Rect(textX, subY, textW, subH), subText, subStyle);

            // Click button to toggle gate
            if (GUI.Button(promptRect, GUIContent.none, emptyBtnStyle))
            {
                ToggleGate();
            }
        }
    }
}
