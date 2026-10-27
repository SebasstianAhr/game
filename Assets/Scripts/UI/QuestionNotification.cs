using System;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Notificación de "pregunta misteriosa" junto a la carta de la mesa.
    /// Muestra cuánto se juega (±monedas) pero NO la pregunta. Un anillo dorado se vacía con el tiempo;
    /// si el jugador la toca antes de que se acabe, la acepta. Si no, desaparece.
    /// Vive dentro de la mesa: si la mesa se oculta (Escudo, estadísticas), su tiempo se congela.
    /// </summary>
    public class QuestionNotification : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image ring;
        [SerializeField] private Text amountText;
        [Tooltip("Parte que late suavemente para llamar la atención.")]
        [SerializeField] private RectTransform body;

        public event Action Accepted;
        public event Action Expired;

        private float total;
        private float remaining;
        private bool showing;
        private bool initialized;
        private float appearTime;

        /// <summary>Mientras está en pausa (menú de pausa abierto), el tiempo no corre.</summary>
        public bool Paused { get; set; }

        public bool IsShowing
        {
            get { return showing; }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            button.onClick.AddListener(() =>
            {
                if (!showing)
                {
                    return;
                }

                Hide();
                if (Accepted != null)
                {
                    Accepted();
                }
            });
            gameObject.SetActive(false);
        }

        public void Show(int reward, float seconds)
        {
            amountText.text = "±" + reward;
            total = Mathf.Max(0.1f, seconds);
            remaining = total;
            showing = true;
            Paused = false;
            appearTime = 0f;
            if (ring != null)
            {
                ring.fillAmount = 1f;
            }

            gameObject.SetActive(true);
            AudioManager.Play(Sfx.CardLift);
        }

        public void Hide()
        {
            showing = false;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!showing)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;

            // Aparece con un pequeño rebote y luego late despacio.
            appearTime += dt;
            float pop = appearTime < 0.25f ? UiFx.EaseOutBack(appearTime / 0.25f) : 1f;
            transform.localScale = new Vector3(pop, pop, 1f);
            if (body != null)
            {
                float pulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 5f);
                body.localScale = new Vector3(pulse, pulse, 1f);
            }

            if (Paused)
            {
                return;
            }

            remaining -= dt;
            if (ring != null)
            {
                ring.fillAmount = Mathf.Clamp01(remaining / total);
            }

            if (remaining <= 0f)
            {
                Hide();
                if (Expired != null)
                {
                    Expired();
                }
            }
        }
    }
}
