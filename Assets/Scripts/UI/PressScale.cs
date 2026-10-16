using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Hace que un botón "se hunda" un poco mientras el dedo lo mantiene pulsado.
    /// No hace nada si el botón está desactivado.
    /// </summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float speed = 2.5f;

        private Selectable selectable;
        private bool pressed;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable())
            {
                return;
            }

            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressed = false;
        }

        private void OnDisable()
        {
            pressed = false;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float target = pressed ? pressedScale : 1f;
            float current = transform.localScale.x;
            if (Mathf.Approximately(current, target))
            {
                return;
            }

            float next = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * speed);
            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
