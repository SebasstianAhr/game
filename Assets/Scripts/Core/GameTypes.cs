using System.Globalization;
using UnityEngine;

namespace OnePercent
{
    /// <summary>Símbolo decorativo del fondo. Es la señal que modifica en secreto la probabilidad real.</summary>
    public enum BackgroundSymbol
    {
        Sun = 0,
        Moon = 1,
        Eye = 2
    }

    /// <summary>Las tres decisiones que ofrece la mesa.</summary>
    public enum BetAction
    {
        Save = 0,
        Moderate = 1,
        AllIn = 2
    }

    public enum GameOutcome
    {
        InProgress,
        Victory,
        Defeat
    }

    /// <summary>Textos y colores compartidos por la interfaz.</summary>
    public static class Labels
    {
        public const int SymbolCount = 3;

        public static string SymbolName(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return "Sol";
                case BackgroundSymbol.Moon: return "Luna";
                case BackgroundSymbol.Eye: return "Ojo";
                default: return "?";
            }
        }

        public static Color SymbolColor(BackgroundSymbol symbol)
        {
            switch (symbol)
            {
                case BackgroundSymbol.Sun: return new Color(0.95f, 0.78f, 0.20f);
                case BackgroundSymbol.Moon: return new Color(0.45f, 0.60f, 0.95f);
                case BackgroundSymbol.Eye: return new Color(0.80f, 0.30f, 0.60f);
                default: return Color.gray;
            }
        }

        public static string ActionName(BetAction action)
        {
            switch (action)
            {
                case BetAction.Save: return "Guardar";
                case BetAction.Moderate: return "Apuesta moderada";
                case BetAction.AllIn: return "Todo o nada";
                default: return "?";
            }
        }

        /// <summary>Convierte 0.5 en "50 %". Usa cultura invariante para evitar comas o puntos inesperados.</summary>
        public static string Percent(float probability)
        {
            return (probability * 100f).ToString("0.##", CultureInfo.InvariantCulture) + " %";
        }

        public static string Signed(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
