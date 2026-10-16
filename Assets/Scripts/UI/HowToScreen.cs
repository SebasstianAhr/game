using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// "Cómo se juega": 3 páginas cortas. Se muestra sola la primera vez que se pulsa JUGAR
    /// y siempre que se pida desde el menú o la pausa.
    /// No revela las reglas ocultas: solo avisa de que existen, para no estropear el descubrimiento.
    /// </summary>
    public class HowToScreen : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text pageText;
        [SerializeField] private Image pageImage;
        [SerializeField] private Sprite[] pageSprites;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;

        /// <summary>Se dispara al cerrar (con ENTENDIDO o con el botón "atrás" del teléfono).</summary>
        public event Action Closed;

        private const string SeenKey = "tutorial_seen";

        private string[] titles;
        private string[] bodies;
        private int page;
        private bool initialized;

        public static bool HasBeenSeen
        {
            get { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
        }

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            backButton.onClick.AddListener(PreviousPage);
            nextButton.onClick.AddListener(NextPage);
        }

        public void Open(GameConfig config)
        {
            titles = new[] { "TU OBJETIVO", "TUS DECISIONES", "UNA ADVERTENCIA" };
            bodies = new[]
            {
                "Empiezas con " + FormatNumber(config.startingCoins) + " monedas.\n" +
                "Llega a " + FormatNumber(config.goalCoins) + " para comprar tu libertad.\n\n" +
                "Si te quedas casi sin nada, tendrás una última oportunidad: El Escudo.",

                "GUARDAR: +" + config.saveReturn + " monedas seguras.\n" +
                "APOSTAR " + config.moderateStake + ": si ganas, cobras ×" + config.moderatePayoutMultiplier + ".\n" +
                "APOSTAR " + config.allInStake + ": si ganas, cobras ×" + config.allInPayoutMultiplier + ".\n\n" +
                "Cada ronda cuesta " + config.roundCost + (config.roundCost == 1 ? " moneda." : " monedas."),

                "Las probabilidades que ves no siempre son las reales.\n\n" +
                "Observa la carta de cada ronda y tu registro.\nLo que descubras será tu mejor arma."
            };

            page = 0;
            gameObject.SetActive(true);
            ShowPage();
        }

        /// <summary>Cierra y recuerda que ya se vio, para no mostrarla sola otra vez.</summary>
        public void Close()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);
            if (Closed != null)
            {
                Closed();
            }
        }

        private void NextPage()
        {
            if (page >= titles.Length - 1)
            {
                Close();
                return;
            }

            page++;
            ShowPage();
        }

        private void PreviousPage()
        {
            if (page > 0)
            {
                page--;
                ShowPage();
            }
        }

        private void ShowPage()
        {
            titleText.text = titles[page];
            bodyText.text = bodies[page];
            pageText.text = (page + 1) + " / " + titles.Length;
            backButton.gameObject.SetActive(page > 0);
            nextLabel.text = page == titles.Length - 1 ? "ENTENDIDO" : "SIGUIENTE";

            if (pageImage != null)
            {
                Sprite sprite = pageSprites != null && page < pageSprites.Length ? pageSprites[page] : null;
                pageImage.sprite = sprite;
                pageImage.enabled = sprite != null;
            }
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", ".");
        }
    }
}
