namespace SportManager.Models
{
    public class Joueur
    {
        public int    Id                      { get; set; }
        public string Nom                     { get; set; } = "";
        public int    ScoreDefense            { get; set; }
        public int    ScoreAttaque            { get; set; }
        public int    ScoreVitesse            { get; set; }
        public int    ScoreEndurance          { get; set; }
        public int    ScoreGeneral            { get; set; }
        public int    ButsMarques             { get; set; }
        public string Affectation             { get; set; } = "";
        public int?   IdBlessure              { get; set; }
        public int    MatchsRestantsBlessure  { get; set; }
        public string? TypeBlessure           { get; set; }
        public int?   Penalite               { get; set; }

        public string BlessureDisplay =>
            TypeBlessure != null ? $"{TypeBlessure} ({Penalite})" : "Aucune";

        public string DisplayLabel => $"{Nom} (#{Id})";

        public override string ToString() => DisplayLabel;

        // endurance a un défaut de 50 pour la compatibilité avec l'ancien code à 3 params
        public static int CalculerScoreGeneral(int defense, int attaque, int vitesse, int endurance = 50)
            => (defense + attaque + vitesse + endurance) / 4;

        public static bool QualifiePour(string poste, int defense, int attaque, int vitesse, int endurance) =>
            poste switch
            {
                "Poursuiveur" => attaque >= 50 && vitesse >= 40,
                "Batteur"     => defense >= 50 && endurance >= 40,
                "Gardien"     => defense >= 60 && endurance >= 50,
                "Attrapeur"   => vitesse >= 70 && endurance >= 40,
                _             => true,
            };

        public static string ExigencesPoste(string poste) =>
            poste switch
            {
                "Poursuiveur" => "Attaque >= 50  •  Vitesse >= 40",
                "Batteur"     => "Defense >= 50  •  Endurance >= 40",
                "Gardien"     => "Defense >= 60  •  Endurance >= 50",
                "Attrapeur"   => "Vitesse >= 70  •  Endurance >= 40",
                _             => "Aucune exigence particuliere",
            };
    }
}
