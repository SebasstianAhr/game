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
    ///
    /// Etapa 1 (móvil): diseño en VERTICAL (referencia 1080x1920), zona segura (muescas),
    /// orientación fija en vertical, paleta "tarot nocturno" y fuentes propias si están en Assets/Fonts.
    /// IMPORTANTE: este script es la única fuente de verdad de la interfaz. Los cambios hechos a mano
    /// en la escena se pierden al volver a ejecutar "Construir escena".
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ConfigPath = "Assets/Settings/GameConfig.asset";

        // Fuentes opcionales (Google Fonts, licencia OFL). Si no existen se usa la fuente por defecto de Unity.
        private const string BodyFontPath = "Assets/Fonts/Figtree-Regular.ttf";
        private const string BoldFontPath = "Assets/Fonts/Figtree-Bold.ttf";
        private const string TitleFontPath = "Assets/Fonts/Cinzel-Bold.ttf";

        // Paleta "tarot nocturno" (ver guía de estilo).
        private static readonly Color Night = Hex(0x0B0F24);
        private static readonly Color Ink = Hex(0x121839);
        private static readonly Color ButtonBlue = Hex(0x1E2C66);
        private static readonly Color ButtonJade = Hex(0x24564C);
        private static readonly Color ButtonAmber = Hex(0x8A541D);
        private static readonly Color ButtonCarmine = Hex(0x7A2234);
        private static readonly Color ButtonGray = Hex(0x353C6A);
        private static readonly Color Gold = Hex(0xF2CF7A);
        private static readonly Color Ivory = Hex(0xEDE6D3);
        private static readonly Color Mist = Hex(0xA9AECB);
        private static readonly Color DebugCyan = new Color(0.55f, 0.90f, 0.95f);

        private static Font bodyFont;
        private static Font boldFont;
        private static Font titleFont;

        [MenuItem("1% de Probabilidad/Construir escena")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            LoadFonts();
            ConfigurePortrait();
            GameConfig config = LoadOrCreateConfig();
            EnsureFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            // Todo el contenido va dentro de SafeArea para no quedar debajo de la muesca o la barra de gestos.
            RectTransform safeArea = NewRect("SafeArea", canvas.transform, V(0f, 0f), V(1f, 1f));
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            TableScreen table = BuildTablePanel(safeArea);
            ShieldMinigameScreen shield = BuildShieldPanel(safeArea);
            StatsScreen stats = BuildStatsPanel(safeArea);
            EndScreen end = BuildEndPanel(safeArea);

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

        private static void LoadFonts()
        {
            Font builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtIn == null)
            {
                builtIn = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            bodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            boldFont = AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath);
            titleFont = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);

            if (bodyFont == null || boldFont == null || titleFont == null)
            {
                Debug.LogWarning("[1% de Probabilidad] Faltan fuentes en Assets/Fonts (Figtree-Regular.ttf, Figtree-Bold.ttf, " +
                                 "Cinzel-Bold.ttf). Se usará la fuente por defecto donde falten.");
            }

            if (bodyFont == null)
            {
                bodyFont = builtIn;
            }

            if (boldFont == null)
            {
                boldFont = bodyFont;
            }

            if (titleFont == null)
            {
                titleFont = boldFont;
            }
        }

        /// <summary>El juego es solo vertical: sin giro automático a horizontal.</summary>
        private static void ConfigurePortrait()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
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

            // El color de la cámara rellena lo que queda fuera de la zona segura: igual que el fondo.
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Night;
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

            // Referencia vertical 1080x1920. Todas las medidas de texto de abajo están en esta escala.
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        // ------------------------------------------------------------------ Pantalla 1: Mesa

        private static TableScreen BuildTablePanel(Transform parent)
        {
            Image panel = NewImage("TablePanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            TableScreen screen = panel.gameObject.AddComponent<TableScreen>();

            // Barra superior: monedas y estadísticas.
            Text coins = NewText("CoinsText", root, V(0.05f, 0.905f), V(0.66f, 0.975f),
                "Monedas: 100", 72, TextAnchor.MiddleLeft, Gold, boldFont);
            coins.resizeTextForBestFit = true;
            coins.resizeTextMinSize = 40;
            coins.resizeTextMaxSize = 72;

            Text unusedLabel;
            Button statsButton = NewButton("StatsButton", root, V(0.68f, 0.915f), V(0.95f, 0.965f),
                "Estadísticas", 36, ButtonBlue, out unusedLabel);

            Text goal = NewText("GoalText", root, V(0.05f, 0.865f), V(0.95f, 0.900f),
                "Meta: 10000", 36, TextAnchor.MiddleLeft, Mist, bodyFont);

            // La carta del símbolo: un contenedor y, dentro, la imagen con proporción de carta (0,7).
            RectTransform symbolArea = NewRect("SymbolArea", root, V(0.20f, 0.565f), V(0.80f, 0.850f));
            Image symbolImage = NewImage("SymbolImage", symbolArea, V(0f, 0f), V(1f, 1f), Color.gray);
            AspectRatioFitter fitter = symbolImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 0.7f;

            Text symbolCaption = NewText("SymbolCaptionText", root, V(0.05f, 0.528f), V(0.95f, 0.562f),
                "Símbolo del fondo", 34, TextAnchor.MiddleCenter, Ivory, bodyFont);

            Image messagePanel = NewImage("MessagePanel", root, V(0.05f, 0.405f), V(0.95f, 0.518f), Ink);
            Text message = NewText("MessageText", messagePanel.transform, V(0.04f, 0.06f), V(0.96f, 0.94f),
                string.Empty, 40, TextAnchor.MiddleCenter, Ivory, bodyFont);
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 26;
            message.resizeTextMaxSize = 40;

            NewText("HistoryTitle", root, V(0.05f, 0.368f), V(0.95f, 0.400f),
                "Historial", 32, TextAnchor.MiddleLeft, Mist, titleFont);
            Image historyPanel = NewImage("HistoryPanel", root, V(0.05f, 0.215f), V(0.95f, 0.366f), Ink);
            Text history = NewText("HistoryText", historyPanel.transform, V(0.04f, 0.05f), V(0.96f, 0.95f),
                string.Empty, 30, TextAnchor.UpperLeft, Ivory, bodyFont);
            history.resizeTextForBestFit = true;
            history.resizeTextMinSize = 22;
            history.resizeTextMaxSize = 30;

            // Solo visible al pulsar Debug (y el botón Debug solo existe en compilaciones de desarrollo).
            Text debug = NewText("DebugText", root, V(0.05f, 0.160f), V(0.95f, 0.212f),
                string.Empty, 24, TextAnchor.UpperLeft, DebugCyan, bodyFont);
            debug.resizeTextForBestFit = true;
            debug.resizeTextMinSize = 16;
            debug.resizeTextMaxSize = 24;

            // Botones de decisión en la zona del pulgar.
            Text saveLabel;
            Text moderateLabel;
            Text allInLabel;
            Button saveButton = NewButton("SaveButton", root, V(0.05f, 0.045f), V(0.335f, 0.155f),
                "GUARDAR", 40, ButtonJade, out saveLabel);
            Button moderateButton = NewButton("ModerateButton", root, V(0.3575f, 0.045f), V(0.6425f, 0.155f),
                "APOSTAR 20", 40, ButtonAmber, out moderateLabel);
            Button allInButton = NewButton("AllInButton", root, V(0.665f, 0.045f), V(0.95f, 0.155f),
                "APOSTAR 100", 40, ButtonCarmine, out allInLabel);

            Button debugButton = NewButton("DebugButton", root, V(0.05f, 0.002f), V(0.25f, 0.038f),
                "Debug", 26, ButtonGray, out unusedLabel);

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
            Image panel = NewImage("ShieldPanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            ShieldMinigameScreen screen = panel.gameObject.AddComponent<ShieldMinigameScreen>();

            NewText("TitleText", root, V(0.05f, 0.930f), V(0.95f, 0.985f),
                "EL ESCUDO", 64, TextAnchor.MiddleCenter, Gold, titleFont);
            Text timer = NewText("TimerText", root, V(0.05f, 0.885f), V(0.48f, 0.925f),
                "Tiempo: 10.0", 38, TextAnchor.MiddleLeft, Ivory, boldFont);
            Text score = NewText("ScoreText", root, V(0.50f, 0.885f), V(0.95f, 0.925f),
                "Bloqueos: 0", 38, TextAnchor.MiddleRight, Gold, boldFont);

            float[] laneStart = { 0.05f, 0.3667f, 0.6833f };
            const float laneWidth = 0.2667f;
            RectTransform[] lanes = new RectTransform[3];
            Button[] laneButtons = new Button[3];

            for (int i = 0; i < 3; i++)
            {
                Image lane = NewImage("Lane" + (i + 1), root, V(laneStart[i], 0.20f), V(laneStart[i] + laneWidth, 0.87f), Ink);
                lanes[i] = lane.rectTransform;

                Text unusedLabel;
                laneButtons[i] = NewButton("BlockButton" + (i + 1), root,
                    V(laneStart[i], 0.06f), V(laneStart[i] + laneWidth, 0.18f),
                    "BLOQUEAR", 36, ButtonBlue, out unusedLabel);
            }

            // El panel de introducción/resultado va al final para dibujarse encima de los carriles.
            Image overlay = NewImage("Overlay", root, V(0.05f, 0.20f), V(0.95f, 0.87f), new Color(0.03f, 0.04f, 0.10f, 0.94f));
            Text info = NewText("InfoText", overlay.transform, V(0.06f, 0.22f), V(0.94f, 0.96f),
                string.Empty, 38, TextAnchor.MiddleCenter, Ivory, bodyFont);
            info.resizeTextForBestFit = true;
            info.resizeTextMinSize = 24;
            info.resizeTextMaxSize = 38;

            Text startLabel;
            Button start = NewButton("StartButton", overlay.transform, V(0.15f, 0.05f), V(0.85f, 0.16f),
                "COMENZAR", 44, ButtonJade, out startLabel);
            Text continueLabel;
            Button cont = NewButton("ContinueButton", overlay.transform, V(0.15f, 0.05f), V(0.85f, 0.16f),
                "CONTINUAR", 44, ButtonJade, out continueLabel);

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
            Image panel = NewImage("StatsPanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            StatsScreen screen = panel.gameObject.AddComponent<StatsScreen>();

            NewText("TitleText", root, V(0.05f, 0.930f), V(0.95f, 0.985f),
                "LOGROS Y ESTADÍSTICAS", 54, TextAnchor.MiddleCenter, Gold, titleFont);

            // En vertical, las tres columnas pasan a ser tres bloques apilados.
            Text stats = NewSection("StatsBlock", root, V(0.05f, 0.705f), V(0.95f, 0.920f));
            Text patterns = NewSection("PatternsBlock", root, V(0.05f, 0.430f), V(0.95f, 0.695f));
            Text achievements = NewSection("AchievementsBlock", root, V(0.05f, 0.125f), V(0.95f, 0.420f));

            Text backLabel;
            Button back = NewButton("BackButton", root, V(0.05f, 0.03f), V(0.48f, 0.105f),
                "VOLVER", 40, ButtonBlue, out backLabel);
            Text resetLabel;
            Button reset = NewButton("ResetButton", root, V(0.52f, 0.03f), V(0.95f, 0.105f),
                "Borrar progreso", 34, ButtonCarmine, out resetLabel);

            Assign(screen, "statsText", stats);
            Assign(screen, "patternsText", patterns);
            Assign(screen, "achievementsText", achievements);
            Assign(screen, "backButton", back);
            Assign(screen, "resetButton", reset);
            Assign(screen, "resetLabel", resetLabel);

            return screen;
        }

        /// <summary>Bloque de fondo oscuro con un texto que se reduce solo si no cabe.</summary>
        private static Text NewSection(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image block = NewImage(name, parent, anchorMin, anchorMax, Ink);
            Text text = NewText("Text", block.transform, V(0.04f, 0.04f), V(0.96f, 0.96f),
                string.Empty, 30, TextAnchor.UpperLeft, Ivory, bodyFont);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = 30;
            return text;
        }

        // ------------------------------------------------------------------ Victoria / Derrota

        private static EndScreen BuildEndPanel(Transform parent)
        {
            Image panel = NewImage("EndPanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            EndScreen screen = panel.gameObject.AddComponent<EndScreen>();

            Text title = NewText("TitleText", root, V(0.05f, 0.64f), V(0.95f, 0.78f),
                "FIN", 104, TextAnchor.MiddleCenter, Ivory, titleFont);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 60;
            title.resizeTextMaxSize = 104;
            Text summary = NewText("SummaryText", root, V(0.08f, 0.36f), V(0.92f, 0.62f),
                string.Empty, 42, TextAnchor.UpperCenter, Ivory, bodyFont);

            Text newGameLabel;
            Button newGame = NewButton("NewGameButton", root, V(0.10f, 0.20f), V(0.90f, 0.29f),
                "NUEVA PARTIDA", 44, ButtonJade, out newGameLabel);
            Text statsLabel;
            Button stats = NewButton("StatsButton", root, V(0.10f, 0.08f), V(0.90f, 0.17f),
                "ESTADÍSTICAS", 44, ButtonBlue, out statsLabel);

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

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
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
            string content, int size, TextAnchor alignment, Color color, Font textFont)
        {
            RectTransform rect = NewRect(name, parent, anchorMin, anchorMax);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = textFont != null ? textFont : bodyFont;
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

            labelText = NewText("Label", image.transform, V(0.05f, 0.06f), V(0.95f, 0.94f),
                label, size, TextAnchor.MiddleCenter, Ivory, boldFont);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 18;
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
