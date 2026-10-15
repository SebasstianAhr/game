using System;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Pantalla de victoria o derrota al terminar una partida.</summary>
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button statsButton;

        public event Action NewGameRequested;
        public event Action StatsRequested;

        private bool initialized;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            newGameButton.onClick.AddListener(() =>
            {
                if (NewGameRequested != null)
                {
                    NewGameRequested();
                }
            });

            statsButton.onClick.AddListener(() =>
            {
                if (StatsRequested != null)
                {
                    StatsRequested();
                }
            });
        }

        public void Show(GameOutcome outcome, GameSession session, GameConfig config)
        {
            gameObject.SetActive(true);

            if (outcome == GameOutcome.Victory)
            {
                titleText.text = "¡VICTORIA!";
                titleText.color = new Color(0.42f, 0.90f, 0.46f);
                summaryText.text = "Alcanzaste " + config.goalCoins + " monedas y compraste tu libertad.\n\n" +
                                   "Rondas jugadas: " + session.Round + "\n" +
                                   "Capital final: " + session.Coins;
            }
            else
            {
                titleText.text = "BANCARROTA";
                titleText.color = new Color(1f, 0.42f, 0.42f);
                summaryText.text = "Te quedaste sin monedas.\n\n" +
                                   "Rondas jugadas: " + session.Round + "\n" +
                                   "Mayor capital de esta partida: " + session.PeakCoins;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
