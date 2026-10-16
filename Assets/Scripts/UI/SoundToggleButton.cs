using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>Botón con icono de altavoz: silencia o activa todo el sonido de un toque.</summary>
    [RequireComponent(typeof(Button))]
    public class SoundToggleButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.ToggleAll();
                Refresh();
            });
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (icon == null)
            {
                return;
            }

            bool anySound = AudioManager.MusicEnabled || AudioManager.SfxEnabled;
            Sprite sprite = anySound ? soundOnSprite : soundOffSprite;
            if (sprite != null)
            {
                icon.sprite = sprite;
            }
        }
    }
}
