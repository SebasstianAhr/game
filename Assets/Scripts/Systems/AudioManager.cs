using System.Collections;
using UnityEngine;

namespace OnePercent
{
    /// <summary>Efectos de sonido del juego. El orden debe coincidir con el array sfxClips del AudioManager.</summary>
    public enum Sfx
    {
        Tap = 0,
        CardLift = 1,
        CardFlip = 2,
        Win = 3,
        BigWin = 4,
        Lose = 5,
        Save = 6,
        Achievement = 7,
        ShieldAlert = 8,
        ShieldStart = 9,
        ShieldBlock = 10,
        TimerTick = 11,
        Victory = 12,
        Bankrupt = 13
    }

    public enum Music
    {
        None,
        Table,
        Shield
    }

    /// <summary>
    /// Reproduce la música (con fundido al cambiar de pista) y los efectos de sonido.
    /// Recuerda entre partidas si la música y los efectos están activados (PlayerPrefs).
    /// Las demás pantallas lo usan con llamadas estáticas: AudioManager.Play(Sfx.Win).
    /// Si no hay AudioManager en la escena, esas llamadas simplemente no hacen nada.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Nombres de archivo (sin extensión) de cada efecto, en el mismo orden que el enum Sfx.</summary>
        public static readonly string[] SfxFileNames =
        {
            "tap", "card_lift", "card_flip", "win", "big_win", "lose", "save", "achievement",
            "shield_alert", "shield_start", "shield_block", "timer_tick", "victory", "bankrupt"
        };

        [Header("Clips")]
        [SerializeField] private AudioClip[] sfxClips;
        [SerializeField] private AudioClip tableMusic;
        [SerializeField] private AudioClip shieldMusic;

        [Header("Volumen")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private float musicFadeTime = 0.8f;

        private const string MusicKey = "audio_music_on";
        private const string SfxKey = "audio_sfx_on";

        private static AudioManager instance;

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private AudioSource sfxSource;
        private Music currentMusic = Music.None;
        private bool musicOn = true;
        private bool sfxOn = true;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            instance = this;
            musicA = CreateSource(true);
            musicB = CreateSource(true);
            sfxSource = CreateSource(false);
            activeMusic = musicA;
            musicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            sfxOn = PlayerPrefs.GetInt(SfxKey, 1) == 1;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = 0f;
            return source;
        }

        // ------------------------------------------------------------------ API estática

        public static bool MusicEnabled
        {
            get { return instance != null ? instance.musicOn : PlayerPrefs.GetInt(MusicKey, 1) == 1; }
        }

        public static bool SfxEnabled
        {
            get { return instance != null ? instance.sfxOn : PlayerPrefs.GetInt(SfxKey, 1) == 1; }
        }

        /// <summary>Reproduce un efecto. volumeScale permite hacerlo más suave (0 a 1).</summary>
        public static void Play(Sfx sound, float volumeScale = 1f)
        {
            if (instance == null || !instance.sfxOn || instance.sfxClips == null)
            {
                return;
            }

            int index = (int)sound;
            if (index < 0 || index >= instance.sfxClips.Length || instance.sfxClips[index] == null)
            {
                return;
            }

            instance.sfxSource.PlayOneShot(instance.sfxClips[index], instance.sfxVolume * volumeScale);
        }

        /// <summary>Cambia la música de fondo con un fundido. Music.None la apaga suavemente.</summary>
        public static void PlayMusic(Music music)
        {
            if (instance == null)
            {
                return;
            }

            instance.SwitchMusic(music);
        }

        public static void SetMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (instance == null)
            {
                return;
            }

            instance.musicOn = enabled;
            if (instance.fadeRoutine == null && instance.activeMusic != null)
            {
                instance.activeMusic.volume = instance.TargetMusicVolume;
            }
        }

        public static void SetSfxEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(SfxKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (instance != null)
            {
                instance.sfxOn = enabled;
            }
        }

        /// <summary>Si algo suena, silencia todo; si todo está en silencio, activa todo.</summary>
        public static void ToggleAll()
        {
            bool turnOn = !(MusicEnabled || SfxEnabled);
            SetMusicEnabled(turnOn);
            SetSfxEnabled(turnOn);
        }

        // ------------------------------------------------------------------ Música

        private float TargetMusicVolume
        {
            get { return musicOn ? musicVolume : 0f; }
        }

        private void SwitchMusic(Music music)
        {
            if (music == currentMusic)
            {
                return;
            }

            currentMusic = music;
            AudioClip clip = music == Music.Table ? tableMusic : music == Music.Shield ? shieldMusic : null;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(Crossfade(clip));
        }

        private IEnumerator Crossfade(AudioClip clip)
        {
            AudioSource from = activeMusic;
            AudioSource to = activeMusic == musicA ? musicB : musicA;
            float fromStart = from.volume;

            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }

            float t = 0f;
            while (t < musicFadeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / musicFadeTime);
                from.volume = fromStart * (1f - k);
                if (clip != null)
                {
                    to.volume = TargetMusicVolume * k;
                }

                yield return null;
            }

            from.Stop();
            from.volume = 0f;
            if (clip != null)
            {
                to.volume = TargetMusicVolume;
                activeMusic = to;
            }

            fadeRoutine = null;
        }
    }
}
