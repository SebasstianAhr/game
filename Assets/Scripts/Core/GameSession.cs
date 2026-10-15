using System;
using System.Collections.Generic;

namespace OnePercent
{
    /// <summary>
    /// Estado y reglas de UNA partida. No depende de la interfaz ni de MonoBehaviour,
    /// así que se puede probar de forma aislada.
    /// </summary>
    public class GameSession
    {
        private readonly GameConfig config;
        private readonly System.Random rng;
        private readonly List<RoundResult> history = new List<RoundResult>();

        public int Coins { get; private set; }
        public int PeakCoins { get; private set; }
        public int Round { get; private set; }
        public BackgroundSymbol CurrentSymbol { get; private set; }
        public int ConsecutiveWins { get; private set; }
        public int ConsecutiveLosses { get; private set; }
        public bool MinigameUsed { get; private set; }
        public GameOutcome Outcome { get; private set; }

        public IReadOnlyList<RoundResult> History
        {
            get { return history; }
        }

        /// <summary>Cuando el capital es bajo y aún no se usó el minijuego, el juego lo ofrece.</summary>
        public bool ShouldOfferMinigame
        {
            get
            {
                return Outcome == GameOutcome.InProgress
                       && !MinigameUsed
                       && Coins <= config.minigameThresholdCoins;
            }
        }

        public GameSession(GameConfig config, int? seed)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.config = config;
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Coins = config.startingCoins;
            PeakCoins = Coins;
            Outcome = GameOutcome.InProgress;
            CurrentSymbol = RollSymbol();
        }

        public bool CanPerform(BetAction action)
        {
            if (Outcome != GameOutcome.InProgress)
            {
                return false;
            }

            if (action == BetAction.Save)
            {
                return Coins >= 1;
            }

            return Coins >= config.GetStake(action);
        }

        /// <summary>Probabilidad real que tendría ahora mismo la acción (se usa en el panel de depuración).</summary>
        public float GetRealProbability(BetAction action)
        {
            bool streakApplied;
            return RoundResolver.GetRealProbability(
                config, action, CurrentSymbol, ConsecutiveWins, ConsecutiveLosses, out streakApplied);
        }

        public RoundResult Play(BetAction action)
        {
            if (Outcome != GameOutcome.InProgress)
            {
                throw new InvalidOperationException("La partida ya terminó.");
            }

            if (!CanPerform(action))
            {
                throw new InvalidOperationException("No hay monedas suficientes para la acción " + action + ".");
            }

            RoundResult result = new RoundResult();
            result.RoundNumber = Round + 1;
            result.Action = action;
            result.Symbol = CurrentSymbol;
            result.CoinsBefore = Coins;
            result.WinStreakBefore = ConsecutiveWins;
            result.LossStreakBefore = ConsecutiveLosses;

            int stake = config.GetStake(action);

            // El coste de ronda solo se cobra del capital que no se apuesta.
            int fee = Math.Min(config.roundCost, Coins - stake);
            if (fee < 0)
            {
                fee = 0;
            }

            result.Stake = stake;
            result.Fee = fee;
            result.DisplayedProbability = config.GetDisplayedProbability(action);

            Coins -= fee + stake;

            if (action == BetAction.Save)
            {
                result.IsBet = false;
                result.Won = false;
                result.RealProbability = 1f;
                result.GrossGain = config.saveReturn;
                Coins += result.GrossGain;
            }
            else
            {
                bool streakApplied;
                float real = RoundResolver.GetRealProbability(
                    config, action, CurrentSymbol, ConsecutiveWins, ConsecutiveLosses, out streakApplied);

                result.IsBet = true;
                result.RealProbability = real;
                result.StreakModifierApplied = streakApplied;
                result.Won = RoundResolver.Roll(rng, real);

                if (result.Won)
                {
                    result.GrossGain = stake * config.GetPayoutMultiplier(action);
                    Coins += result.GrossGain;
                    ConsecutiveWins++;
                    ConsecutiveLosses = 0;
                    result.ExploitedHiddenRule = real > result.DisplayedProbability + 0.0001f;
                }
                else
                {
                    ConsecutiveLosses++;
                    ConsecutiveWins = 0;
                }
            }

            Round++;
            result.CoinsAfter = Coins;
            if (Coins > PeakCoins)
            {
                PeakCoins = Coins;
            }

            history.Add(result);
            CurrentSymbol = RollSymbol();
            EvaluateOutcome();
            return result;
        }

        /// <summary>Aplica la recompensa del minijuego. Solo se puede usar una vez por partida.</summary>
        public void ApplyMinigameReward(int reward)
        {
            if (MinigameUsed)
            {
                throw new InvalidOperationException("El minijuego ya se usó en esta partida.");
            }

            MinigameUsed = true;
            Coins += Math.Max(0, reward);
            if (Coins > PeakCoins)
            {
                PeakCoins = Coins;
            }

            EvaluateOutcome();
        }

        private void EvaluateOutcome()
        {
            if (Coins >= config.goalCoins)
            {
                Outcome = GameOutcome.Victory;
            }
            else if (Coins <= 0 && MinigameUsed)
            {
                Outcome = GameOutcome.Defeat;
            }
        }

        private BackgroundSymbol RollSymbol()
        {
            float total = config.sunWeight + config.moonWeight + config.eyeWeight;
            if (total <= 0f)
            {
                return BackgroundSymbol.Sun;
            }

            double roll = rng.NextDouble() * total;
            if (roll < config.sunWeight)
            {
                return BackgroundSymbol.Sun;
            }

            if (roll < config.sunWeight + config.moonWeight)
            {
                return BackgroundSymbol.Moon;
            }

            return BackgroundSymbol.Eye;
        }
    }
}
