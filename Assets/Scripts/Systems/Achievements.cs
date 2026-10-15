namespace OnePercent
{
    public sealed class AchievementInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;

        public AchievementInfo(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    /// <summary>Catálogo de logros del MVP.</summary>
    public static class Achievements
    {
        public const string FirstVictory = "first_victory";
        public const string AllInWin = "all_in_win";
        public const string ShieldSaved = "shield_saved";
        public const string SharpEye = "sharp_eye";
        public const string Thousand = "thousand";

        public static readonly AchievementInfo[] All =
        {
            new AchievementInfo(FirstVictory, "Libre", "Gana tu primera partida."),
            new AchievementInfo(AllInWin, "El 1 %", "Gana una apuesta de todo o nada."),
            new AchievementInfo(ShieldSaved, "Salvado por el Escudo", "Consigue monedas en el minijuego El Escudo."),
            new AchievementInfo(SharpEye, "Ojo clínico", "Engaña al sistema 5 veces en total."),
            new AchievementInfo(Thousand, "Primer millar", "Llega a 1000 monedas en una partida.")
        };

        public static AchievementInfo Find(string id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}
