using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Director del juego: crea las partidas, conecta las pantallas y decide a cuál ir.
    /// Flujo: Mesa -> (capital bajo) Minijuego -> Mesa -> ... -> Victoria o Derrota.
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

        private GameSession session;
        private StatsTracker tracker;
        private string lastMessage = string.Empty;

        // Pausa antes de pasar al Escudo o a la pantalla final, para que se vea el resultado de la ronda.
        private const float TransitionDelay = 0.8f;
        private bool transitioning;

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

            tableScreen.ActionChosen += OnActionChosen;
            tableScreen.StatsRequested += OnStatsRequested;
            statsScreen.BackRequested += OnStatsBack;
            statsScreen.ResetRequested += OnStatsReset;
            endScreen.NewGameRequested += StartNewGame;
            endScreen.StatsRequested += OnStatsRequested;

            StartNewGame();
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

        private void StartNewGame()
        {
            int? seed = config.useFixedSeed ? (int?)config.fixedSeed : null;
            session = new GameSession(config, seed);
            lastMessage = "Elige una acción. Observa con atención: lo que ves no siempre es lo que parece.";

            endScreen.Hide();
            statsScreen.Hide();
            shieldScreen.Hide();

            tableScreen.Show();
            tableScreen.Refresh(session, config, lastMessage);
        }

        private void OnActionChosen(BetAction action)
        {
            if (transitioning || session == null || !session.CanPerform(action))
            {
                return;
            }

            RoundResult result = session.Play(action);
            List<AchievementInfo> unlocked = tracker.RegisterRound(result);
            SaveService.Save(tracker.Data);

            lastMessage = BuildRoundMessage(result, unlocked);

            // Siempre se muestra primero el resultado en la mesa (con su animación).
            tableScreen.Refresh(session, config, lastMessage);

            if (session.Outcome != GameOutcome.InProgress)
            {
                StartCoroutine(AfterDelay(FinishGame));
            }
            else if (session.ShouldOfferMinigame)
            {
                StartCoroutine(AfterDelay(StartMinigame));
            }
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
        }

        private void OnMinigameFinished(int reward)
        {
            session.ApplyMinigameReward(reward);
            List<AchievementInfo> unlocked = tracker.RegisterMinigame(reward, session.Coins);
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
            }
        }

        private void FinishGame()
        {
            tableScreen.Hide();
            shieldScreen.Hide();

            bool victory = session.Outcome == GameOutcome.Victory;
            tracker.RegisterGameEnd(victory, session.PeakCoins);
            SaveService.Save(tracker.Data);

            endScreen.Show(session.Outcome, session, config);
        }

        private void OnStatsRequested()
        {
            if (transitioning)
            {
                return;
            }

            tableScreen.Hide();
            endScreen.Hide();
            statsScreen.Show(tracker.Data, config);
        }

        private void OnStatsBack()
        {
            statsScreen.Hide();

            if (session.Outcome == GameOutcome.InProgress)
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
