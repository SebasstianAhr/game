using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Botón que activa o desactiva la música o los efectos y muestra su estado ("Música: SÍ").</summary>
    [RequireComponent(typeof(Button))]
    public class AudioSettingToggle : MonoBehaviour
    {
        public enum Setting
        {
            Music,
            Effects
        }

        [SerializeField] private Setting setting = Setting.Music;
        [SerializeField] private Text label;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Toggle()
        {
            if (setting == Setting.Music)
            {
                AudioManager.SetMusicEnabled(!AudioManager.MusicEnabled);
            }
            else
            {
                AudioManager.SetSfxEnabled(!AudioManager.SfxEnabled);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (label == null)
            {
                return;
            }

            bool on = setting == Setting.Music ? AudioManager.MusicEnabled : AudioManager.SfxEnabled;
            string name = setting == Setting.Music ? "Música" : "Efectos";
            label.text = name + ": " + (on ? "SÍ" : "NO");
        }
    }
}
