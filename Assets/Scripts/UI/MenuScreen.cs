using System;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Menú principal: jugar, cómo se juega, estadísticas y ajustes de sonido. La carta flota suavemente.</summary>
    public class MenuScreen : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button statsButton;
        [Tooltip("Contenedor de la carta decorativa, que sube y baja lentamente.")]
        [SerializeField] private RectTransform heroArea;

        public event Action PlayRequested;
        public event Action HowToRequested;
        public event Action StatsRequested;

        private bool initialized;
        private Vector2 heroBasePosition;

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
            playButton.onClick.AddListener(() => { if (PlayRequested != null) PlayRequested(); });
            howToButton.onClick.AddListener(() => { if (HowToRequested != null) HowToRequested(); });
            statsButton.onClick.AddListener(() => { if (StatsRequested != null) StatsRequested(); });
            if (heroArea != null)
            {
                heroBasePosition = heroArea.anchoredPosition;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (heroArea == null || !initialized)
            {
                return;
            }

            float t = Time.unscaledTime;
            heroArea.anchoredPosition = heroBasePosition + new Vector2(0f, Mathf.Sin(t * 1.2f) * 12f);
            heroArea.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 1.5f);
        }
    }
}
