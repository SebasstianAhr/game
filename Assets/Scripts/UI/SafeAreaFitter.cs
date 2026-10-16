using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Ajusta este RectTransform a la "zona segura" de la pantalla (Screen.safeArea):
    /// la parte que no tapan la muesca, la cámara frontal ni la barra de gestos del teléfono.
    /// Todas las pantallas del juego cuelgan de este objeto.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Solo recalcula si algo cambió (por ejemplo, al cambiar de dispositivo en el Simulator).
            if (Screen.safeArea != lastSafeArea || Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
