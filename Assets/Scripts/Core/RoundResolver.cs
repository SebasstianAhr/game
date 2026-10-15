using UnityEngine;

namespace OnePercent
{
    /// <summary>
    /// Lógica pura de probabilidades. Aquí viven las DOS reglas ocultas del MVP:
    /// 1) el símbolo del fondo cambia la probabilidad real;
    /// 2) las rachas de la apuesta moderada la corrigen (tras N derrotas sube, tras N victorias baja).
    /// </summary>
    public static class RoundResolver
    {
        public static float GetRealProbability(
            GameConfig config,
            BetAction action,
            BackgroundSymbol symbol,
            int winStreak,
            int lossStreak,
            out bool streakApplied)
        {
            streakApplied = false;

            if (action == BetAction.Save)
            {
                return 1f;
            }

            float probability = config.GetSymbolProbability(action, symbol);

            if (action == BetAction.Moderate)
            {
                if (lossStreak >= config.streakLength)
                {
                    probability += config.streakModerateBonus;
                    streakApplied = true;
                }
                else if (winStreak >= config.streakLength)
                {
                    probability -= config.streakModerateBonus;
                    streakApplied = true;
                }
            }

            return Mathf.Clamp01(probability);
        }

        /// <summary>true con probabilidad "probability". rng.NextDouble() está en [0, 1).</summary>
        public static bool Roll(System.Random rng, float probability)
        {
            return rng.NextDouble() < probability;
        }
    }
}
