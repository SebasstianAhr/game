using System.Collections;
using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Cada vez que una pantalla se muestra, aparece con un fundido corto y un leve acercamiento.
    /// No cambia ninguna lógica: solo actúa al activarse el objeto.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] private float duration = 0.22f;

        private CanvasGroup group;

        private void Awake()
        {
            EnsureGroup();
        }

        private void OnEnable()
        {
            EnsureGroup();
            StartCoroutine(FadeIn());
        }

        private void OnDisable()
        {
            if (group != null)
            {
                group.alpha = 1f;
            }

            transform.localScale = Vector3.one;
        }

        private void EnsureGroup()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private IEnumerator FadeIn()
        {
            float t = 0f;
            group.alpha = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / duration));
                group.alpha = k;
                float s = 0.985f + 0.015f * k;
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            group.alpha = 1f;
            transform.localScale = Vector3.one;
        }
    }
}
