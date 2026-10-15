using System;
using System.Collections.Generic;

namespace OnePercent
{
    /// <summary>Datos que se guardan en disco (JSON). Solo campos simples para que JsonUtility los serialice.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;

        public long totalCoinsEarned;
        public int gamesPlayed;
        public int victories;
        public int defeats;
        public int timesCheatedSystem;
        public int bestCoins;

        // Un valor por símbolo: 0 = Sol, 1 = Luna, 2 = Ojo (mismo orden que BackgroundSymbol).
        public int[] symbolModerateBets = new int[Labels.SymbolCount];
        public int[] symbolModerateWins = new int[Labels.SymbolCount];

        public int afterWinStreakBets;
        public int afterWinStreakWins;
        public int afterLossStreakBets;
        public int afterLossStreakWins;

        public List<string> unlockedAchievements = new List<string>();

        /// <summary>Repara datos incompletos (por ejemplo, un archivo antiguo o editado a mano).</summary>
        public void EnsureValid()
        {
            if (symbolModerateBets == null || symbolModerateBets.Length != Labels.SymbolCount)
            {
                symbolModerateBets = new int[Labels.SymbolCount];
            }

            if (symbolModerateWins == null || symbolModerateWins.Length != Labels.SymbolCount)
            {
                symbolModerateWins = new int[Labels.SymbolCount];
            }

            if (unlockedAchievements == null)
            {
                unlockedAchievements = new List<string>();
            }
        }
    }
}
