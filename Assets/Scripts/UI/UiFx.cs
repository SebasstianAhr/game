using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Utilidades de animación de interfaz hechas con corrutinas (sin librerías externas).
    /// Usan tiempo real (unscaled) para que funcionen aunque el juego se pause.
    /// </summary>
    public static class UiFx
    {
        // ------------------------------------------------------------------ Curvas de suavizado

        public static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Se pasa un poco del final y vuelve: da sensación de "rebote".</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float EaseInOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }

        // ------------------------------------------------------------------ Animaciones básicas

        /// <summary>Escala de "from" a "to" en "duration" segundos.</summary>
        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration, System.Func<float, float> ease)
        {
            float t = 0f;
            while (t < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = ease(Mathf.Clamp01(t / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = to;
            }
        }

        /// <summary>Crece un momento y vuelve a su tamaño: para resaltar un número o un botón.</summary>
        public static IEnumerator Punch(Transform target, float amount, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float s = 1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        /// <summary>Temblor horizontal que se apaga: para señalar una pérdida.</summary>
        public static IEnumerator Shake(RectTransform target, Vector2 basePosition, float strength, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float offset = Mathf.Sin(k * Mathf.PI * 7f) * strength * (1f - k);
                target.anchoredPosition = basePosition + new Vector2(offset, 0f);
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = basePosition;
            }
        }

        /// <summary>Cambia la transparencia de un gráfico (texto o imagen).</summary>
        public static IEnumerator Fade(Graphic graphic, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                if (graphic == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = EaseOutCubic(Mathf.Clamp01(t / duration));
                SetAlpha(graphic, Mathf.Lerp(from, to, k));
                yield return null;
            }

            if (graphic != null)
            {
                SetAlpha(graphic, to);
            }
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        // ------------------------------------------------------------------ Capa de efectos y destellos

        /// <summary>Crea una capa invisible que cubre la pantalla, encima de todo, para dibujar efectos.</summary>
        public static RectTransform CreateFxLayer(Transform screen)
        {
            GameObject go = new GameObject("FxLayer", typeof(RectTransform));
            go.layer = screen.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(screen, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            return rect;
        }

        /// <summary>Borra los efectos que quedaran a medias (por ejemplo, al cambiar de pantalla).</summary>
        public static void ClearFxLayer(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            for (int i = layer.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(layer.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Lanza "count" destellos desde una posición del mundo hacia afuera, que giran y se desvanecen.
        /// "distance" y "size" están en unidades del lienzo (referencia 1080x1920).
        /// </summary>
        public static void SparkBurst(MonoBehaviour host, RectTransform layer, Vector3 worldPosition, Sprite sprite,
            Color color, int count, float distance, float size)
        {
            if (host == null || layer == null || !host.isActiveAndEnabled)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
                go.layer = layer.gameObject.layer;
                RectTransform rect = (RectTransform)go.transform;
                rect.SetParent(layer, false);
                rect.sizeDelta = new Vector2(size, size);
                rect.position = worldPosition;

                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;

                float angle = (360f / count) * i + Random.Range(-14f, 14f);
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 offset = direction * distance * Random.Range(0.65f, 1.1f);
                host.StartCoroutine(FlySpark(rect, image, offset, Random.Range(0.45f, 0.7f)));
            }
        }

        private static IEnumerator FlySpark(RectTransform rect, Image image, Vector2 offset, float duration)
        {
            Vector2 start = rect.anchoredPosition;
            Color color = image.color;
            float t = 0f;
            while (t < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                rect.anchoredPosition = start + offset * EaseOutCubic(k);
                float s = 1f - 0.6f * k;
                rect.localScale = new Vector3(s, s, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 120f * k);
                image.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
                yield return null;
            }

            if (rect != null)
            {
                Object.Destroy(rect.gameObject);
            }
        }

        /// <summary>Texto que sube y se desvanece (por ejemplo "+3" al bloquear en El Escudo).</summary>
        public static void FloatingText(MonoBehaviour host, RectTransform layer, Vector3 worldPosition, string content,
            Font font, int size, Color color)
        {
            if (host == null || layer == null || !host.isActiveAndEnabled)
            {
                return;
            }

            GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(Text));
            go.layer = layer.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(layer, false);
            rect.sizeDelta = new Vector2(300f, 80f);
            rect.position = worldPosition;

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            host.StartCoroutine(FloatUp(rect, text, 0.7f));
        }

        private static IEnumerator FloatUp(RectTransform rect, Text text, float duration)
        {
            Vector2 start = rect.anchoredPosition;
            Color color = text.color;
            float t = 0f;
            while (t < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                rect.anchoredPosition = start + new Vector2(0f, 110f * EaseOutCubic(k));
                text.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k));
                yield return null;
            }

            if (rect != null)
            {
                Object.Destroy(rect.gameObject);
            }
        }
    }
}
