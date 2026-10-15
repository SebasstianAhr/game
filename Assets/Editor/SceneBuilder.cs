using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OnePercent.EditorTools
{
    /// <summary>
    /// Construye automáticamente la escena Assets/Scenes/Main.unity con toda la interfaz,
    /// crea Assets/Settings/GameConfig.asset, conecta todas las referencias y la añade al Build Settings.
    /// Se ejecuta desde el menú: 1% de Probabilidad > Construir escena.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string SettingsFolder = "Assets/Settings";
        private const string ConfigPath = "Assets/Settings/GameConfig.asset";

        private static readonly Color CameraBg = new Color(0.05f, 0.06f, 0.10f);
        private static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.14f);
        private static readonly Color PanelLight = new Color(0.14f, 0.16f, 0.23f);
        private static readonly Color LaneBg = new Color(0.12f, 0.14f, 0.20f);
        private static readonly Color ButtonBlue = new Color(0.20f, 0.35f, 0.65f);
        private static readonly Color ButtonGreen = new Color(0.20f, 0.55f, 0.35f);
        private static readonly Color ButtonOrange = new Color(0.80f, 0.45f, 0.15f);
        private static readonly Color ButtonRed = new Color(0.70f, 0.20f, 0.25f);
        private static readonly Color ButtonGray = new Color(0.30f, 0.33f, 0.40f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.30f);
        private static readonly Color Soft = new Color(0.80f, 0.85f, 0.92f);

        private static Font font;

        [MenuItem("1% de Probabilidad/Construir escena")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            font = LoadFont();
            GameConfig config = LoadOrCreateConfig();
            EnsureFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            TableScreen table = BuildTablePanel(canvas.transform);
            ShieldMinigameScreen shield = BuildShieldPanel(canvas.transform);
            StatsScreen stats = BuildStatsPanel(canvas.transform);
            EndScreen end = BuildEndPanel(canvas.transform);

            GameObject managerObject = new GameObject("GameManager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            Assign(manager, "config", config);
            Assign(manager, "tableScreen", table);
            Assign(manager, "shieldScreen", shield);
            Assign(manager, "statsScreen", stats);
            Assign(manager, "endScreen", end);

            // Solo la mesa empieza visible. El GameManager muestra las demás cuando hacen falta.
            table.gameObject.SetActive(true);
            shield.gameObject.SetActive(false);
            stats.gameObject.SetActive(false);
            end.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[1% de Probabilidad] Escena creada en " + ScenePath + " y añadida al Build Settings.");
            EditorUtility.DisplayDialog(
                "1% de Probabilidad",
                "Escena creada correctamente:\n" + ScenePath + "\n\nPulsa el botón Play para probar el juego.",
                "Aceptar");
        }

        // ------------------------------------------------------------------ Escena base

        private static Font LoadFont()
        {
            Font builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtIn == null)
            {
                builtIn = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (builtIn == null)
            {
                builtIn = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            return builtIn;
        }

        private static GameConfig LoadOrCreateConfig()
        {
            EnsureFolder("Assets", "Settings");

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void SetupCamera()
        {
            Light light = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBg;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            // Con el nuevo Input System hay que usar InputSystemUIInputModule. Se busca por nombre
            // para que este script compile aunque el paquete no estuviera instalado.
            Type inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                Component module = eventSystemObject.AddComponent(inputSystemModule);
                System.Reflection.MethodInfo assign = inputSystemModule.GetMethod("AssignDefaultActions");
                if (assign != null)
                {
                    assign.Invoke(module, null);
                }
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        // ------------------------------------------------------------------ Pantalla 1: Mesa

        private static TableScreen BuildTablePanel(Transform parent)
        {
            Image panel = NewImage("TablePanel", parent, V(0f, 0f), V(1f, 1f), PanelBg);
            Transform root = panel.transform;
            TableScreen screen = panel.gameObject.AddComponent<TableScreen>();

            Text coins = NewText("CoinsText", root, V(0.03f, 0.86f), V(0.40f, 0.98f),
                "Monedas: 100", 40, TextAnchor.MiddleLeft, Gold);
            Text goal = NewText("GoalText", root, V(0.40f, 0.86f), V(0.76f, 0.98f),
                "Meta: 10000", 24, TextAnchor.MiddleCenter, Soft);

            Text unusedLabel;
            Button statsButton = NewButton("StatsButton", root, V(0.78f, 0.88f), V(0.97f, 0.97f),
                "Estadísticas", 24, ButtonBlue, out unusedLabel);

            Image symbolImage = NewImage("SymbolImage", root, V(0.04f, 0.50f), V(0.27f, 0.82f), Color.gray);
            Text symbolCaption = NewText("SymbolCaptionText", root, V(0.02f, 0.43f), V(0.29f, 0.50f),
                "Símbolo del fondo", 20, TextAnchor.MiddleCenter, Soft);

            Image messagePanel = NewImage("MessagePanel", root, V(0.30f, 0.45f), V(0.67f, 0.82f), PanelLight);
            Text message = NewText("MessageText", messagePanel.transform, V(0.04f, 0.04f), V(0.96f, 0.96f),
                string.Empty, 26, TextAnchor.MiddleCenter, Color.white);

            Text debug = NewText("DebugText", root, V(0.03f, 0.28f), V(0.67f, 0.42f),
                string.Empty, 18, TextAnchor.UpperLeft, new Color(0.55f, 0.90f, 0.95f));

            NewText("HistoryTitle", root, V(0.70f, 0.80f), V(0.97f, 0.86f),
                "Historial", 24, TextAnchor.MiddleCenter, Soft);
            Image historyPanel = NewImage("HistoryPanel", root, V(0.70f, 0.28f), V(0.97f, 0.80f), PanelLight);
            Text history = NewText("HistoryText", historyPanel.transform, V(0.05f, 0.03f), V(0.95f, 0.97f),
                string.Empty, 18, TextAnchor.UpperLeft, Color.white);

            Text saveLabel;
            Text moderateLabel;
            Text allInLabel;
            Button saveButton = NewButton("SaveButton", root, V(0.03f, 0.08f), V(0.33f, 0.27f),
                "GUARDAR", 26, ButtonGreen, out saveLabel);
            Button moderateButton = NewButton("ModerateButton", root, V(0.35f, 0.08f), V(0.65f, 0.27f),
                "APOSTAR 20", 26, ButtonOrange, out moderateLabel);
            Button allInButton = NewButton("AllInButton", root, V(0.67f, 0.08f), V(0.97f, 0.27f),
                "APOSTAR 100", 26, ButtonRed, out allInLabel);

            Button debugButton = NewButton("DebugButton", root, V(0.03f, 0.01f), V(0.14f, 0.06f),
                "Debug", 16, ButtonGray, out unusedLabel);

            Assign(screen, "coinsText", coins);
            Assign(screen, "goalText", goal);
            Assign(screen, "messageText", message);
            Assign(screen, "historyText", history);
            Assign(screen, "symbolCaptionText", symbolCaption);
            Assign(screen, "debugText", debug);
            Assign(screen, "symbolImage", symbolImage);
            Assign(screen, "saveButton", saveButton);
            Assign(screen, "moderateButton", moderateButton);
            Assign(screen, "allInButton", allInButton);
            Assign(screen, "statsButton", statsButton);
            Assign(screen, "debugButton", debugButton);
            Assign(screen, "saveLabel", saveLabel);
            Assign(screen, "moderateLabel", moderateLabel);
            Assign(screen, "allInLabel", allInLabel);

            return screen;
        }

        // ------------------------------------------------------------------ Pantalla 2: El Escudo

        private static ShieldMinigameScreen BuildShieldPanel(Transform parent)
        {
            Image panel = NewImage("ShieldPanel", parent, V(0f, 0f), V(1f, 1f), PanelBg);
            Transform root = panel.transform;
            ShieldMinigameScreen screen = panel.gameObject.AddComponent<ShieldMinigameScreen>();

            Text timer = NewText("TimerText", root, V(0.04f, 0.87f), V(0.30f, 0.97f),
                "Tiempo: 10.0", 28, TextAnchor.MiddleLeft, Soft);
            NewText("TitleText", root, V(0.30f, 0.88f), V(0.70f, 0.98f),
                "EL ESCUDO", 40, TextAnchor.MiddleCenter, Gold);
            Text score = NewText("ScoreText", root, V(0.70f, 0.87f), V(0.96f, 0.97f),
                "Bloqueos: 0", 28, TextAnchor.MiddleRight, Soft);

            float[] laneStart = { 0.10f, 0.40f, 0.70f };
            RectTransform[] lanes = new RectTransform[3];
            Button[] laneButtons = new Button[3];

            for (int i = 0; i < 3; i++)
            {
                Image lane = NewImage("Lane" + (i + 1), root, V(laneStart[i], 0.18f), V(laneStart[i] + 0.20f, 0.84f), LaneBg);
                lanes[i] = lane.rectTransform;

                Text unusedLabel;
                laneButtons[i] = NewButton("BlockButton" + (i + 1), root,
                    V(laneStart[i], 0.04f), V(laneStart[i] + 0.20f, 0.16f),
                    "BLOQUEAR", 26, ButtonBlue, out unusedLabel);
            }

            // El panel de introducción/resultado va al final para dibujarse encima de los carriles.
            Image overlay = NewImage("Overlay", root, V(0.08f, 0.17f), V(0.92f, 0.85f), new Color(0f, 0f, 0f, 0.88f));
            Text info = NewText("InfoText", overlay.transform, V(0.03f, 0.28f), V(0.97f, 0.97f),
                string.Empty, 24, TextAnchor.MiddleCenter, Color.white);

            Text startLabel;
            Button start = NewButton("StartButton", overlay.transform, V(0.33f, 0.05f), V(0.67f, 0.22f),
                "COMENZAR", 28, ButtonGreen, out startLabel);
            Text continueLabel;
            Button cont = NewButton("ContinueButton", overlay.transform, V(0.33f, 0.05f), V(0.67f, 0.22f),
                "CONTINUAR", 28, ButtonGreen, out continueLabel);

            Assign(screen, "timerText", timer);
            Assign(screen, "scoreText", score);
            Assign(screen, "infoText", info);
            Assign(screen, "overlay", overlay.gameObject);
            Assign(screen, "startButton", start);
            Assign(screen, "continueButton", cont);
            AssignArray(screen, "laneAreas", lanes);
            AssignArray(screen, "laneButtons", laneButtons);

            return screen;
        }

        // ------------------------------------------------------------------ Pantalla 3: Estadísticas

        private static StatsScreen BuildStatsPanel(Transform parent)
        {
            Image panel = NewImage("StatsPanel", parent, V(0f, 0f), V(1f, 1f), PanelBg);
            Transform root = panel.transform;
            StatsScreen screen = panel.gameObject.AddComponent<StatsScreen>();

            NewText("TitleText", root, V(0.10f, 0.90f), V(0.90f, 0.99f),
                "LOGROS Y ESTADÍSTICAS", 38, TextAnchor.MiddleCenter, Gold);

            Image statsPanel = NewImage("StatsColumn", root, V(0.03f, 0.16f), V(0.34f, 0.89f), PanelLight);
            Text stats = NewText("StatsText", statsPanel.transform, V(0.05f, 0.03f), V(0.95f, 0.97f),
                string.Empty, 20, TextAnchor.UpperLeft, Color.white);

            Image patternsPanel = NewImage("PatternsColumn", root, V(0.355f, 0.16f), V(0.675f, 0.89f), PanelLight);
            Text patterns = NewText("PatternsText", patternsPanel.transform, V(0.05f, 0.03f), V(0.95f, 0.97f),
                string.Empty, 20, TextAnchor.UpperLeft, Color.white);

            Image achievementsPanel = NewImage("AchievementsColumn", root, V(0.69f, 0.16f), V(0.97f, 0.89f), PanelLight);
            Text achievements = NewText("AchievementsText", achievementsPanel.transform, V(0.05f, 0.03f), V(0.95f, 0.97f),
                string.Empty, 20, TextAnchor.UpperLeft, Color.white);

            Text backLabel;
            Button back = NewButton("BackButton", root, V(0.03f, 0.03f), V(0.30f, 0.13f),
                "VOLVER", 26, ButtonBlue, out backLabel);
            Text resetLabel;
            Button reset = NewButton("ResetButton", root, V(0.69f, 0.03f), V(0.97f, 0.13f),
                "Borrar progreso", 22, ButtonRed, out resetLabel);

            Assign(screen, "statsText", stats);
            Assign(screen, "patternsText", patterns);
            Assign(screen, "achievementsText", achievements);
            Assign(screen, "backButton", back);
            Assign(screen, "resetButton", reset);
            Assign(screen, "resetLabel", resetLabel);

            return screen;
        }

        // ------------------------------------------------------------------ Victoria / Derrota

        private static EndScreen BuildEndPanel(Transform parent)
        {
            Image panel = NewImage("EndPanel", parent, V(0f, 0f), V(1f, 1f), new Color(0.05f, 0.07f, 0.12f, 1f));
            Transform root = panel.transform;
            EndScreen screen = panel.gameObject.AddComponent<EndScreen>();

            Text title = NewText("TitleText", root, V(0.10f, 0.62f), V(0.90f, 0.85f),
                "FIN", 64, TextAnchor.MiddleCenter, Color.white);
            Text summary = NewText("SummaryText", root, V(0.15f, 0.33f), V(0.85f, 0.60f),
                string.Empty, 28, TextAnchor.UpperCenter, Soft);

            Text newGameLabel;
            Button newGame = NewButton("NewGameButton", root, V(0.18f, 0.12f), V(0.48f, 0.26f),
                "NUEVA PARTIDA", 28, ButtonGreen, out newGameLabel);
            Text statsLabel;
            Button stats = NewButton("StatsButton", root, V(0.52f, 0.12f), V(0.82f, 0.26f),
                "ESTADÍSTICAS", 28, ButtonBlue, out statsLabel);

            Assign(screen, "titleText", title);
            Assign(screen, "summaryText", summary);
            Assign(screen, "newGameButton", newGame);
            Assign(screen, "statsButton", stats);

            return screen;
        }

        // ------------------------------------------------------------------ Utilidades de interfaz

        private static Vector2 V(float x, float y)
        {
            return new Vector2(x, y);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image NewImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = NewRect(name, parent, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text NewText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string content, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, anchorMin, anchorMax);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button NewButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string label, int size, Color color, out Text labelText)
        {
            Image image = NewImage(name, parent, anchorMin, anchorMax, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", image.transform, V(0.03f, 0.05f), V(0.97f, 0.95f),
                label, size, TextAnchor.MiddleCenter, Color.white);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 10;
            labelText.resizeTextMaxSize = size;
            return button;
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[SceneBuilder] No existe el campo '" + field + "' en " + target.GetType().Name);
                return;
            }

            if (value == null)
            {
                Debug.LogError("[SceneBuilder] Valor nulo para " + target.GetType().Name + "." + field);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[SceneBuilder] No existe el campo '" + field + "' en " + target.GetType().Name);
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
