using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Pantalla de la pregunta de matemáticas: enunciado, 4 opciones y tiempo límite.
    /// Al responder (o al acabarse el tiempo) muestra si acertó, cuál era la respuesta y cuánto gana o pierde.
    /// El botón CONTINUAR cierra y avisa al GameManager del resultado.
    /// </summary>
    public class QuestionScreen : MonoBehaviour
    {
        [Header("Textos")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text stakeText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text questionText;
        [SerializeField] private Text feedbackText;

        [Header("Barra de tiempo (se acorta de derecha a izquierda)")]
        [SerializeField] private RectTransform timerFill;

        [Header("Opciones")]
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Text[] optionLabels;
        [SerializeField] private Button continueButton;

        private static readonly Color Gold = new Color(0.949f, 0.812f, 0.478f);       // #F2CF7A
        private static readonly Color Ivory = new Color(0.929f, 0.902f, 0.827f);      // #EDE6D3
        private static readonly Color Urgent = new Color(0.910f, 0.475f, 0.541f);     // #E8798A
        private static readonly Color RightColor = new Color(0.141f, 0.337f, 0.298f); // jade #24564C
        private static readonly Color WrongColor = new Color(0.478f, 0.133f, 0.204f); // carmín #7A2234

        private enum State
        {
            Idle,
            Answering,
            Result
        }

        private State state = State.Idle;
        private MathQuestion question;
        private Action<bool> onFinished;
        private float total;
        private float remaining;
        private int lastTickSecond;
        private bool answeredRight;
        private bool initialized;
        private Color[] baseColors;

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
            baseColors = new Color[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                baseColors[i] = optionButtons[i].targetGraphic.color;
                optionButtons[i].onClick.AddListener(() => Choose(index));
            }

            continueButton.onClick.AddListener(Continue);
        }

        /// <summary>Abre la pregunta. finished recibe true si acertó.</summary>
        public void Open(MathQuestion newQuestion, float seconds, Action<bool> finished)
        {
            question = newQuestion;
            onFinished = finished;
            total = Mathf.Max(1f, seconds);
            remaining = total;
            lastTickSecond = -1;
            state = State.Answering;

            titleText.text = question.Mandatory ? "PREGUNTA OBLIGATORIA" : "PREGUNTA MISTERIOSA";
            titleText.color = question.Mandatory ? Urgent : Gold;
            stakeText.text = "Aciertas: +" + question.Reward + " monedas      Fallas: -" + question.Reward + " monedas";
            questionText.text = question.Text;
            feedbackText.text = string.Empty;
            continueButton.gameObject.SetActive(false);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool used = i < question.Options.Length;
                optionButtons[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                optionLabels[i].text = question.Options[i].ToString(CultureInfo.InvariantCulture);
                optionButtons[i].targetGraphic.color = baseColors[i];
                optionButtons[i].interactable = true;
            }

            UpdateTimer();
            gameObject.SetActive(true);
        }

        /// <summary>Cierra sin avisar (por ejemplo, al volver al menú).</summary>
        public void ForceClose()
        {
            state = State.Idle;
            onFinished = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (state != State.Answering)
            {
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            UpdateTimer();

            int whole = Mathf.CeilToInt(remaining);
            if (remaining > 0f && remaining <= 3f && whole != lastTickSecond)
            {
                lastTickSecond = whole;
                AudioManager.Play(Sfx.TimerTick);
            }

            if (remaining <= 0f)
            {
                Resolve(-1);
            }
        }

        private void UpdateTimer()
        {
            float shown = Mathf.Max(0f, remaining);
            timerText.text = shown.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            timerText.color = shown <= 3f ? Urgent : Ivory;
            if (timerFill != null)
            {
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(shown / total), timerFill.anchorMax.y);
            }
        }

        private void Choose(int index)
        {
            if (state == State.Answering)
            {
                Resolve(index);
            }
        }

        /// <param name="chosen">Opción elegida, o -1 si se acabó el tiempo.</param>
        private void Resolve(int chosen)
        {
            state = State.Result;
            answeredRight = chosen == question.CorrectIndex;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
                if (i != question.CorrectIndex && i != chosen)
                {
                    // Las opciones que no importan se apagan un poco.
                    Color c = baseColors[i] * 0.55f;
                    c.a = 1f;
                    optionButtons[i].targetGraphic.color = c;
                }
            }

            // La respuesta correcta se marca en verde jade; la elegida por error, en carmín.
            optionButtons[question.CorrectIndex].targetGraphic.color = RightColor;
            if (chosen >= 0 && !answeredRight)
            {
                optionButtons[chosen].targetGraphic.color = WrongColor;
            }

            if (answeredRight)
            {
                feedbackText.text = "<color=#F2CF7A>¡Correcto! +" + question.Reward + " monedas</color>";
                AudioManager.Play(Sfx.Win);
            }
            else
            {
                string reason = chosen < 0 ? "Se acabó el tiempo." : "Incorrecto.";
                feedbackText.text = "<color=#E8798A>" + reason + " La respuesta era " + question.CorrectAnswer +
                                    ". -" + question.Reward + " monedas</color>";
                AudioManager.Play(Sfx.Lose);
            }

            continueButton.gameObject.SetActive(true);
        }

        private void Continue()
        {
            if (state != State.Result)
            {
                return;
            }

            state = State.Idle;
            gameObject.SetActive(false);
            Action<bool> callback = onFinished;
            onFinished = null;
            if (callback != null)
            {
                callback(answeredRight);
            }
        }
    }
}
