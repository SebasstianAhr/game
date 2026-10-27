using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OnePercent
{
    /// <summary>
    /// Director del juego: crea las partidas, conecta las pantallas y decide a cuál ir.
    /// Flujo: Menú -> (primera vez: Cómo se juega) -> Mesa -> (capital bajo) Escudo -> Mesa -> ... -> Victoria o Derrota.
    /// Desde la mesa se puede abrir la Pausa. El botón "atrás" de Android se gestiona aquí (Update).
    /// Preguntas de matemáticas: una opcional (notificación con tiempo) cada 4–7 rondas y una obligatoria cada 10.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private GameConfig config;

        [Header("Pantallas")]
        [SerializeField] private TableScreen tableScreen;
        [SerializeField] private ShieldMinigameScreen shieldScreen;
        [SerializeField] private StatsScreen statsScreen;
        [SerializeField] private EndScreen endScreen;
        [SerializeField] private MenuScreen menuScreen;
        [SerializeField] private PauseScreen pauseScreen;
        [SerializeField] private HowToScreen howToScreen;

        [Header("Preguntas de matemáticas")]
        [SerializeField] private QuestionNotification questionNotification;
        [SerializeField] private QuestionScreen questionScreen;

        private GameSession session;
        private StatsTracker tracker;
        private string lastMessage = string.Empty;

        // Pausa antes de pasar al Escudo o a la pantalla final, para que se vea el resultado de la ronda.
        private const float TransitionDelay = 0.8f;
        private bool transitioning;

        // A dónde vuelve Estadísticas al pulsar VOLVER.
        private bool statsOpenedFromMenu;

        // Qué hacer cuando se cierre "Cómo se juega" (por ejemplo, empezar la partida la primera vez).
        private System.Action afterHowTo;

        // Preguntas: cuándo aparecen y la pregunta opcional que espera a ser aceptada.
        private QuestionScheduler questionScheduler;
        private System.Random questionRng;
        private MathQuestion pendingQuestion;

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            tracker = new StatsTracker(SaveService.Load(), config);

            tableScreen.Initialize();
            shieldScreen.Initialize();
            statsScreen.Initialize();
            endScreen.Initialize();
            menuScreen.Initialize();
            pauseScreen.Initialize();
            howToScreen.Initialize();
            questionNotification.Initialize();
            questionScreen.Initialize();

            tableScreen.ActionChosen += OnActionChosen;
            tableScreen.StatsRequested += OnStatsRequested;
            tableScreen.PauseRequested += OnPauseRequested;
            statsScreen.BackRequested += OnStatsBack;
            statsScreen.ResetRequested += OnStatsReset;
            endScreen.NewGameRequested += StartNewGame;
            endScreen.StatsRequested += OnStatsRequested;
            endScreen.MenuRequested += ShowMenu;
            menuScreen.PlayRequested += OnMenuPlay;
            menuScreen.HowToRequested += OnHowToRequested;
            menuScreen.StatsRequested += OnMenuStats;
            pauseScreen.ResumeRequested += OnResume;
            pauseScreen.HowToRequested += OnHowToRequested;
            pauseScreen.MenuRequested += OnPauseMenu;
            howToScreen.Closed += OnHowToClosed;
            questionNotification.Accepted += OnQuestionAccepted;
            questionNotification.Expired += OnQuestionExpired;

            ShowMenu();
        }

        private bool ValidateReferences()
        {
            bool ok = true;

            if (config == null)
            {
                Debug.LogWarning("[1% de Probabilidad] GameManager no tiene GameConfig asignado. Se usarán los valores por defecto. " +
                                 "Para editarlos, ejecuta el menú '1% de Probabilidad > Construir escena'.");
                config = ScriptableObject.CreateInstance<GameConfig>();
            }

            ok &= Require(tableScreen, "TableScreen");
            ok &= Require(shieldScreen, "ShieldMinigameScreen");
            ok &= Require(statsScreen, "StatsScreen");
            ok &= Require(endScreen, "EndScreen");
            ok &= Require(menuScreen, "MenuScreen");
            ok &= Require(pauseScreen, "PauseScreen");
            ok &= Require(howToScreen, "HowToScreen");
            ok &= Require(questionNotification, "QuestionNotification");
            ok &= Require(questionScreen, "QuestionScreen");

            if (!ok)
            {
                Debug.LogError("[1% de Probabilidad] Faltan referencias en el GameManager. " +
                               "Ejecuta de nuevo el menú '1% de Probabilidad > Construir escena' o arrástralas a mano en el Inspector.");
            }

            return ok;
        }

        private static bool Require(Object reference, string name)
        {
            if (reference == null)
            {
                Debug.LogError("[1% de Probabilidad] Referencia sin asignar en GameManager: " + name);
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------ Botón "atrás" de Android

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // Se atiende solo la pantalla que está delante de todo.
            // Con una pregunta abierta, "atrás" no hace nada: hay que responderla.
            if (questionScreen.IsOpen)
            {
                return;
            }

            if (howToScreen.IsOpen)
            {
                howToScreen.Close();
            }
            else if (pauseScreen.IsOpen)
            {
                OnResume();
            }
            else if (statsScreen.gameObject.activeSelf)
            {
                OnStatsBack();
            }
            else if (tableScreen.gameObject.activeSelf)
            {
                OnPauseRequested();
            }
            else if (menuScreen.IsOpen)
            {
                Application.Quit();
            }

            // En El Escudo y en la pantalla final, "atrás" no hace nada para no perder la partida por error.
        }

        // ------------------------------------------------------------------ Menú, ayuda y pausa

        private void ShowMenu()
        {
            StopAllCoroutines();
            transitioning = false;
            session = null;

            tableScreen.Hide();
            shieldScreen.Hide();
            statsScreen.Hide();
            endScreen.Hide();
            pauseScreen.Close();
            HideQuestions();
            menuScreen.Show();
            AudioManager.PlayMusic(Music.Table);
        }

        private void OnMenuPlay()
        {
            if (!HowToScreen.HasBeenSeen)
            {
                // La primera vez se explica el juego y, al cerrar, empieza la partida.
                afterHowTo = StartNewGame;
                howToScreen.Open(config);
                return;
            }

            StartNewGame();
        }

        private void OnHowToRequested()
        {
            afterHowTo = null;
            howToScreen.Open(config);
        }

        private void OnHowToClosed()
        {
            System.Action next = afterHowTo;
            afterHowTo = null;
            if (next != null)
            {
                next();
            }
        }

        private void OnMenuStats()
        {
            statsOpenedFromMenu = true;
            menuScreen.Hide();
            statsScreen.Show(tracker.Data, config);
        }

        private void OnPauseRequested()
        {
            if (transitioning || session == null || tableScreen.IsBusy || questionScreen.IsOpen)
            {
                return;
            }

            pauseScreen.Open();
            questionNotification.Paused = true;
        }

        private void OnResume()
        {
            pauseScreen.Close();
            questionNotification.Paused = false;
        }

        private void OnPauseMenu()
        {
            // Se abandona la partida en curso (no cuenta como derrota).
            ShowMenu();
        }

        // ------------------------------------------------------------------ Partida

        private void StartNewGame()
        {
            int? seed = config.useFixedSeed ? (int?)config.fixedSeed : null;
            session = new GameSession(config, seed);
            questionRng = seed.HasValue ? new System.Random(seed.Value + 1) : new System.Random();
            questionScheduler = new QuestionScheduler(config, questionRng);
            HideQuestions();
            lastMessage = "Elige una acción. Observa con atención: lo que ves no siempre es lo que parece.";

            menuScreen.Hide();
            pauseScreen.Close();
            endScreen.Hide();
            statsScreen.Hide();
            shieldScreen.Hide();

            tableScreen.Show();
            tableScreen.Refresh(session, config, lastMessage);
            AudioManager.PlayMusic(Music.Table);
        }

        private void OnActionChosen(BetAction action)
        {
            if (transitioning || session == null || questionScreen.IsOpen || !session.CanPerform(action))
            {
                return;
            }

            RoundResult result = session.Play(action);
            List<AchievementInfo> unlocked = tracker.RegisterRound(result);
            PlayAchievementSound(unlocked);
            SaveService.Save(tracker.Data);

            lastMessage = BuildRoundMessage(result, unlocked);

            // Siempre se muestra primero el resultado en la mesa (con su animación).
            tableScreen.Refresh(session, config, lastMessage);

            if (session.Outcome != GameOutcome.InProgress)
            {
                HideQuestions();
                StartCoroutine(AfterDelay(FinishGame));
            }
            else if (session.ShouldOfferMinigame)
            {
                HideQuestions();
                StartCoroutine(AfterDelay(StartMinigame));
            }
            else
            {
                CheckQuestionTriggers();
            }
        }

        // ------------------------------------------------------------------ Preguntas de matemáticas

        /// <summary>Después de cada ronda: ¿toca una pregunta obligatoria u ofrecer una opcional?</summary>
        private void CheckQuestionTriggers()
        {
            QuestionTrigger trigger = questionScheduler.OnRoundPlayed(session.Round);
            if (trigger == QuestionTrigger.Mandatory)
            {
                // La obligatoria se abre sola, un instante después de ver el resultado de la ronda.
                questionNotification.Hide();
                pendingQuestion = null;
                MathQuestion mandatory = MathQuestionGenerator.Generate(session.Round, true, questionRng, config);
                StartCoroutine(AfterDelay(() => OpenQuestion(mandatory)));
            }
            else if (trigger == QuestionTrigger.Optional && !questionNotification.IsShowing)
            {
                // La opcional se crea ya (para mostrar cuánto se juega), pero el enunciado queda oculto hasta aceptarla.
                pendingQuestion = MathQuestionGenerator.Generate(session.Round, false, questionRng, config);
                questionNotification.Show(pendingQuestion.Reward, config.questionOfferSeconds);
            }
        }

        private void OnQuestionAccepted()
        {
            if (pendingQuestion == null || session == null || session.Outcome != GameOutcome.InProgress || transitioning)
            {
                pendingQuestion = null;
                return;
            }

            MathQuestion question = pendingQuestion;
            pendingQuestion = null;
            OpenQuestion(question);
        }

        private void OnQuestionExpired()
        {
            pendingQuestion = null;
        }

        private void OpenQuestion(MathQuestion question)
        {
            if (session == null || session.Outcome != GameOutcome.InProgress)
            {
                return;
            }

            AudioManager.Play(question.Mandatory ? Sfx.ShieldAlert : Sfx.CardFlip);
            questionScreen.Open(question, config.questionAnswerSeconds, correct => OnQuestionAnswered(question, correct));
        }

        private void OnQuestionAnswered(MathQuestion question, bool correct)
        {
            if (session == null)
            {
                return;
            }

            session.ApplyQuestionResult(correct ? question.Reward : -question.Reward);

            string kind = question.Mandatory ? "Pregunta obligatoria" : "Pregunta misteriosa";
            lastMessage = correct
                ? kind + ": ¡correcta! +" + question.Reward + " monedas."
                : kind + ": fallada. -" + question.Reward + " monedas.";

            // Las monedas suben o bajan contando en la mesa.
            tableScreen.Refresh(session, config, lastMessage);

            if (session.Outcome != GameOutcome.InProgress)
            {
                HideQuestions();
                StartCoroutine(AfterDelay(FinishGame));
            }
            else if (session.ShouldOfferMinigame)
            {
                HideQuestions();
                StartCoroutine(AfterDelay(StartMinigame));
            }
        }

        /// <summary>Quita la notificación y cierra la pregunta (cambio de partida, menú o fin).</summary>
        private void HideQuestions()
        {
            pendingQuestion = null;
            questionNotification.Hide();
            questionNotification.Paused = false;
            questionScreen.ForceClose();
        }

        /// <summary>Espera un momento (sin aceptar más acciones) y luego ejecuta el siguiente paso.</summary>
        private IEnumerator AfterDelay(System.Action next)
        {
            transitioning = true;
            yield return new WaitForSecondsRealtime(TransitionDelay);
            transitioning = false;
            next();
        }

        private void StartMinigame()
        {
            tableScreen.Hide();
            shieldScreen.Show();
            shieldScreen.Begin(config, session.Coins, OnMinigameFinished);
            AudioManager.PlayMusic(Music.Shield);
            AudioManager.Play(Sfx.ShieldAlert);
        }

        private void OnMinigameFinished(int reward)
        {
            session.ApplyMinigameReward(reward);
            List<AchievementInfo> unlocked = tracker.RegisterMinigame(reward, session.Coins);
            PlayAchievementSound(unlocked);
            SaveService.Save(tracker.Data);

            StringBuilder builder = new StringBuilder();
            builder.Append(reward > 0
                ? "El Escudo te dio " + reward + " monedas."
                : "El Escudo no te dio monedas.");
            AppendAchievements(builder, unlocked);
            lastMessage = builder.ToString();

            shieldScreen.Hide();

            if (session.Outcome != GameOutcome.InProgress)
            {
                FinishGame();
            }
            else
            {
                tableScreen.Show();
                tableScreen.Refresh(session, config, lastMessage);
                AudioManager.PlayMusic(Music.Table);
            }
        }

        private static void PlayAchievementSound(List<AchievementInfo> unlocked)
        {
            if (unlocked != null && unlocked.Count > 0)
            {
                AudioManager.Play(Sfx.Achievement, 0.8f);
            }
        }

        private void FinishGame()
        {
            HideQuestions();
            tableScreen.Hide();
            shieldScreen.Hide();
            pauseScreen.Close();

            bool victory = session.Outcome == GameOutcome.Victory;
            tracker.RegisterGameEnd(victory, session.PeakCoins);
            SaveService.Save(tracker.Data);

            endScreen.Show(session.Outcome, session, config);
            AudioManager.PlayMusic(Music.None);
            AudioManager.Play(victory ? Sfx.Victory : Sfx.Bankrupt);
        }

        // ------------------------------------------------------------------ Estadísticas

        private void OnStatsRequested()
        {
            if (transitioning || questionScreen.IsOpen)
            {
                return;
            }

            statsOpenedFromMenu = false;
            tableScreen.Hide();
            endScreen.Hide();
            statsScreen.Show(tracker.Data, config);
        }

        private void OnStatsBack()
        {
            statsScreen.Hide();

            if (statsOpenedFromMenu || session == null)
            {
                menuScreen.Show();
            }
            else if (session.Outcome == GameOutcome.InProgress)
            {
                tableScreen.Show();
                tableScreen.Refresh(session, config, lastMessage);
            }
            else
            {
                endScreen.Show(session.Outcome, session, config);
            }
        }

        private void OnStatsReset()
        {
            SaveService.Delete();
            tracker.ResetAll();
            statsScreen.Show(tracker.Data, config);
        }

        // ------------------------------------------------------------------ Mensajes

        private string BuildRoundMessage(RoundResult result, List<AchievementInfo> unlocked)
        {
            StringBuilder builder = new StringBuilder();

            if (!result.IsBet)
            {
                builder.Append("Guardaste. Cobras +").Append(result.GrossGain).Append(" monedas.");
            }
            else if (result.Won)
            {
                builder.Append("¡GANASTE! Apostaste ").Append(result.Stake)
                    .Append(" y cobras ").Append(result.GrossGain).Append(" monedas.");
            }
            else
            {
                builder.Append("Perdiste las ").Append(result.Stake).Append(" monedas apostadas.");
            }

            if (result.Fee > 0)
            {
                builder.Append("\nCoste de ronda: -").Append(result.Fee).Append('.');
            }

            AppendAchievements(builder, unlocked);
            return builder.ToString();
        }

        private static void AppendAchievements(StringBuilder builder, List<AchievementInfo> unlocked)
        {
            if (unlocked == null)
            {
                return;
            }

            for (int i = 0; i < unlocked.Count; i++)
            {
                builder.Append("\n\n<color=#FFD54A>Logro desbloqueado: ").Append(unlocked[i].Name).Append("</color>");
            }
        }
    }
}
