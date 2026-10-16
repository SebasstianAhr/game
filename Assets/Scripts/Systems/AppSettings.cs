using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Ajustes generales de la aplicación al arrancar.
    /// En móvil, Unity dibuja a 30 imágenes por segundo por defecto; a 60 las animaciones se ven fluidas.
    /// </summary>
    public class AppSettings : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
