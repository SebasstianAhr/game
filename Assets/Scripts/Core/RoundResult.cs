namespace OnePercent
{
    /// <summary>Datos de una ronda ya resuelta. Los usan el historial, las estadísticas y los mensajes.</summary>
    public class RoundResult
    {
        public int RoundNumber;
        public BetAction Action;
        public BackgroundSymbol Symbol;

        /// <summary>true si fue una apuesta (moderada o todo o nada). Guardar no es apuesta.</summary>
        public bool IsBet;
        public bool Won;

        public int Stake;
        public int Fee;
        /// <summary>Monedas recibidas en la ronda (premio de la apuesta o retorno de Guardar).</summary>
        public int GrossGain;

        public int CoinsBefore;
        public int CoinsAfter;

        public float DisplayedProbability;
        public float RealProbability;
        public bool StreakModifierApplied;

        /// <summary>Ganó una apuesta mientras la probabilidad real superaba a la mostrada.</summary>
        public bool ExploitedHiddenRule;

        public int WinStreakBefore;
        public int LossStreakBefore;

        public int NetChange
        {
            get { return CoinsAfter - CoinsBefore; }
        }
    }
}
