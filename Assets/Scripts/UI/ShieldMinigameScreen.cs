using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercent
{
    /// <summary>
    /// Pantalla 2: minijuego "El Escudo".
    /// Caen proyectiles por 3 carriles. Pulsa el botón BLOQUEAR de un carril cuando el proyectil
    /// esté dentro de la zona verde (parte baja del carril). Cada bloqueo da monedas.
    /// </summary>
    public class ShieldMinigameScreen : MonoBehaviour
    {
        [Header("Textos")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text infoText;

        [Header("Panel de introducción y resultado")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;

        [Header("Carriles (3)")]
        [SerializeField] private RectTransform[] laneAreas;
        [SerializeField] private Button[] laneButtons;

        [Header("Aspecto")]
        [Tooltip("Imagen del proyectil (rombo). Si falta, se dibuja un rectángulo de color.")]
        [SerializeField] private Sprite projectileSprite;
        [Tooltip("Destello que sale al bloquear un proyectil.")]
        [SerializeField] private Sprite sparkSprite;

        private enum State
        {
            Idle,
            Intro,
            Playing,
            Result
        }

        private class Projectile
        {
            public RectTransform Rect;
            public int Lane;
            public float Age;
        }

        private readonly List<Projectile> projectiles = new List<Projectile>();
        private RectTransform[] zones;

        private GameConfig config;
        private Action<int> onFinished;
        private State state = State.Idle;
        private bool initialized;

        private float elapsed;
        private float spawnTimer;
        private int blocks;
        private int reward;

        // Efectos visuales (Etapa 3).
        private static readonly Color Gold = new Color(0.949f, 0.812f, 0.478f);     // #F2CF7A
        private static readonly Color Urgent = new Color(0.910f, 0.475f, 0.541f);   // #E8798A
        private RectTransform fxLayer;
        private int lastTickSecond = -1;
        private Color timerBaseColor = Color.white;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            startButton.onClick.AddListener(StartPlaying);
            continueButton.onClick.AddListener(Continue);

            zones = new RectTransform[laneAreas.Length];
            fxLayer = UiFx.CreateFxLayer(transform);
            timerBaseColor = timerText.color;
            for (int i = 0; i < laneButtons.Length; i++)
            {
                int lane = i;
                laneButtons[i].onClick.AddListener(() => TryBlock(lane));
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            UiFx.ClearFxLayer(fxLayer);
            scoreText.transform.localScale = Vector3.one;
            timerText.color = timerBaseColor;
            ClearProjectiles();
            state = State.Idle;
            gameObject.SetActive(false);
        }

        /// <summary>Prepara el minijuego. onFinished recibe las monedas ganadas (0 o más).</summary>
        public void Begin(GameConfig gameConfig, int coinsLeft, Action<int> finishedCallback)
        {
            config = gameConfig;
            onFinished = finishedCallback;
            state = State.Intro;
            elapsed = 0f;
            lastTickSecond = -1;
            spawnTimer = 0f;
            blocks = 0;
            reward = 0;

            ClearProjectiles();
            UpdateZones();
            SetLaneButtonsInteractable(false);

            overlay.SetActive(true);
            startButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(false);

            infoText.text =
                "¡EMERGENCIA!\nTe quedan " + coinsLeft + " monedas. Esta es tu última oportunidad.\n\n" +
                "Caerán proyectiles por 3 carriles.\n" +
                "Pulsa BLOQUEAR en un carril cuando el proyectil esté dentro de la zona del escudo (la parte baja, marcada con una línea dorada).\n" +
                "Cada bloqueo te da " + config.coinsPerBlock + " monedas (máximo " + config.maxMinigameReward + ").\n" +
                "Tienes " + config.minigameDuration.ToString("0", CultureInfo.InvariantCulture) + " segundos.";

            timerText.text = "Tiempo: " + config.minigameDuration.ToString("0.0", CultureInfo.InvariantCulture);
            timerText.color = timerBaseColor;
            UpdateScore();
        }

        private void StartPlaying()
        {
            if (state != State.Intro)
            {
                return;
            }

            state = State.Playing;
            AudioManager.Play(Sfx.ShieldStart);
            overlay.SetActive(false);
            SetLaneButtonsInteractable(true);
        }

        private void Update()
        {
            if (state != State.Playing)
            {
                return;
            }

            float dt = Time.deltaTime;
            elapsed += dt;

            if (elapsed < config.minigameDuration)
            {
                spawnTimer += dt;
                while (spawnTimer >= config.projectileSpawnInterval)
                {
                    spawnTimer -= config.projectileSpawnInterval;
                    SpawnProjectile();
                }
            }

            UpdateProjectiles(dt);

            float remaining = Mathf.Max(0f, config.minigameDuration - elapsed);
            timerText.text = "Tiempo: " + remaining.ToString("0.0", CultureInfo.InvariantCulture);
            // Los últimos 3 segundos el tiempo se pone en rojo para meter prisa.
            timerText.color = remaining <= 3f ? Urgent : timerBaseColor;
            int wholeSecond = Mathf.CeilToInt(remaining);
            if (remaining > 0f && remaining <= 3f && wholeSecond != lastTickSecond)
            {
                lastTickSecond = wholeSecond;
                AudioManager.Play(Sfx.TimerTick);
            }

            if (elapsed >= config.minigameDuration)
            {
                Finish();
            }
        }

        private void SpawnProjectile()
        {
            int lane = UnityEngine.Random.Range(0, laneAreas.Length);

            GameObject go = new GameObject("Projectile", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(laneAreas[lane], false);

            Image image = go.GetComponent<Image>();
            if (projectileSprite != null)
            {
                image.sprite = projectileSprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.761f, 0.220f, 0.310f); // carmín #C2384F
            }
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Projectile projectile = new Projectile();
            projectile.Rect = rect;
            projectile.Lane = lane;
            projectile.Age = 0f;
            projectiles.Add(projectile);

            PositionProjectile(projectile);
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = projectiles[i];
                projectile.Age += dt;

                if (projectile.Age >= config.projectileFallTime)
                {
                    Destroy(projectile.Rect.gameObject);
                    projectiles.RemoveAt(i);
                    continue;
                }

                PositionProjectile(projectile);
            }
        }

        /// <summary>y = 1 arriba del carril, y = 0 abajo. El proyectil mide un 10 % de la altura del carril.</summary>
        private float CenterY(Projectile projectile)
        {
            return 1f - projectile.Age / config.projectileFallTime;
        }

        private void PositionProjectile(Projectile projectile)
        {
            float y = CenterY(projectile);
            projectile.Rect.anchorMin = new Vector2(0.15f, Mathf.Clamp01(y - 0.05f));
            projectile.Rect.anchorMax = new Vector2(0.85f, Mathf.Clamp01(y + 0.05f));
        }

        private void TryBlock(int lane)
        {
            if (state != State.Playing)
            {
                return;
            }

            Projectile best = null;
            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile candidate = projectiles[i];
                if (candidate.Lane != lane || CenterY(candidate) > config.blockZoneTop)
                {
                    continue;
                }

                if (best == null || candidate.Age > best.Age)
                {
                    best = candidate;
                }
            }

            if (best == null)
            {
                return;
            }

            Vector3 hitPosition = best.Rect.position;
            int before = Mathf.Min(blocks * config.coinsPerBlock, config.maxMinigameReward);
            Destroy(best.Rect.gameObject);
            projectiles.Remove(best);
            blocks++;
            UpdateScore();

            // Respuesta visual al bloquear: destellos dorados, "+3" que sube y el marcador que late.
            int gained = Mathf.Min(blocks * config.coinsPerBlock, config.maxMinigameReward) - before;
            UiFx.SparkBurst(this, fxLayer, hitPosition, sparkSprite, Gold, 10, 150f, 34f);
            UiFx.FloatingText(this, fxLayer, hitPosition, gained > 0 ? "+" + gained : "MÁX", scoreText.font, 48, Gold);
            StartCoroutine(UiFx.Punch(scoreText.transform, 0.15f, 0.25f));
            AudioManager.Play(Sfx.ShieldBlock);
        }

        private void Finish()
        {
            state = State.Result;
            ClearProjectiles();
            SetLaneButtonsInteractable(false);

            reward = Mathf.Min(blocks * config.coinsPerBlock, config.maxMinigameReward);

            overlay.SetActive(true);
            startButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(true);

            if (reward > 0)
            {
                infoText.text = "¡Tiempo!\nBloqueaste " + blocks + " proyectiles.\nRecuperas " + reward + " monedas.";
            }
            else
            {
                infoText.text = "¡Tiempo!\nNo bloqueaste ningún proyectil.\nNo recuperas monedas.";
            }

            timerText.text = "Tiempo: 0.0";
        }

        private void Continue()
        {
            if (state != State.Result)
            {
                return;
            }

            state = State.Idle;
            Action<int> callback = onFinished;
            onFinished = null;
            if (callback != null)
            {
                callback(reward);
            }
        }

        private void UpdateScore()
        {
            int pending = Mathf.Min(blocks * config.coinsPerBlock, config.maxMinigameReward);
            scoreText.text = "Bloqueos: " + blocks + " (+" + pending + ")";
        }

        private void UpdateZones()
        {
            for (int i = 0; i < laneAreas.Length; i++)
            {
                if (zones[i] == null)
                {
                    GameObject go = new GameObject("BlockZone", typeof(RectTransform), typeof(Image));
                    go.layer = gameObject.layer;
                    go.transform.SetParent(laneAreas[i], false);
                    go.transform.SetAsFirstSibling();

                    Image image = go.GetComponent<Image>();
                    image.color = new Color(0.118f, 0.173f, 0.400f); // zona del escudo: azul #1E2C66
                    image.raycastTarget = false;

                    zones[i] = go.GetComponent<RectTransform>();
                    zones[i].offsetMin = Vector2.zero;
                    zones[i].offsetMax = Vector2.zero;

                    // Línea dorada que marca el borde superior de la zona.
                    GameObject line = new GameObject("BlockZoneEdge", typeof(RectTransform), typeof(Image));
                    line.layer = gameObject.layer;
                    line.transform.SetParent(zones[i], false);
                    Image lineImage = line.GetComponent<Image>();
                    lineImage.color = new Color(0.949f, 0.812f, 0.478f); // oro #F2CF7A
                    lineImage.raycastTarget = false;
                    RectTransform lineRect = line.GetComponent<RectTransform>();
                    lineRect.anchorMin = new Vector2(0f, 1f);
                    lineRect.anchorMax = new Vector2(1f, 1f);
                    lineRect.pivot = new Vector2(0.5f, 1f);
                    lineRect.sizeDelta = new Vector2(0f, 6f);
                    lineRect.anchoredPosition = Vector2.zero;
                }

                zones[i].anchorMin = new Vector2(0f, 0f);
                zones[i].anchorMax = new Vector2(1f, config.blockZoneTop);
                // Un poco hacia dentro para no tapar el borde dorado redondeado del carril.
                zones[i].offsetMin = new Vector2(4f, 4f);
                zones[i].offsetMax = new Vector2(-4f, 0f);
            }
        }

        private void SetLaneButtonsInteractable(bool value)
        {
            for (int i = 0; i < laneButtons.Length; i++)
            {
                laneButtons[i].interactable = value;
            }
        }

        private void ClearProjectiles()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                if (projectiles[i].Rect != null)
                {
                    Destroy(projectiles[i].Rect.gameObject);
                }
            }

            projectiles.Clear();
        }
    }
}
