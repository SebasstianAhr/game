using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Pantalla 3: logros y estadísticas.</summary>
    public class StatsScreen : MonoBehaviour
    {
        [SerializeField] private Text statsText;
        [SerializeField] private Text patternsText;
        [SerializeField] private Text achievementsText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text resetLabel;

        public event Action BackRequested;
        public event Action ResetRequested;

        private const string ResetDefaultLabel = "Borrar progreso";
        private const string ResetArmedLabel = "¿Seguro? Pulsa otra vez";

        private bool initialized;
        private bool resetArmed;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            backButton.onClick.AddListener(() =>
            {
                if (BackRequested != null)
                {
                    BackRequested();
                }
            });

            resetButton.onClick.AddListener(OnResetClicked);
        }

        public void Show(SaveData data, GameConfig config)
        {
            gameObject.SetActive(true);
            resetArmed = false;
            resetLabel.text = ResetDefaultLabel;

            statsText.text = BuildStats(data);
            patternsText.text = BuildPatterns(data, config);
            achievementsText.text = BuildAchievements(data);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // El botón "atrás" de Android llega como la tecla Escape. Aquí equivale a pulsar VOLVER.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && BackRequested != null)
            {
                BackRequested();
            }
        }

        private void OnResetClicked()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                resetLabel.text = ResetArmedLabel;
                return;
            }

            resetArmed = false;
            resetLabel.text = ResetDefaultLabel;
            if (ResetRequested != null)
            {
                ResetRequested();
            }
        }

        private static string BuildStats(SaveData data)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<b>ESTADÍSTICAS</b>\n\n");
            builder.Append("Monedas totales obtenidas: ").Append(data.totalCoinsEarned).Append("\n\n");
            builder.Append("Partidas jugadas: ").Append(data.gamesPlayed).Append('\n');
            builder.Append("Victorias: ").Append(data.victories).Append('\n');
            builder.Append("Derrotas: ").Append(data.defeats).Append("\n\n");
            builder.Append("Veces que engañaste al sistema: ").Append(data.timesCheatedSystem).Append("\n\n");
            builder.Append("Mayor cantidad en una partida: ").Append(data.bestCoins);
            return builder.ToString();
        }

        private static string BuildPatterns(SaveData data, GameConfig config)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<b>PATRONES</b>\n");
            builder.Append("Apuestas moderadas ganadas / jugadas (mostrada: ")
                .Append(Labels.Percent(config.moderateDisplayedProbability)).Append(")\n\n");

            for (int i = 0; i < Labels.SymbolCount; i++)
            {
                BackgroundSymbol symbol = (BackgroundSymbol)i;
                AppendPatternLine(builder, Labels.SymbolName(symbol) + " en el fondo",
                    data.symbolModerateWins[i], data.symbolModerateBets[i], config.patternMinSamples);
            }

            builder.Append('\n');
            AppendPatternLine(builder, "Tras " + config.streakLength + " derrotas seguidas",
                data.afterLossStreakWins, data.afterLossStreakBets, config.patternMinSamples);
            AppendPatternLine(builder, "Tras " + config.streakLength + " victorias seguidas",
                data.afterWinStreakWins, data.afterWinStreakBets, config.patternMinSamples);

            builder.Append("\n\nUn patrón se marca como descubierto con al menos ")
                .Append(config.patternMinSamples).Append(" apuestas registradas.");
            return builder.ToString();
        }

        private static void AppendPatternLine(StringBuilder builder, string title, int wins, int bets, int minSamples)
        {
            builder.Append(title).Append(": ").Append(wins).Append(" / ").Append(bets);
            if (bets > 0)
            {
                builder.Append(" (").Append(Mathf.RoundToInt(100f * wins / bets)).Append(" %)");
            }

            if (bets >= minSamples)
            {
                builder.Append("  <color=#F2CF7A>[DESCUBIERTO]</color>");
            }

            builder.Append('\n');
        }

        private static string BuildAchievements(SaveData data)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<b>LOGROS</b>\n\n");

            for (int i = 0; i < Achievements.All.Length; i++)
            {
                AchievementInfo info = Achievements.All[i];
                bool unlocked = data.unlockedAchievements.Contains(info.Id);

                if (unlocked)
                {
                    builder.Append("<color=#FFD54A>[X] ").Append(info.Name).Append("</color>\n");
                }
                else
                {
                    builder.Append("[  ] ").Append(info.Name).Append('\n');
                }

                builder.Append("     ").Append(info.Description).Append("\n\n");
            }

            return builder.ToString();
        }
    }
}
