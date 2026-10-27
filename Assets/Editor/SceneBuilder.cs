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
    /// Etapa 1 (móvil): diseño en VERTICAL (referencia 1080x1920), zona segura, orientación vertical, fuentes.
    /// Etapa 2 (identidad visual): cartas del símbolo, moneda, registro con mini cartas, paneles y botones
    /// redondeados, fondo con astrolabio y motivos sutiles. Las imágenes viven en Assets/Art y este script
    /// las configura solo como sprites antes de construir. Si alguna falta, se usa un color liso.
    /// Etapa 3 (vida): fundido al cambiar de pantalla, botones que se hunden, destellos y 60 fps.
    /// Etapa 4 (audio): AudioManager con música y efectos de Assets/Audio, botón de silencio y ajustes de sonido.
    /// Etapa 5 (pantallas): menú principal, pausa (botón en la mesa) y "Cómo se juega" (sale sola la primera vez).
    /// Preguntas de matemáticas: notificación con tiempo junto a la carta y pantalla de pregunta con 4 opciones.
    /// IMPORTANTE: este script es la única fuente de verdad de la interfaz. Los cambios hechos a mano
    /// en la escena se pierden al volver a ejecutar "Construir escena".
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ConfigPath = "Assets/Settings/GameConfig.asset";
        private const string ArtFolder = "Assets/Art";
        private const string AudioFolder = "Assets/Audio";

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
        private static readonly Color GoldLine = Hex(0xC9A55A);
        private static readonly Color Ivory = Hex(0xEDE6D3);
        private static readonly Color Mist = Hex(0xA9AECB);
        private static readonly Color DebugCyan = new Color(0.55f, 0.90f, 0.95f);

        private static Font bodyFont;
        private static Font boldFont;
        private static Font titleFont;

        private static Sprite panelSprite;
        private static Sprite buttonSprite;
        private static Sprite outlineSprite;
        private static Sprite minicardSprite;
        private static Sprite coinSprite;
        private static Sprite glowSprite;
        private static Sprite astrolabeSprite;
        private static Sprite motifEyeSprite;
        private static Sprite motifMoonSprite;
        private static Sprite projectileSprite;
        private static Sprite sparkSprite;
        private static Sprite soundOnSprite;
        private static Sprite soundOffSprite;
        private static Sprite pauseSprite;
        private static Sprite ringSprite;
        private static Sprite questionCardSprite;

        private static AudioClip[] sfxClips;
        private static AudioClip tableMusic;
        private static AudioClip shieldMusic;
        private static Sprite sunCard;
        private static Sprite moonCard;
        private static Sprite eyeCard;
        private static Sprite sunIcon;
        private static Sprite moonIcon;
        private static Sprite eyeIcon;

        [MenuItem("1% de Probabilidad/Construir escena")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            LoadFonts();
            PrepareArt();
            LoadSprites();
            PrepareAudio();
            LoadAudio();
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
            MenuScreen menu = BuildMenuPanel(safeArea);
            PauseScreen pause = BuildPausePanel(safeArea);
            HowToScreen howTo = BuildHowToPanel(safeArea);
            QuestionNotification questionNotification = BuildQuestionNotification(table.transform);
            QuestionScreen questionScreen = BuildQuestionPanel(safeArea);

            // Audio: música y efectos (Etapa 4).
            GameObject audioObject = new GameObject("AudioManager");
            AudioManager audioManager = audioObject.AddComponent<AudioManager>();
            AssignArray(audioManager, "sfxClips", sfxClips);
            AssignOptional(audioManager, "tableMusic", tableMusic);
            AssignOptional(audioManager, "shieldMusic", shieldMusic);

            GameObject managerObject = new GameObject("GameManager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            managerObject.AddComponent<AppSettings>();
            Assign(manager, "config", config);
            Assign(manager, "tableScreen", table);
            Assign(manager, "shieldScreen", shield);
            Assign(manager, "statsScreen", stats);
            Assign(manager, "endScreen", end);
            Assign(manager, "menuScreen", menu);
            Assign(manager, "pauseScreen", pause);
            Assign(manager, "howToScreen", howTo);
            Assign(manager, "questionNotification", questionNotification);
            Assign(manager, "questionScreen", questionScreen);

            // El GameManager muestra cada pantalla cuando hace falta.
            // Se empieza en el menú principal.
            menu.gameObject.SetActive(true);
            table.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            howTo.gameObject.SetActive(false);
            questionNotification.gameObject.SetActive(false);
            questionScreen.gameObject.SetActive(false);
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

        // ------------------------------------------------------------------ Recursos

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

        /// <summary>
        /// Configura todas las imágenes de Assets/Art como sprites de interfaz: sin mipmaps, con transparencia
        /// y, en las que terminan en "_9slice", con bordes para estirarse sin deformar las esquinas.
        /// </summary>
        private static void PrepareArt()
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                Debug.LogWarning("[1% de Probabilidad] No existe " + ArtFolder + ". La interfaz se construirá con colores lisos.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                Vector4 border = Vector4.zero;
                if (fileName == "minicard_9slice")
                {
                    border = new Vector4(20f, 20f, 20f, 20f);
                }
                else if (fileName.EndsWith("_9slice"))
                {
                    border = new Vector4(32f, 32f, 32f, 32f);
                }

                bool upToDate = importer.textureType == TextureImporterType.Sprite &&
                                importer.spriteImportMode == SpriteImportMode.Single &&
                                !importer.mipmapEnabled &&
                                importer.alphaIsTransparency &&
                                importer.spriteBorder == border;
                if (upToDate)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static void LoadSprites()
        {
            panelSprite = LoadSprite("UI/panel_9slice.png");
            buttonSprite = LoadSprite("UI/button_9slice.png");
            outlineSprite = LoadSprite("UI/outline_9slice.png");
            minicardSprite = LoadSprite("UI/minicard_9slice.png");
            coinSprite = LoadSprite("Icons/coin.png");
            glowSprite = LoadSprite("Cards/card_glow.png");
            astrolabeSprite = LoadSprite("Background/astrolabe.png");
            motifEyeSprite = LoadSprite("Background/motif_eye.png");
            motifMoonSprite = LoadSprite("Background/motif_moon.png");
            projectileSprite = LoadSprite("Shield/projectile.png");
            sparkSprite = LoadSprite("FX/spark.png");
            soundOnSprite = LoadSprite("Icons/sound_on.png");
            soundOffSprite = LoadSprite("Icons/sound_off.png");
            pauseSprite = LoadSprite("Icons/pause.png");
            ringSprite = LoadSprite("Icons/ring.png");
            questionCardSprite = LoadSprite("Cards/question_card.png");
            sunCard = LoadSprite("Cards/card_sol.png");
            moonCard = LoadSprite("Cards/card_luna.png");
            eyeCard = LoadSprite("Cards/card_ojo.png");
            sunIcon = LoadSprite("Icons/sym_sol.png");
            moonIcon = LoadSprite("Icons/sym_luna.png");
            eyeIcon = LoadSprite("Icons/sym_ojo.png");
        }

        private static Sprite LoadSprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + relativePath);
        }

        /// <summary>
        /// Configura los sonidos de Assets/Audio para móvil: la música se lee poco a poco del disco (Streaming, Vorbis)
        /// y los efectos cortos se cargan en memoria (ADPCM) para sonar sin retraso. Todo en mono.
        /// </summary>
        private static void PrepareAudio()
        {
            if (!AssetDatabase.IsValidFolder(AudioFolder))
            {
                Debug.LogWarning("[1% de Probabilidad] No existe " + AudioFolder + ". El juego no tendrá sonido.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                bool music = path.Contains("/Music/");
                AudioClipLoadType loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                AudioCompressionFormat format = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (settings.loadType == loadType && settings.compressionFormat == format && importer.forceToMono)
                {
                    continue;
                }

                settings.loadType = loadType;
                settings.compressionFormat = format;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = music;
                importer.SaveAndReimport();
            }
        }

        private static void LoadAudio()
        {
            sfxClips = new AudioClip[AudioManager.SfxFileNames.Length];
            for (int i = 0; i < sfxClips.Length; i++)
            {
                sfxClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/Sfx/" + AudioManager.SfxFileNames[i] + ".wav");
            }

            tableMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/Music/music_table.ogg");
            shieldMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/Music/music_shield.ogg");
        }

        /// <summary>El juego es solo vertical: sin giro automático a horizontal.</summary>
        private static void ConfigurePortrait()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // En PC (ejecutable de Windows) el juego se abre en una ventana vertical, no en pantalla completa horizontal.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            // Nombre visible de la aplicación (título de la ventana y nombre del ejecutable).
            if (PlayerSettings.productName == "My project")
            {
                PlayerSettings.productName = "1% de Probabilidad";
            }
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

        // ------------------------------------------------------------------ Escena base

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
            panel.gameObject.AddComponent<ScreenFade>();

            // Fondo: astrolabio detrás de la carta y dos motivos apenas visibles. No cambian entre rondas.
            RectTransform ringsArea = NewRect("BackgroundRingsArea", root, V(-0.20f, 0.47f), V(1.20f, 0.95f));
            NewFitted("BackgroundRings", ringsArea, astrolabeSprite, 1f, Color.white);
            NewDecor("MotifEye", root, V(0.03f, 0.600f), V(0.17f, 0.660f), motifEyeSprite, 0.16f);
            NewDecor("MotifMoon", root, V(0.85f, 0.790f), V(0.97f, 0.850f), motifMoonSprite, 0.16f);

            // Barra superior: moneda, cantidad y estadísticas.
            Image coinIcon = NewDecor("CoinIcon", root, V(0.05f, 0.912f), V(0.13f, 0.968f), coinSprite, 1f);
            float coinsLeft = coinSprite != null ? 0.145f : 0.05f;
            Text coins = NewText("CoinsText", root, V(coinsLeft, 0.905f), V(0.54f, 0.975f),
                "100", 80, TextAnchor.MiddleLeft, Gold, boldFont);
            coins.resizeTextForBestFit = true;
            coins.resizeTextMinSize = 40;
            coins.resizeTextMaxSize = 80;

            Text unusedLabel;
            Button statsButton = NewButton("StatsButton", root, V(0.68f, 0.915f), V(0.95f, 0.965f),
                "Estadísticas", 34, ButtonBlue, out unusedLabel);
            Button pauseButton = NewIconButton("PauseButton", root, V(0.56f, 0.915f), V(0.66f, 0.965f), pauseSprite, "II");

            Text goal = NewText("GoalText", root, V(0.05f, 0.868f), V(0.95f, 0.900f),
                "Meta  10.000", 34, TextAnchor.MiddleLeft, Mist, bodyFont);

            // La carta del símbolo: resplandor detrás y la carta con su proporción (0,7), con el nombre dentro.
            RectTransform glowArea = NewRect("CardGlowArea", root, V(0.05f, 0.520f), V(0.95f, 0.895f));
            Image cardGlow = NewFitted("CardGlow", glowArea, glowSprite, 0.75f, Color.white);

            RectTransform symbolArea = NewRect("SymbolArea", root, V(0.20f, 0.565f), V(0.80f, 0.850f));
            Image symbolImage = NewImage("SymbolImage", symbolArea, V(0f, 0f), V(1f, 1f),
                sunCard != null ? Color.white : Color.gray);
            symbolImage.sprite = sunCard;
            symbolImage.raycastTarget = false;
            AspectRatioFitter fitter = symbolImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 0.7f;

            Text symbolCaption = NewText("CardNameText", symbolImage.transform, V(0.08f, 0.05f), V(0.92f, 0.19f),
                "EL SOL", 44, TextAnchor.MiddleCenter, Ivory, titleFont);
            symbolCaption.resizeTextForBestFit = true;
            symbolCaption.resizeTextMinSize = 18;
            symbolCaption.resizeTextMaxSize = 44;

            Text message = NewText("MessageText", root, V(0.06f, 0.455f), V(0.94f, 0.555f),
                string.Empty, 40, TextAnchor.MiddleCenter, Ivory, bodyFont);
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 24;
            message.resizeTextMaxSize = 40;

            // Solo visible al pulsar Debug (y el botón Debug solo existe en compilaciones de desarrollo).
            Text debug = NewText("DebugText", root, V(0.05f, 0.405f), V(0.95f, 0.452f),
                string.Empty, 24, TextAnchor.UpperLeft, DebugCyan, bodyFont);
            debug.resizeTextForBestFit = true;
            debug.resizeTextMinSize = 16;
            debug.resizeTextMaxSize = 24;

            // Registro: 8 mini cartas, la más reciente a la izquierda.
            NewText("HistoryTitle", root, V(0.05f, 0.357f), V(0.95f, 0.392f),
                "REGISTRO", 30, TextAnchor.MiddleLeft, Mist, titleFont);
            RectTransform row = NewRect("HistoryRow", root, V(0.05f, 0.255f), V(0.95f, 0.352f));
            Text emptyText = NewText("HistoryEmptyText", row, V(0f, 0f), V(1f, 1f),
                "Aún no hay rondas.", 30, TextAnchor.MiddleLeft, Mist, bodyFont);

            const int slotCount = 8;
            const float gap = 0.012f;
            float slotWidth = (1f - gap * (slotCount - 1)) / slotCount;
            Image[] slots = new Image[slotCount];
            Image[] outlines = new Image[slotCount];
            Image[] icons = new Image[slotCount];
            Text[] numbers = new Text[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                float x0 = i * (slotWidth + gap);
                Image slot = NewImage("Slot" + (i + 1), row, V(x0, 0f), V(x0 + slotWidth, 1f), Night);
                slot.raycastTarget = false;
                if (minicardSprite != null)
                {
                    slot.sprite = minicardSprite;
                    slot.type = Image.Type.Sliced;
                }

                Image outline = NewImage("Outline", slot.transform, V(0f, 0f), V(1f, 1f), GoldLine);
                outline.raycastTarget = false;
                if (outlineSprite != null)
                {
                    outline.sprite = outlineSprite;
                    outline.type = Image.Type.Sliced;
                    outline.fillCenter = false;
                    outline.pixelsPerUnitMultiplier = 2f;
                }
                else
                {
                    outline.enabled = false;
                }

                Image icon = NewImage("Icon", slot.transform, V(0.18f, 0.42f), V(0.82f, 0.88f), Gold);
                icon.sprite = sunIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                Text number = NewText("Number", slot.transform, V(0.02f, 0.06f), V(0.98f, 0.40f),
                    "+19", 30, TextAnchor.MiddleCenter, Gold, boldFont);
                number.resizeTextForBestFit = true;
                number.resizeTextMinSize = 16;
                number.resizeTextMaxSize = 30;

                slot.gameObject.SetActive(false);
                slots[i] = slot;
                outlines[i] = outline;
                icons[i] = icon;
                numbers[i] = number;
            }

            // Botones de decisión en la zona del pulgar.
            Text saveLabel;
            Text moderateLabel;
            Text allInLabel;
            Button saveButton = NewButton("SaveButton", root, V(0.05f, 0.050f), V(0.335f, 0.205f),
                "GUARDAR", 40, ButtonJade, out saveLabel);
            Button moderateButton = NewButton("ModerateButton", root, V(0.3575f, 0.050f), V(0.6425f, 0.205f),
                "APOSTAR 20", 40, ButtonAmber, out moderateLabel);
            Button allInButton = NewButton("AllInButton", root, V(0.665f, 0.050f), V(0.95f, 0.205f),
                "APOSTAR 100", 40, ButtonCarmine, out allInLabel);

            Button debugButton = NewButton("DebugButton", root, V(0.05f, 0.004f), V(0.25f, 0.040f),
                "Debug", 26, ButtonGray, out unusedLabel);

            Assign(screen, "coinsText", coins);
            Assign(screen, "goalText", goal);
            Assign(screen, "messageText", message);
            Assign(screen, "historyText", emptyText);
            Assign(screen, "symbolCaptionText", symbolCaption);
            Assign(screen, "debugText", debug);
            Assign(screen, "symbolImage", symbolImage);
            AssignOptional(screen, "sunCard", sunCard);
            AssignOptional(screen, "moonCard", moonCard);
            AssignOptional(screen, "eyeCard", eyeCard);
            AssignArray(screen, "historySlots", slots);
            AssignArray(screen, "historyOutlines", outlines);
            AssignArray(screen, "historyIcons", icons);
            AssignArray(screen, "historyNumbers", numbers);
            AssignOptional(screen, "sunIcon", sunIcon);
            AssignOptional(screen, "moonIcon", moonIcon);
            AssignOptional(screen, "eyeIcon", eyeIcon);
            AssignOptional(screen, "cardGlow", cardGlow);
            AssignOptional(screen, "coinIcon", coinIcon);
            AssignOptional(screen, "sparkSprite", sparkSprite);
            Assign(screen, "saveButton", saveButton);
            Assign(screen, "moderateButton", moderateButton);
            Assign(screen, "allInButton", allInButton);
            Assign(screen, "statsButton", statsButton);
            Assign(screen, "debugButton", debugButton);
            Assign(screen, "pauseButton", pauseButton);
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
            panel.gameObject.AddComponent<ScreenFade>();

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
                Image lane = NewPanel("Lane" + (i + 1), root, V(laneStart[i], 0.20f), V(laneStart[i] + laneWidth, 0.87f));
                lanes[i] = lane.rectTransform;

                Text unusedLabel;
                laneButtons[i] = NewButton("BlockButton" + (i + 1), root,
                    V(laneStart[i], 0.06f), V(laneStart[i] + laneWidth, 0.18f),
                    "BLOQUEAR", 36, ButtonBlue, out unusedLabel);
            }

            // El panel de introducción/resultado va al final para dibujarse encima de los carriles.
            Image overlay = NewPanel("Overlay", root, V(0.05f, 0.20f), V(0.95f, 0.87f));
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
            AssignOptional(screen, "projectileSprite", projectileSprite);
            AssignOptional(screen, "sparkSprite", sparkSprite);

            return screen;
        }

        // ------------------------------------------------------------------ Pantalla 3: Estadísticas

        private static StatsScreen BuildStatsPanel(Transform parent)
        {
            Image panel = NewImage("StatsPanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            StatsScreen screen = panel.gameObject.AddComponent<StatsScreen>();
            panel.gameObject.AddComponent<ScreenFade>();

            NewText("TitleText", root, V(0.05f, 0.930f), V(0.95f, 0.985f),
                "LOGROS Y ESTADÍSTICAS", 54, TextAnchor.MiddleCenter, Gold, titleFont);

            // En vertical, las tres columnas pasan a ser tres bloques apilados.
            Text stats = NewSection("StatsBlock", root, V(0.05f, 0.705f), V(0.95f, 0.920f));
            Text patterns = NewSection("PatternsBlock", root, V(0.05f, 0.430f), V(0.95f, 0.695f));
            Text achievements = NewSection("AchievementsBlock", root, V(0.05f, 0.200f), V(0.95f, 0.420f));

            // Ajustes de sonido (Etapa 4).
            NewAudioToggle("MusicToggle", root, V(0.05f, 0.122f), V(0.48f, 0.185f), AudioSettingToggle.Setting.Music);
            NewAudioToggle("EffectsToggle", root, V(0.52f, 0.122f), V(0.95f, 0.185f), AudioSettingToggle.Setting.Effects);

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

        /// <summary>Bloque de panel con un texto que se reduce solo si no cabe.</summary>
        private static Text NewSection(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image block = NewPanel(name, parent, anchorMin, anchorMax);
            Text text = NewText("Text", block.transform, V(0.05f, 0.05f), V(0.95f, 0.95f),
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
            panel.gameObject.AddComponent<ScreenFade>();

            RectTransform ringsArea = NewRect("BackgroundRingsArea", root, V(-0.20f, 0.45f), V(1.20f, 0.97f));
            NewFitted("BackgroundRings", ringsArea, astrolabeSprite, 1f, Color.white);

            Text title = NewText("TitleText", root, V(0.05f, 0.64f), V(0.95f, 0.78f),
                "FIN", 104, TextAnchor.MiddleCenter, Ivory, titleFont);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 60;
            title.resizeTextMaxSize = 104;
            Text summary = NewText("SummaryText", root, V(0.08f, 0.36f), V(0.92f, 0.62f),
                string.Empty, 42, TextAnchor.UpperCenter, Ivory, bodyFont);

            Text newGameLabel;
            Button newGame = NewButton("NewGameButton", root, V(0.10f, 0.245f), V(0.90f, 0.325f),
                "NUEVA PARTIDA", 44, ButtonJade, out newGameLabel);
            Text statsLabel;
            Button stats = NewButton("StatsButton", root, V(0.10f, 0.145f), V(0.90f, 0.220f),
                "ESTADÍSTICAS", 40, ButtonBlue, out statsLabel);
            Text menuLabel;
            Button menuButton = NewButton("MenuButton", root, V(0.10f, 0.045f), V(0.90f, 0.120f),
                "MENÚ PRINCIPAL", 40, ButtonBlue, out menuLabel);

            Assign(screen, "titleText", title);
            Assign(screen, "summaryText", summary);
            Assign(screen, "newGameButton", newGame);
            Assign(screen, "statsButton", stats);
            Assign(screen, "menuButton", menuButton);

            return screen;
        }

        // ------------------------------------------------------------------ Menú principal (Etapa 5)

        private static MenuScreen BuildMenuPanel(Transform parent)
        {
            Image panel = NewImage("MenuPanel", parent, V(0f, 0f), V(1f, 1f), Night);
            Transform root = panel.transform;
            MenuScreen screen = panel.gameObject.AddComponent<MenuScreen>();
            panel.gameObject.AddComponent<ScreenFade>();

            RectTransform ringsArea = NewRect("BackgroundRingsArea", root, V(-0.20f, 0.42f), V(1.20f, 0.93f));
            NewFitted("BackgroundRings", ringsArea, astrolabeSprite, 1f, Color.white);
            NewDecor("MotifEye", root, V(0.03f, 0.57f), V(0.17f, 0.63f), motifEyeSprite, 0.16f);
            NewDecor("MotifMoon", root, V(0.85f, 0.76f), V(0.97f, 0.82f), motifMoonSprite, 0.16f);

            Text title = NewText("TitleText", root, V(0.05f, 0.880f), V(0.95f, 0.955f),
                "1 % DE PROBABILIDAD", 76, TextAnchor.MiddleCenter, Gold, titleFont);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 40;
            title.resizeTextMaxSize = 76;

            // Carta decorativa que flota (el MenuScreen mueve su contenedor).
            RectTransform heroArea = NewRect("HeroArea", root, V(0.10f, 0.50f), V(0.90f, 0.86f));
            RectTransform glowArea = NewRect("CardGlowArea", heroArea, V(0f, 0f), V(1f, 1f));
            NewFitted("CardGlow", glowArea, glowSprite, 0.75f, Color.white);
            RectTransform cardArea = NewRect("CardArea", heroArea, V(0.22f, 0.10f), V(0.78f, 0.90f));
            Image card = NewFitted("HeroCard", cardArea, eyeCard, 0.7f, Color.white);
            if (card != null)
            {
                Text cardName = NewText("CardNameText", card.transform, V(0.08f, 0.05f), V(0.92f, 0.19f),
                    "1 %", 44, TextAnchor.MiddleCenter, Ivory, titleFont);
                cardName.resizeTextForBestFit = true;
                cardName.resizeTextMinSize = 18;
                cardName.resizeTextMaxSize = 44;
            }

            NewText("Subtitle", root, V(0.08f, 0.440f), V(0.92f, 0.495f),
                "Compra tu libertad. No confíes en lo que ves.", 34, TextAnchor.MiddleCenter, Mist, bodyFont);

            Text playLabel;
            Button play = NewButton("PlayButton", root, V(0.15f, 0.335f), V(0.85f, 0.420f),
                "JUGAR", 52, ButtonJade, out playLabel);
            Text howToLabel;
            Button howTo = NewButton("HowToButton", root, V(0.15f, 0.245f), V(0.85f, 0.315f),
                "CÓMO SE JUEGA", 40, ButtonBlue, out howToLabel);
            Text statsLabel;
            Button stats = NewButton("StatsButton", root, V(0.15f, 0.155f), V(0.85f, 0.225f),
                "ESTADÍSTICAS", 40, ButtonBlue, out statsLabel);

            NewAudioToggle("MusicToggle", root, V(0.15f, 0.075f), V(0.485f, 0.132f), AudioSettingToggle.Setting.Music);
            NewAudioToggle("EffectsToggle", root, V(0.515f, 0.075f), V(0.85f, 0.132f), AudioSettingToggle.Setting.Effects);

            NewText("VersionText", root, V(0.05f, 0.012f), V(0.95f, 0.045f),
                "Versión " + PlayerSettings.bundleVersion, 24, TextAnchor.MiddleCenter, Mist, bodyFont);

            Assign(screen, "playButton", play);
            Assign(screen, "howToButton", howTo);
            Assign(screen, "statsButton", stats);
            Assign(screen, "heroArea", heroArea);
            return screen;
        }

        // ------------------------------------------------------------------ Pausa (Etapa 5)

        private static PauseScreen BuildPausePanel(Transform parent)
        {
            // Fondo oscuro que cubre la mesa y bloquea los toques sobre ella.
            Image panel = NewImage("PausePanel", parent, V(0f, 0f), V(1f, 1f), new Color(0f, 0f, 0f, 0.72f));
            Transform root = panel.transform;
            PauseScreen screen = panel.gameObject.AddComponent<PauseScreen>();
            panel.gameObject.AddComponent<ScreenFade>();

            Image box = NewPanel("Box", root, V(0.08f, 0.25f), V(0.92f, 0.75f));
            Transform b = box.transform;

            NewText("TitleText", b, V(0.05f, 0.83f), V(0.95f, 0.95f), "PAUSA", 64, TextAnchor.MiddleCenter, Gold, titleFont);

            Text resumeLabel;
            Button resume = NewButton("ResumeButton", b, V(0.10f, 0.64f), V(0.90f, 0.78f), "CONTINUAR", 46, ButtonJade, out resumeLabel);
            Text howToLabel;
            Button howTo = NewButton("HowToButton", b, V(0.10f, 0.47f), V(0.90f, 0.60f), "CÓMO SE JUEGA", 40, ButtonBlue, out howToLabel);
            NewAudioToggle("MusicToggle", b, V(0.10f, 0.32f), V(0.485f, 0.43f), AudioSettingToggle.Setting.Music);
            NewAudioToggle("EffectsToggle", b, V(0.515f, 0.32f), V(0.90f, 0.43f), AudioSettingToggle.Setting.Effects);
            Text menuLabel;
            Button menu = NewButton("MenuButton", b, V(0.10f, 0.08f), V(0.90f, 0.22f), "MENÚ PRINCIPAL", 40, ButtonCarmine, out menuLabel);

            Assign(screen, "resumeButton", resume);
            Assign(screen, "howToButton", howTo);
            Assign(screen, "menuButton", menu);
            Assign(screen, "menuLabel", menuLabel);
            return screen;
        }

        // ------------------------------------------------------------------ Cómo se juega (Etapa 5)

        private static HowToScreen BuildHowToPanel(Transform parent)
        {
            Image panel = NewImage("HowToPanel", parent, V(0f, 0f), V(1f, 1f), new Color(0f, 0f, 0f, 0.8f));
            Transform root = panel.transform;
            HowToScreen screen = panel.gameObject.AddComponent<HowToScreen>();
            panel.gameObject.AddComponent<ScreenFade>();

            Image box = NewPanel("Box", root, V(0.06f, 0.16f), V(0.94f, 0.84f));
            Transform b = box.transform;

            Image pageImage = NewImage("PageImage", b, V(0.30f, 0.66f), V(0.70f, 0.93f), Color.white);
            pageImage.preserveAspect = true;
            pageImage.raycastTarget = false;

            Text title = NewText("TitleText", b, V(0.05f, 0.555f), V(0.95f, 0.640f), "TU OBJETIVO", 56, TextAnchor.MiddleCenter, Gold, titleFont);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 56;
            Text body = NewText("BodyText", b, V(0.08f, 0.22f), V(0.92f, 0.545f), string.Empty, 38, TextAnchor.MiddleCenter, Ivory, bodyFont);
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 24;
            body.resizeTextMaxSize = 38;
            Text pageText = NewText("PageText", b, V(0.30f, 0.15f), V(0.70f, 0.21f), "1 / 3", 30, TextAnchor.MiddleCenter, Mist, bodyFont);

            Text backLabel;
            Button back = NewButton("BackButton", b, V(0.06f, 0.03f), V(0.44f, 0.13f), "ATRÁS", 40, ButtonBlue, out backLabel);
            Text nextLabel;
            Button next = NewButton("NextButton", b, V(0.56f, 0.03f), V(0.94f, 0.13f), "SIGUIENTE", 40, ButtonJade, out nextLabel);

            Assign(screen, "titleText", title);
            Assign(screen, "bodyText", body);
            Assign(screen, "pageText", pageText);
            Assign(screen, "pageImage", pageImage);
            AssignArray(screen, "pageSprites", new UnityEngine.Object[] { coinSprite, sunCard, eyeCard });
            Assign(screen, "backButton", back);
            Assign(screen, "nextButton", next);
            Assign(screen, "nextLabel", nextLabel);
            return screen;
        }

        // ------------------------------------------------------------------ Preguntas de matemáticas

        /// <summary>
        /// Notificación a la derecha de la carta: carta con "?", anillo dorado que se vacía y cuánto se juega.
        /// Se crea como último hijo de la mesa para quedar encima de todo lo demás.
        /// </summary>
        private static QuestionNotification BuildQuestionNotification(Transform table)
        {
            Image root = NewImage("QuestionNotification", table, V(0.73f, 0.555f), V(0.97f, 0.700f), new Color(0f, 0f, 0f, 0f));
            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            QuestionNotification note = root.gameObject.AddComponent<QuestionNotification>();

            RectTransform ringArea = NewRect("RingArea", root.transform, V(0f, 0.22f), V(1f, 1f));
            Image ring = NewFitted("Ring", ringArea, ringSprite, 1f, Gold);
            if (ring != null)
            {
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = false;
                ring.fillAmount = 1f;
            }

            RectTransform bodyArea = NewRect("Body", ringArea, V(0.22f, 0.16f), V(0.78f, 0.84f));
            Image card = NewFitted("Card", bodyArea, questionCardSprite, 0.7f, Color.white);
            if (card == null)
            {
                // Sin la imagen: un panel con un "?" grande.
                Image fallback = NewPanel("Card", bodyArea, V(0.1f, 0f), V(0.9f, 1f));
                fallback.raycastTarget = false;
                NewText("Mark", fallback.transform, V(0f, 0f), V(1f, 1f), "?", 90, TextAnchor.MiddleCenter, Gold, titleFont);
            }

            Text amount = NewText("AmountText", root.transform, V(0f, 0f), V(1f, 0.22f), "±10", 46, TextAnchor.MiddleCenter, Gold, boldFont);
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 24;
            amount.resizeTextMaxSize = 46;

            Assign(note, "button", button);
            AssignOptional(note, "ring", ring);
            Assign(note, "amountText", amount);
            Assign(note, "body", bodyArea);
            return note;
        }

        /// <summary>Pantalla de la pregunta: título, apuesta, tiempo, enunciado, 4 opciones y resultado.</summary>
        private static QuestionScreen BuildQuestionPanel(Transform parent)
        {
            Image panel = NewImage("QuestionPanel", parent, V(0f, 0f), V(1f, 1f), new Color(0f, 0f, 0f, 0.78f));
            Transform root = panel.transform;
            QuestionScreen screen = panel.gameObject.AddComponent<QuestionScreen>();
            panel.gameObject.AddComponent<ScreenFade>();

            Image box = NewPanel("Box", root, V(0.06f, 0.20f), V(0.94f, 0.80f));
            Transform b = box.transform;

            Text title = NewText("TitleText", b, V(0.05f, 0.880f), V(0.95f, 0.960f), "PREGUNTA MISTERIOSA", 54, TextAnchor.MiddleCenter, Gold, titleFont);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 54;
            Text stake = NewText("StakeText", b, V(0.05f, 0.815f), V(0.95f, 0.870f), string.Empty, 32, TextAnchor.MiddleCenter, Mist, bodyFont);
            stake.resizeTextForBestFit = true;
            stake.resizeTextMinSize = 20;
            stake.resizeTextMaxSize = 32;

            Image barBack = NewImage("TimerBar", b, V(0.08f, 0.765f), V(0.78f, 0.785f), Hex(0x1B2456));
            barBack.raycastTarget = false;
            Image barFill = NewImage("Fill", barBack.transform, V(0f, 0f), V(1f, 1f), Gold);
            barFill.raycastTarget = false;
            Text timer = NewText("TimerText", b, V(0.80f, 0.745f), V(0.95f, 0.805f), "15.0 s", 32, TextAnchor.MiddleRight, Ivory, boldFont);

            Text question = NewText("QuestionText", b, V(0.05f, 0.600f), V(0.95f, 0.735f), "? = ?", 96, TextAnchor.MiddleCenter, Ivory, boldFont);
            question.resizeTextForBestFit = true;
            question.resizeTextMinSize = 48;
            question.resizeTextMaxSize = 96;

            Vector2[] mins = { V(0.06f, 0.410f), V(0.515f, 0.410f), V(0.06f, 0.230f), V(0.515f, 0.230f) };
            Vector2[] maxs = { V(0.485f, 0.570f), V(0.94f, 0.570f), V(0.485f, 0.390f), V(0.94f, 0.390f) };
            UnityEngine.Object[] buttons = new UnityEngine.Object[4];
            UnityEngine.Object[] labels = new UnityEngine.Object[4];
            for (int i = 0; i < 4; i++)
            {
                Text optionLabel;
                Button option = NewButton("Option" + (i + 1), b, mins[i], maxs[i], "0", 64, ButtonBlue, out optionLabel);
                // Al responder, el color lo pone QuestionScreen (verde, carmín o apagado): el botón no debe teñirlo.
                ColorBlock colors = option.colors;
                colors.disabledColor = Color.white;
                option.colors = colors;
                buttons[i] = option;
                labels[i] = optionLabel;
            }

            Text feedback = NewText("FeedbackText", b, V(0.05f, 0.135f), V(0.95f, 0.215f), string.Empty, 36, TextAnchor.MiddleCenter, Ivory, boldFont);
            feedback.resizeTextForBestFit = true;
            feedback.resizeTextMinSize = 22;
            feedback.resizeTextMaxSize = 36;

            Text continueLabel;
            Button continueButton = NewButton("ContinueButton", b, V(0.22f, 0.025f), V(0.78f, 0.120f), "CONTINUAR", 44, ButtonJade, out continueLabel);

            Assign(screen, "titleText", title);
            Assign(screen, "stakeText", stake);
            Assign(screen, "timerText", timer);
            Assign(screen, "questionText", question);
            Assign(screen, "feedbackText", feedback);
            Assign(screen, "timerFill", barFill.rectTransform);
            AssignArray(screen, "optionButtons", buttons);
            AssignArray(screen, "optionLabels", labels);
            Assign(screen, "continueButton", continueButton);
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

        /// <summary>Panel redondeado con borde dorado fino. Sin la imagen, un rectángulo de color tinta.</summary>
        private static Image NewPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image image = NewImage(name, parent, anchorMin, anchorMax, Ink);
            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            return image;
        }

        /// <summary>Imagen decorativa que conserva su proporción y no bloquea toques. Si no hay sprite, no se crea.</summary>
        private static Image NewDecor(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, float alpha)
        {
            if (sprite == null)
            {
                return null;
            }

            Image image = NewImage(name, parent, anchorMin, anchorMax, new Color(1f, 1f, 1f, alpha));
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Imagen que llena su contenedor manteniendo una proporción fija (ancho / alto).</summary>
        private static Image NewFitted(string name, Transform parent, Sprite sprite, float aspect, Color color)
        {
            if (sprite == null)
            {
                return null;
            }

            Image image = NewImage(name, parent, V(0f, 0f), V(1f, 1f), color);
            image.sprite = sprite;
            image.raycastTarget = false;
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;
            return image;
        }

        /// <summary>Botón cuadrado con un icono. Si falta la imagen, muestra el texto alternativo.</summary>
        private static Button NewIconButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Sprite sprite, string fallbackText)
        {
            Text label;
            Button button = NewButton(name, parent, anchorMin, anchorMax, string.Empty, 36, ButtonBlue, out label);
            if (sprite != null)
            {
                Image icon = NewImage("Icon", button.transform, V(0.22f, 0.22f), V(0.78f, 0.78f), Gold);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            else
            {
                label.text = fallbackText;
            }

            return button;
        }

        /// <summary>Botón cuadrado con icono de altavoz que silencia o activa todo el sonido.</summary>
        private static void NewSoundButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            Text label;
            Button button = NewButton("SoundButton", parent, anchorMin, anchorMax, string.Empty, 20, ButtonBlue, out label);
            Image icon = NewImage("Icon", button.transform, V(0.2f, 0.2f), V(0.8f, 0.8f), Gold);
            icon.sprite = soundOnSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (soundOnSprite == null)
            {
                icon.enabled = false;
                label.text = "♪";
            }

            SoundToggleButton toggle = button.gameObject.AddComponent<SoundToggleButton>();
            Assign(toggle, "icon", icon);
            AssignOptional(toggle, "soundOnSprite", soundOnSprite);
            AssignOptional(toggle, "soundOffSprite", soundOffSprite);
        }

        /// <summary>Botón "Música: SÍ / NO" o "Efectos: SÍ / NO".</summary>
        private static void NewAudioToggle(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            AudioSettingToggle.Setting setting)
        {
            Text label;
            string initialText = setting == AudioSettingToggle.Setting.Music ? "Música: SÍ" : "Efectos: SÍ";
            Button button = NewButton(name, parent, anchorMin, anchorMax, initialText, 32, Hex(0x1B2456), out label);
            AudioSettingToggle toggle = button.gameObject.AddComponent<AudioSettingToggle>();
            SerializedObject serialized = new SerializedObject(toggle);
            serialized.FindProperty("setting").enumValueIndex = (int)setting;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>
        /// Botón redondeado del color indicado, con un borde dorado fino encima.
        /// Al pulsarlo se oscurece un poco; desactivado se ve mucho más apagado.
        /// </summary>
        private static Button NewButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string label, int size, Color color, out Text labelText)
        {
            Image image = NewImage(name, parent, anchorMin, anchorMax, color);
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<PressScale>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.32f, 0.34f, 0.45f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (outlineSprite != null)
            {
                Image outline = NewImage("Outline", image.transform, V(0f, 0f), V(1f, 1f), new Color(GoldLine.r, GoldLine.g, GoldLine.b, 0.55f));
                outline.sprite = outlineSprite;
                outline.type = Image.Type.Sliced;
                outline.fillCenter = false;
                outline.raycastTarget = false;
            }

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

        /// <summary>Como Assign, pero sin error si el valor falta (imágenes opcionales).</summary>
        private static void AssignOptional(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            Assign(target, field, value);
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
