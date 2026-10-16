using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Pantalla 1: Reto Central - Mesa de decisiones.</summary>
    public class TableScreen : MonoBehaviour
    {
        [Header("Textos")]
        [SerializeField] private Text coinsText;
        [SerializeField] private Text goalText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text historyText;
        [SerializeField] private Text symbolCaptionText;
        [SerializeField] private Text debugText;

        [Header("Símbolo del fondo")]
        [SerializeField] private Image symbolImage;

        [Header("Botones")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button moderateButton;
        [SerializeField] private Button allInButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button debugButton;

        [Header("Etiquetas de los botones de decisión")]
        [SerializeField] private Text saveLabel;
        [SerializeField] private Text moderateLabel;
        [SerializeField] private Text allInLabel;

        public event Action<BetAction> ActionChosen;
        public event Action StatsRequested;

        // En vertical caben menos líneas sin que el texto quede diminuto.
        private const int HistoryLines = 6;

        private bool initialized;
        private bool debugVisible;
        private GameSession lastSession;
        private GameConfig lastConfig;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            saveButton.onClick.AddListener(() => Raise(BetAction.Save));
            moderateButton.onClick.AddListener(() => Raise(BetAction.Moderate));
            allInButton.onClick.AddListener(() => Raise(BetAction.AllIn));
            statsButton.onClick.AddListener(() =>
            {
                if (StatsRequested != null)
                {
                    StatsRequested();
                }
            });
            debugButton.onClick.AddListener(ToggleDebug);

            // El botón Debug revela la probabilidad real: solo existe en el editor y en compilaciones de desarrollo.
            debugButton.gameObject.SetActive(Debug.isDebugBuild);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Refresh(GameSession session, GameConfig config, string message)
        {
            lastSession = session;
            lastConfig = config;

            coinsText.text = "Monedas: " + session.Coins;
            goalText.text = "Meta: " + config.goalCoins + "   |   Ronda: " + (session.Round + 1);
            messageText.text = message;

            symbolImage.color = Labels.SymbolColor(session.CurrentSymbol);
            symbolCaptionText.text = "Símbolo del fondo: " + Labels.SymbolName(session.CurrentSymbol);

            saveLabel.text = "GUARDAR\n+" + config.saveReturn + " monedas\n(" +
                             Labels.Percent(config.GetDisplayedProbability(BetAction.Save)) + ")";
            moderateLabel.text = "APOSTAR " + config.moderateStake + "\nGana x" + config.moderatePayoutMultiplier +
                                 "\n(" + Labels.Percent(config.GetDisplayedProbability(BetAction.Moderate)) + ")";
            allInLabel.text = "APOSTAR " + config.allInStake + "\nGana x" + config.allInPayoutMultiplier +
                              "\n(" + Labels.Percent(config.GetDisplayedProbability(BetAction.AllIn)) + ")";

            saveButton.interactable = session.CanPerform(BetAction.Save);
            moderateButton.interactable = session.CanPerform(BetAction.Moderate);
            allInButton.interactable = session.CanPerform(BetAction.AllIn);

            historyText.text = BuildHistory(session);
            RefreshDebug();
        }

        private void Raise(BetAction action)
        {
            if (ActionChosen != null)
            {
                ActionChosen(action);
            }
        }

        private void ToggleDebug()
        {
            debugVisible = !debugVisible;
            RefreshDebug();
        }

        private void RefreshDebug()
        {
            debugText.gameObject.SetActive(debugVisible);
            if (!debugVisible || lastSession == null || lastConfig == null)
            {
                return;
            }

            debugText.text =
                "[DEBUG] Probabilidad REAL ahora (entre paréntesis, la mostrada)\n" +
                "Moderada: " + Labels.Percent(lastSession.GetRealProbability(BetAction.Moderate)) +
                " (" + Labels.Percent(lastConfig.GetDisplayedProbability(BetAction.Moderate)) + ")   " +
                "Todo o nada: " + Labels.Percent(lastSession.GetRealProbability(BetAction.AllIn)) +
                " (" + Labels.Percent(lastConfig.GetDisplayedProbability(BetAction.AllIn)) + ")\n" +
                "Racha: " + lastSession.ConsecutiveWins + " victorias / " + lastSession.ConsecutiveLosses + " derrotas";
        }

        private static string BuildHistory(GameSession session)
        {
            if (session.History.Count == 0)
            {
                return "Aún no hay rondas.";
            }

            StringBuilder builder = new StringBuilder();
            int shown = 0;
            for (int i = session.History.Count - 1; i >= 0 && shown < HistoryLines; i--, shown++)
            {
                RoundResult round = session.History[i];
                string outcome;
                if (!round.IsBet)
                {
                    outcome = "<color=#A9AECB>Guardó</color>";
                }
                else if (round.Won)
                {
                    outcome = "<color=#F2CF7A>GANÓ</color>";
                }
                else
                {
                    outcome = "<color=#E8798A>PERDIÓ</color>";
                }

                builder.Append('#').Append(round.RoundNumber).Append("  ")
                    .Append(Labels.SymbolName(round.Symbol)).Append("  ")
                    .Append(round.IsBet ? "Apuesta " + round.Stake : "Guardar").Append("  ")
                    .Append(outcome).Append("  ")
                    .Append(Labels.Signed(round.NetChange)).Append('\n');
            }

            return builder.ToString();
        }
    }
}
