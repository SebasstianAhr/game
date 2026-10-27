using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Todos los números del juego en un solo lugar. Se puede editar desde el Inspector
    /// (Assets/Settings/GameConfig.asset) sin tocar código.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "1% de Probabilidad/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Partida (GDD 5.1 y 5.3)")]
        public int startingCoins = 100;
        public int goalCoins = 10000;
        [Tooltip("Coste de cada ronda. Se cobra del capital que NO se apuesta.")]
        public int roundCost = 1;

        [Header("Guardar")]
        public int saveReturn = 2;

        [Header("Apuesta moderada")]
        public int moderateStake = 20;
        public int moderatePayoutMultiplier = 2;
        [Range(0f, 1f)] public float moderateDisplayedProbability = 0.5f;

        [Header("Todo o nada")]
        public int allInStake = 100;
        public int allInPayoutMultiplier = 100;
        [Range(0f, 1f)] public float allInDisplayedProbability = 0.01f;

        [Header("Regla oculta 1: símbolo del fondo (probabilidad REAL)")]
        [Range(0f, 1f)] public float sunModerateProbability = 0.65f;
        [Range(0f, 1f)] public float moonModerateProbability = 0.30f;
        [Range(0f, 1f)] public float eyeModerateProbability = 0.50f;
        [Range(0f, 1f)] public float sunAllInProbability = 0.01f;
        [Range(0f, 1f)] public float moonAllInProbability = 0.01f;
        [Range(0f, 1f)] public float eyeAllInProbability = 0.05f;

        [Header("Frecuencia con la que aparece cada símbolo (pesos relativos)")]
        public float sunWeight = 0.4f;
        public float moonWeight = 0.4f;
        public float eyeWeight = 0.2f;

        [Header("Regla oculta 2: rachas (solo apuesta moderada)")]
        [Tooltip("Cuántas victorias o derrotas seguidas activan la regla.")]
        public int streakLength = 3;
        [Tooltip("Tras N derrotas seguidas se suma; tras N victorias seguidas se resta.")]
        [Range(0f, 0.5f)] public float streakModerateBonus = 0.15f;

        [Header("Minijuego El Escudo")]
        [Tooltip("Se ofrece cuando el capital es igual o menor que este valor (una vez por partida).")]
        public int minigameThresholdCoins = 10;
        public float minigameDuration = 10f;
        public float projectileFallTime = 2f;
        public float projectileSpawnInterval = 0.8f;
        public int coinsPerBlock = 3;
        public int maxMinigameReward = 30;
        [Tooltip("Parte baja del carril (0 a 1) donde se puede bloquear un proyectil.")]
        [Range(0.1f, 0.6f)] public float blockZoneTop = 0.30f;

        [Header("Preguntas de matemáticas")]
        [Tooltip("La pregunta opcional aparece cada cierto número de rondas, al azar entre este mínimo y el máximo.")]
        public int optionalQuestionMinRounds = 4;
        public int optionalQuestionMaxRounds = 7;
        [Tooltip("Segundos que la notificación de la pregunta opcional queda visible para aceptarla.")]
        public float questionOfferSeconds = 8f;
        [Tooltip("Cada cuántas rondas aparece una pregunta obligatoria (0 = nunca).")]
        public int mandatoryQuestionEvery = 10;
        [Tooltip("Segundos para responder una vez abierta la pregunta.")]
        public float questionAnswerSeconds = 15f;
        public int easyQuestionReward = 10;
        public int mediumQuestionReward = 25;
        public int hardQuestionReward = 50;
        [Tooltip("Las preguntas obligatorias multiplican el premio (y la pérdida) por este valor.")]
        public int mandatoryRewardMultiplier = 2;
        [Tooltip("Desde esta ronda las preguntas son de dificultad media (multiplicar y dividir).")]
        public int mediumQuestionsFromRound = 10;
        [Tooltip("Desde esta ronda las preguntas son difíciles (porcentajes y operaciones de dos pasos).")]
        public int hardQuestionsFromRound = 25;

        [Header("Estadísticas")]
        [Tooltip("Apuestas registradas necesarias para marcar un patrón como descubierto.")]
        public int patternMinSamples = 8;

        [Header("Depuración")]
        [Tooltip("Si está activo, todas las partidas usan la misma semilla y dan los mismos resultados.")]
        public bool useFixedSeed = false;
        public int fixedSeed = 12345;

        public int GetStake(BetAction action)
        {
            switch (action)
            {
                case BetAction.Moderate: return moderateStake;
                case BetAction.AllIn: return allInStake;
                default: return 0;
            }
        }

        public int GetPayoutMultiplier(BetAction action)
        {
            switch (action)
            {
                case BetAction.Moderate: return moderatePayoutMultiplier;
                case BetAction.AllIn: return allInPayoutMultiplier;
                default: return 0;
            }
        }

        public float GetDisplayedProbability(BetAction action)
        {
            switch (action)
            {
                case BetAction.Moderate: return moderateDisplayedProbability;
                case BetAction.AllIn: return allInDisplayedProbability;
                default: return 1f;
            }
        }

        /// <summary>Probabilidad real base según el símbolo (sin tener en cuenta las rachas).</summary>
        public float GetSymbolProbability(BetAction action, BackgroundSymbol symbol)
        {
            if (action == BetAction.Moderate)
            {
                switch (symbol)
                {
                    case BackgroundSymbol.Sun: return sunModerateProbability;
                    case BackgroundSymbol.Moon: return moonModerateProbability;
                    default: return eyeModerateProbability;
                }
            }

            if (action == BetAction.AllIn)
            {
                switch (symbol)
                {
                    case BackgroundSymbol.Sun: return sunAllInProbability;
                    case BackgroundSymbol.Moon: return moonAllInProbability;
                    default: return eyeAllInProbability;
                }
            }

            return 1f;
        }

        public float GetSymbolWeight(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return sunWeight;
                case BackgroundSymbol.Moon: return moonWeight;
                default: return eyeWeight;
            }
        }

        private void OnValidate()
        {
            startingCoins = Mathf.Max(1, startingCoins);
            goalCoins = Mathf.Max(startingCoins + 1, goalCoins);
            roundCost = Mathf.Max(0, roundCost);
            saveReturn = Mathf.Max(0, saveReturn);
            moderateStake = Mathf.Max(1, moderateStake);
            allInStake = Mathf.Max(1, allInStake);
            moderatePayoutMultiplier = Mathf.Max(1, moderatePayoutMultiplier);
            allInPayoutMultiplier = Mathf.Max(1, allInPayoutMultiplier);
            streakLength = Mathf.Max(1, streakLength);
            minigameThresholdCoins = Mathf.Max(0, minigameThresholdCoins);
            minigameDuration = Mathf.Max(3f, minigameDuration);
            projectileFallTime = Mathf.Max(0.5f, projectileFallTime);
            projectileSpawnInterval = Mathf.Max(0.2f, projectileSpawnInterval);
            coinsPerBlock = Mathf.Max(0, coinsPerBlock);
            maxMinigameReward = Mathf.Max(0, maxMinigameReward);
            patternMinSamples = Mathf.Max(1, patternMinSamples);
            optionalQuestionMinRounds = Mathf.Max(1, optionalQuestionMinRounds);
            optionalQuestionMaxRounds = Mathf.Max(optionalQuestionMinRounds, optionalQuestionMaxRounds);
            questionOfferSeconds = Mathf.Max(2f, questionOfferSeconds);
            mandatoryQuestionEvery = Mathf.Max(0, mandatoryQuestionEvery);
            questionAnswerSeconds = Mathf.Max(3f, questionAnswerSeconds);
            easyQuestionReward = Mathf.Max(0, easyQuestionReward);
            mediumQuestionReward = Mathf.Max(0, mediumQuestionReward);
            hardQuestionReward = Mathf.Max(0, hardQuestionReward);
            mandatoryRewardMultiplier = Mathf.Max(1, mandatoryRewardMultiplier);
            mediumQuestionsFromRound = Mathf.Max(1, mediumQuestionsFromRound);
            hardQuestionsFromRound = Mathf.Max(mediumQuestionsFromRound, hardQuestionsFromRound);
            sunWeight = Mathf.Max(0f, sunWeight);
            moonWeight = Mathf.Max(0f, moonWeight);
            eyeWeight = Mathf.Max(0f, eyeWeight);
            if (sunWeight + moonWeight + eyeWeight <= 0f)
            {
                sunWeight = 1f;
            }
        }
    }
}
