using System;
using System.Globalization;
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
        [Tooltip("Texto que se muestra en el registro cuando aún no hay rondas.")]
        [SerializeField] private Text historyText;
        [Tooltip("Nombre de la carta, escrito dentro de la carta (EL SOL, LA LUNA, EL OJO).")]
        [SerializeField] private Text symbolCaptionText;
        [SerializeField] private Text debugText;

        [Header("Carta del símbolo")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private Sprite sunCard;
        [SerializeField] private Sprite moonCard;
        [SerializeField] private Sprite eyeCard;

        [Header("Registro (mini cartas, la más reciente a la izquierda)")]
        [SerializeField] private Image[] historySlots;
        [SerializeField] private Image[] historyOutlines;
        [SerializeField] private Image[] historyIcons;
        [SerializeField] private Text[] historyNumbers;
        [SerializeField] private Sprite sunIcon;
        [SerializeField] private Sprite moonIcon;
        [SerializeField] private Sprite eyeIcon;

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

        // Colores de la paleta "tarot nocturno".
        private static readonly Color Ivory = new Color(0.929f, 0.902f, 0.827f);   // #EDE6D3
        private static readonly Color Gold = new Color(0.949f, 0.812f, 0.478f);    // #F2CF7A
        private static readonly Color GoldLine = new Color(0.788f, 0.647f, 0.353f); // #C9A55A
        private static readonly Color Indigo = new Color(0.106f, 0.141f, 0.337f);  // #1B2456
        private static readonly Color Faded = new Color(0.553f, 0.573f, 0.710f);   // #8D92B5
        private static readonly Color FadedLine = new Color(0.208f, 0.235f, 0.416f); // #353C6A
        private static readonly Color DarkSlot = new Color(0.043f, 0.059f, 0.141f); // #0B0F24
        private static readonly Color DisabledText = new Color(0.435f, 0.455f, 0.600f); // #6F7499

        // Espacio que no permite partir la línea: "APOSTAR 100" nunca se divide en dos renglones.
        private const string Nbsp = "\u00A0";

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

            coinsText.text = FormatNumber(session.Coins);
            goalText.text = "Meta  " + FormatNumber(config.goalCoins) + "      Ronda  " + (session.Round + 1);
            messageText.text = message;

            RefreshCard(session.CurrentSymbol);

            saveLabel.text = "GUARDAR\n+" + config.saveReturn + Nbsp + "monedas\n" +
                             Labels.Percent(config.GetDisplayedProbability(BetAction.Save));
            moderateLabel.text = "APOSTAR" + Nbsp + config.moderateStake + "\nGana" + Nbsp + "×" + config.moderatePayoutMultiplier +
                                 "\n" + Labels.Percent(config.GetDisplayedProbability(BetAction.Moderate));
            allInLabel.text = "APOSTAR" + Nbsp + config.allInStake + "\nGana" + Nbsp + "×" + config.allInPayoutMultiplier +
                              "\n" + Labels.Percent(config.GetDisplayedProbability(BetAction.AllIn));

            SetButtonState(saveButton, saveLabel, session.CanPerform(BetAction.Save));
            SetButtonState(moderateButton, moderateLabel, session.CanPerform(BetAction.Moderate));
            SetButtonState(allInButton, allInLabel, session.CanPerform(BetAction.AllIn));

            RefreshHistory(session);
            RefreshDebug();
        }

        private void RefreshCard(BackgroundSymbol symbol)
        {
            Sprite card = CardSprite(symbol);
            if (card != null)
            {
                symbolImage.sprite = card;
                symbolImage.color = Color.white;
            }
            else
            {
                // Sin la imagen de la carta, se vuelve al color del símbolo.
                symbolImage.sprite = null;
                symbolImage.color = Labels.SymbolColor(symbol);
            }

            symbolCaptionText.text = CardName(symbol);
        }

        /// <summary>Un botón desactivado oscurece también su texto, para que no parezca disponible.</summary>
        private static void SetButtonState(Button button, Text label, bool available)
        {
            button.interactable = available;
            label.color = available ? Ivory : DisabledText;
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

        /// <summary>
        /// Llena las mini cartas del registro: la más reciente a la izquierda.
        /// Ganó = dorada y brillante; perdió = apagada; guardó = neutra.
        /// </summary>
        private void RefreshHistory(GameSession session)
        {
            int count = session.History.Count;
            historyText.gameObject.SetActive(count == 0);
            if (count == 0)
            {
                historyText.text = "Aún no hay rondas.";
            }

            for (int slot = 0; slot < historySlots.Length; slot++)
            {
                int index = count - 1 - slot;
                bool used = index >= 0;
                historySlots[slot].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                RoundResult round = session.History[index];
                Color background;
                Color outline;
                Color ink;
                if (round.IsBet && round.Won)
                {
                    background = Indigo;
                    outline = GoldLine;
                    ink = Gold;
                }
                else if (round.IsBet)
                {
                    background = DarkSlot;
                    outline = FadedLine;
                    ink = Faded;
                }
                else
                {
                    background = DarkSlot;
                    outline = FadedLine;
                    ink = Ivory;
                }

                historySlots[slot].color = background;
                historyOutlines[slot].color = outline;
                historyIcons[slot].sprite = IconSprite(round.Symbol);
                historyIcons[slot].color = ink;
                historyNumbers[slot].text = Labels.Signed(round.NetChange).Replace("-", "−");
                historyNumbers[slot].color = ink;
            }
        }

        private Sprite CardSprite(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return sunCard;
                case BackgroundSymbol.Moon: return moonCard;
                default: return eyeCard;
            }
        }

        private Sprite IconSprite(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return sunIcon;
                case BackgroundSymbol.Moon: return moonIcon;
                default: return eyeIcon;
            }
        }

        private static string CardName(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return "EL SOL";
                case BackgroundSymbol.Moon: return "LA LUNA";
                default: return "EL OJO";
            }
        }

        /// <summary>10000 → "10.000" (separador de miles con punto, como en Colombia).</summary>
        private static string FormatNumber(int value)
        {
            return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", ".");
        }
    }
}
