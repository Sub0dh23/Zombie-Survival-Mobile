using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadDawn.Player
{
    /// <summary>
    /// Renders physical perimeter barriers, a glowing tactical ground line, and proximity HUD warnings
    /// along the player's movement boundary so the player never encounters an invisible wall.
    /// Automatically updates and expands when the base is upgraded.
    /// </summary>
    public class BoundaryVisualizer : MonoBehaviour
    {
        public static BoundaryVisualizer Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerController playerController;

        [Header("Barrier Visual Settings")]
        [SerializeField] private float postSpacing = 6f;
        [SerializeField] private float postHeight = 1.8f;
        [SerializeField] private float postWidth = 0.28f;
        [SerializeField] private Material postMaterial;
        [SerializeField] private Material railMaterial;
        [SerializeField] private Material boundaryLineMaterial;

        [Header("Boundary Line Settings")]
        [SerializeField] private float lineWidth = 0.35f;
        [SerializeField] private Color boundaryLineColor = new Color(0.97f, 0.60f, 0.20f, 0.95f); // Tactical Amber
        [SerializeField] private Color warningLineColor = new Color(0.97f, 0.32f, 0.29f, 1.0f); // Alert Red

        [Header("Proximity Alert")]
        [SerializeField] private float alertDistance = 4.5f;

        private LineRenderer lineRenderer;
        private GameObject fenceParent;
        private float currentNearestDistance = float.MaxValue;
        private bool isNearBoundary = false;
        private bool isTouchingBoundary = false;

        // Figma HUD Style for Proximity Badge
        private static Texture2D promptBgTex;
        private static Texture2D promptPillTex;
        private static GUIStyle promptBgStyle;
        private static GUIStyle promptPillStyle;
        private static GUIStyle promptTitleStyle;
        private static GUIStyle promptSubStyle;
        private static GUIStyle pillTextStyle;
        private static Font uiFont;
        private static bool stylesInitialized = false;
        private float lastScale = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        }

        private void Start()
        {
            LoadMaterials();
            CreateLineRenderer();
            BuildPhysicalPerimeter();

            if (playerController != null)
            {
                playerController.OnBoundaryChanged += HandleBoundaryChanged;
            }
        }

        private void OnDestroy()
        {
            if (playerController != null)
            {
                playerController.OnBoundaryChanged -= HandleBoundaryChanged;
            }
        }

        private void HandleBoundaryChanged(Vector2 newX, Vector2 newZ)
        {
            BuildPhysicalPerimeter();
            UpdateLineRendererPositions();
        }

        private void LoadMaterials()
        {
#if UNITY_EDITOR
            if (postMaterial == null)
            {
                postMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/MAT_Wood.mat");
            }
            if (railMaterial == null)
            {
                railMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/MAT_Scrap.mat");
            }
#endif
            if (postMaterial == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                if (s != null) postMaterial = new Material(s) { color = new Color(0.45f, 0.28f, 0.15f) };
            }
            if (railMaterial == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                if (s != null) railMaterial = new Material(s) { color = new Color(0.35f, 0.38f, 0.42f) };
            }
            if (boundaryLineMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                boundaryLineMaterial = new Material(shader);
                boundaryLineMaterial.color = boundaryLineColor;
            }
        }

        private void CreateLineRenderer()
        {
            var lineGo = new GameObject("BoundaryLineRenderer");
            lineGo.transform.SetParent(transform, false);
            lineRenderer = lineGo.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 5;
            lineRenderer.loop = true;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = boundaryLineMaterial;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.textureMode = LineTextureMode.Tile;
            UpdateLineRendererPositions();
        }

        private void UpdateLineRendererPositions()
        {
            if (lineRenderer == null || playerController == null) return;

            Vector2 bx = playerController.BoundaryX;
            Vector2 bz = playerController.BoundaryZ;
            float y = 0.06f; // Slightly above ground plane

            lineRenderer.SetPosition(0, new Vector3(bx.x, y, bz.x));
            lineRenderer.SetPosition(1, new Vector3(bx.y, y, bz.x));
            lineRenderer.SetPosition(2, new Vector3(bx.y, y, bz.y));
            lineRenderer.SetPosition(3, new Vector3(bx.x, y, bz.y));
            lineRenderer.SetPosition(4, new Vector3(bx.x, y, bz.x));
        }

        public void BuildPhysicalPerimeter()
        {
            if (playerController == null) return;

            if (fenceParent != null)
            {
                Destroy(fenceParent);
            }

            fenceParent = new GameObject("Boundary_Perimeter_Fence");
            fenceParent.transform.SetParent(transform, false);

            Vector2 bx = playerController.BoundaryX;
            Vector2 bz = playerController.BoundaryZ;

            // Load cube primitive
            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempCube);

            System.Action<Vector3, Vector3, Material> createBox = (pos, scale, mat) =>
            {
                var go = new GameObject("BarrierPost");
                go.transform.SetParent(fenceParent.transform, false);
                go.transform.position = pos;
                go.transform.localScale = scale;
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = cubeMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            };

            // Build along 4 perimeter sides:
            // Side 1: South (z = bz.x, from bx.x to bx.y)
            BuildWallSide(new Vector3(bx.x, 0, bz.x), new Vector3(bx.y, 0, bz.x), createBox);
            // Side 2: North (z = bz.y, from bx.x to bx.y)
            BuildWallSide(new Vector3(bx.x, 0, bz.y), new Vector3(bx.y, 0, bz.y), createBox);
            // Side 3: West (x = bx.x, from bz.x to bz.y)
            BuildWallSide(new Vector3(bx.x, 0, bz.x), new Vector3(bx.x, 0, bz.y), createBox);
            // Side 4: East (x = bx.y, from bz.x to bz.y)
            BuildWallSide(new Vector3(bx.y, 0, bz.x), new Vector3(bx.y, 0, bz.y), createBox);
        }

        private void BuildWallSide(Vector3 start, Vector3 end, System.Action<Vector3, Vector3, Material> createBox)
        {
            float length = Vector3.Distance(start, end);
            int postCount = Mathf.Max(2, Mathf.RoundToInt(length / postSpacing));
            Vector3 dir = (end - start).normalized;
            float step = length / (postCount - 1);

            for (int i = 0; i < postCount; i++)
            {
                Vector3 postPos = start + dir * (i * step);
                postPos.y = postHeight * 0.5f;

                // Vertical fence post
                createBox(postPos, new Vector3(postWidth, postHeight, postWidth), postMaterial);

                // Top post cap
                Vector3 capPos = postPos + Vector3.up * (postHeight * 0.5f + 0.05f);
                createBox(capPos, new Vector3(postWidth * 1.3f, 0.10f, postWidth * 1.3f), railMaterial);

                // Horizontal rails between posts
                if (i < postCount - 1)
                {
                    Vector3 nextPostPos = start + dir * ((i + 1) * step);
                    Vector3 midPos = (postPos + nextPostPos) * 0.5f;

                    // Rail 1 (lower)
                    Vector3 rail1Pos = midPos;
                    rail1Pos.y = postHeight * 0.35f;
                    Vector3 railScale1 = dir.x != 0
                        ? new Vector3(step, 0.12f, 0.10f)
                        : new Vector3(0.10f, 0.12f, step);
                    createBox(rail1Pos, railScale1, railMaterial);

                    // Rail 2 (upper)
                    Vector3 rail2Pos = midPos;
                    rail2Pos.y = postHeight * 0.75f;
                    Vector3 railScale2 = dir.x != 0
                        ? new Vector3(step, 0.12f, 0.10f)
                        : new Vector3(0.10f, 0.12f, step);
                    createBox(rail2Pos, railScale2, railMaterial);
                }
            }
        }

        private void Update()
        {
            if (playerController == null) return;

            Vector3 p = playerController.transform.position;
            Vector2 bx = playerController.BoundaryX;
            Vector2 bz = playerController.BoundaryZ;

            float distLeft = p.x - bx.x;
            float distRight = bx.y - p.x;
            float distBottom = p.z - bz.x;
            float distTop = bz.y - p.z;

            currentNearestDistance = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));
            isNearBoundary = currentNearestDistance <= alertDistance;
            isTouchingBoundary = currentNearestDistance <= 0.45f;

            // Pulse line color when approaching
            if (lineRenderer != null && boundaryLineMaterial != null)
            {
                if (isTouchingBoundary)
                {
                    float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 10f);
                    boundaryLineMaterial.color = warningLineColor * pulse;
                }
                else if (isNearBoundary)
                {
                    float t = 1f - Mathf.Clamp01(currentNearestDistance / alertDistance);
                    boundaryLineMaterial.color = Color.Lerp(boundaryLineColor, warningLineColor, t);
                }
                else
                {
                    boundaryLineMaterial.color = boundaryLineColor;
                }
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
            catch { }
            return null;
        }

        private static GUIStyle Create9SliceStyle(Texture2D tex, int border)
        {
            GUIStyle st = new GUIStyle();
            st.normal.background = tex;
            st.border = new RectOffset(border, border, border, border);
            return st;
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

            if (promptBgTex == null) promptBgTex = LoadSprite("Slice_Prompt_Default_BG.png");
            if (promptPillTex == null) promptPillTex = LoadSprite("Slice_Prompt_KeyPill.png");

            promptBgStyle = Create9SliceStyle(promptBgTex, 10);
            promptPillStyle = Create9SliceStyle(promptPillTex, 8);

            promptTitleStyle = new GUIStyle();
            if (uiFont != null) promptTitleStyle.font = uiFont;
            promptTitleStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 16);
            promptTitleStyle.fontStyle = FontStyle.Bold;
            promptTitleStyle.normal.textColor = new Color(0.97f, 0.60f, 0.20f);
            promptTitleStyle.alignment = TextAnchor.MiddleLeft;

            promptSubStyle = new GUIStyle();
            if (uiFont != null) promptSubStyle.font = uiFont;
            promptSubStyle.fontSize = Mathf.Clamp((int)(10 * scale), 9, 14);
            promptSubStyle.fontStyle = FontStyle.Bold;
            promptSubStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
            promptSubStyle.alignment = TextAnchor.MiddleLeft;

            pillTextStyle = new GUIStyle();
            if (uiFont != null) pillTextStyle.font = uiFont;
            pillTextStyle.fontSize = Mathf.Clamp((int)(12 * scale), 10, 16);
            pillTextStyle.fontStyle = FontStyle.Bold;
            pillTextStyle.normal.textColor = new Color(0.97f, 0.60f, 0.20f);
            pillTextStyle.alignment = TextAnchor.MiddleCenter;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isNearBoundary || Time.timeScale <= 0f || DeadDawn.UI.DeathScreenUI.IsGameOver) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float scale = Mathf.Clamp(screenH / 1080f, 0.75f, 1.25f);

            if (!stylesInitialized || Mathf.Abs(scale - lastScale) > 0.01f)
            {
                InitStyles(scale);
                lastScale = scale;
            }

            // Floating alert banner at top center
            float badgeW = Mathf.Clamp(340f * scale, 280f, 420f);
            float badgeH = 50f * scale;
            float badgeX = (screenW - badgeW) * 0.5f;
            float badgeY = 90f * scale;

            Rect badgeRect = new Rect(badgeX, badgeY, badgeW, badgeH);

            if (promptBgStyle != null && Event.current.type == EventType.Repaint)
            {
                promptBgStyle.Draw(badgeRect, false, false, false, false);
            }

            // Warning Icon / Pill
            float pillSize = 32f * scale;
            float pillX = badgeX + 10f * scale;
            float pillY = badgeY + (badgeH - pillSize) * 0.5f;
            Rect pillRect = new Rect(pillX, pillY, pillSize, pillSize);

            if (promptPillStyle != null && Event.current.type == EventType.Repaint)
            {
                promptPillStyle.Draw(pillRect, false, false, false, false);
            }

            string iconText = isTouchingBoundary ? "⛔" : "⚠️";
            GUI.Label(pillRect, iconText, pillTextStyle);

            // Text
            float textX = pillX + pillSize + 10f * scale;
            float textW = badgeW - (textX - badgeX) - 10f * scale;
            float titleY = badgeY + 7f * scale;
            float titleH = 18f * scale;
            float subY = titleY + titleH;
            float subH = 15f * scale;

            string title = isTouchingBoundary
                ? "SECTOR PERIMETER // LIMIT REACHED"
                : "SECTOR PERIMETER // OUTPOST LIMIT";
            string sub = isTouchingBoundary
                ? "UPGRADE BASE TO EXPAND PATROL AREA"
                : $"PERIMETER BARRIER AHEAD ({(int)currentNearestDistance}m)";

            if (promptTitleStyle != null)
            {
                promptTitleStyle.normal.textColor = isTouchingBoundary
                    ? new Color(0.97f, 0.32f, 0.29f) // Red
                    : new Color(0.97f, 0.60f, 0.20f); // Amber
                GUI.Label(new Rect(textX, titleY, textW, titleH), title, promptTitleStyle);
            }

            if (promptSubStyle != null)
            {
                GUI.Label(new Rect(textX, subY, textW, subH), sub, promptSubStyle);
            }
        }
    }
}
