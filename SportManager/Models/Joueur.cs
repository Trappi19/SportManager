namespace SportManager.Models
{
    public class Joueur
    {
        public int    Id                      { get; set; }
        public string Nom                     { get; set; } = "";
        public int    ScoreDefense            { get; set; }
        public int    ScoreAttaque            { get; set; }
        public int    ScoreGoal               { get; set; }
        public int    ScoreGeneral            { get; set; }
        public string Affectation             { get; set; } = "";
        public int?   IdBlessure              { get; set; }
        public int    MatchsRestantsBlessure  { get; set; }
        public string? TypeBlessure           { get; set; }
        public int?   Penalite               { get; set; }

        public string BlessureDisplay =>
            TypeBlessure != null ? $"{TypeBlessure} ({Penalite})" : "Aucune";

        public string DisplayLabel => $"{Nom} (#{Id})";

        public static int CalculerScoreGeneral(int defense, int attaque, int goal)
            => (defense + attaque + goal) / 3;
    }
}
