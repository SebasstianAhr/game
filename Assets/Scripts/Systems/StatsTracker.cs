using System.Collections.Generic;

namespace OnePercent
{
    /// <summary>
    /// Actualiza las estadísticas, los patrones observados y los logros.
    /// "Engañar al sistema" = ganar una apuesta mientras la probabilidad real superaba a la mostrada.
    /// </summary>
    public class StatsTracker
    {
        private readonly GameConfig config;

        public SaveData Data { get; private set; }

        public StatsTracker(SaveData data, GameConfig config)
        {
            Data = data ?? new SaveData();
            Data.EnsureValid();
            this.config = config;
        }

        public void ResetAll()
        {
            Data = new SaveData();
        }

        /// <summary>Registra una ronda. Devuelve los logros recién desbloqueados.</summary>
        public List<AchievementInfo> RegisterRound(RoundResult round)
        {
            List<AchievementInfo> unlocked = new List<AchievementInfo>();

            Data.totalCoinsEarned += round.GrossGain;
            if (round.CoinsAfter > Data.bestCoins)
            {
                Data.bestCoins = round.CoinsAfter;
            }

            if (round.ExploitedHiddenRule)
            {
                Data.timesCheatedSystem++;
            }

            if (round.Action == BetAction.Moderate)
            {
                int symbolIndex = (int)round.Symbol;
                Data.symbolModerateBets[symbolIndex]++;
                if (round.Won)
                {
                    Data.symbolModerateWins[symbolIndex]++;
                }

                if (round.WinStreakBefore >= config.streakLength)
                {
                    Data.afterWinStreakBets++;
                    if (round.Won)
                    {
                        Data.afterWinStreakWins++;
                    }
                }

                if (round.LossStreakBefore >= config.streakLength)
                {
                    Data.afterLossStreakBets++;
                    if (round.Won)
                    {
                        Data.afterLossStreakWins++;
                    }
                }
            }

            if (round.Action == BetAction.AllIn && round.Won)
            {
                Unlock(Achievements.AllInWin, unlocked);
            }

            if (round.CoinsAfter >= 1000)
            {
                Unlock(Achievements.Thousand, unlocked);
            }

            if (Data.timesCheatedSystem >= 5)
            {
                Unlock(Achievements.SharpEye, unlocked);
            }

            return unlocked;
        }

        public List<AchievementInfo> RegisterMinigame(int reward, int coinsAfter)
        {
            List<AchievementInfo> unlocked = new List<AchievementInfo>();

            Data.totalCoinsEarned += reward;
            if (coinsAfter > Data.bestCoins)
            {
                Data.bestCoins = coinsAfter;
            }

            if (reward > 0)
            {
                Unlock(Achievements.ShieldSaved, unlocked);
            }

            return unlocked;
        }

        public List<AchievementInfo> RegisterGameEnd(bool victory, int peakCoins)
        {
            List<AchievementInfo> unlocked = new List<AchievementInfo>();

            Data.gamesPlayed++;
            if (victory)
            {
                Data.victories++;
                Unlock(Achievements.FirstVictory, unlocked);
            }
            else
            {
                Data.defeats++;
            }

            if (peakCoins > Data.bestCoins)
            {
                Data.bestCoins = peakCoins;
            }

            return unlocked;
        }

        private void Unlock(string id, List<AchievementInfo> newlyUnlocked)
        {
            if (Data.unlockedAchievements.Contains(id))
            {
                return;
            }

            Data.unlockedAchievements.Add(id);
            AchievementInfo info = Achievements.Find(id);
            if (info != null)
            {
                newlyUnlocked.Add(info);
            }
        }
    }
}
