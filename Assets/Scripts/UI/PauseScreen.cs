using System;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Pausa encima de la mesa: continuar, cómo se juega, ajustes de sonido y volver al menú.
    /// Volver al menú abandona la partida, así que pide confirmación (hay que pulsar dos veces).
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text menuLabel;

        public event Action ResumeRequested;
        public event Action HowToRequested;
        public event Action MenuRequested;

        private const string MenuDefaultLabel = "MENÚ PRINCIPAL";
        private const string MenuArmedLabel = "¿Salir? Se pierde la partida";

        private bool initialized;
        private bool menuArmed;

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
            resumeButton.onClick.AddListener(() => { if (ResumeRequested != null) ResumeRequested(); });
            howToButton.onClick.AddListener(() => { if (HowToRequested != null) HowToRequested(); });
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Open()
        {
            menuArmed = false;
            menuLabel.text = MenuDefaultLabel;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnMenuClicked()
        {
            if (!menuArmed)
            {
                menuArmed = true;
                menuLabel.text = MenuArmedLabel;
                return;
            }

            menuArmed = false;
            menuLabel.text = MenuDefaultLabel;
            if (MenuRequested != null)
            {
                MenuRequested();
            }
        }
    }
}
