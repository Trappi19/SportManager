namespace SportManager.Models
{
    /// <summary>
    /// Représente un joueur de Quidditch avec ses statistiques, son affectation et son état de blessure.
    /// </summary>
    public class Joueur
    {
        // ── Identité ──────────────────────────────────────────────
        public int    Id                      { get; set; }
        public string Nom                     { get; set; } = "";

        // ── Statistiques (0-100) ──────────────────────────────────
        public int    ScoreDefense            { get; set; }
        public int    ScoreAttaque            { get; set; }
        public int    ScoreVitesse            { get; set; }
        public int    ScoreEndurance          { get; set; }
        /// <summary>Moyenne des 4 stats — recalculée à chaque modification.</summary>
        public int    ScoreGeneral            { get; set; }

        // ── Statistiques de match ─────────────────────────────────
        public int    ButsMarques             { get; set; }

        // ── Poste et blessure ─────────────────────────────────────
        public string Affectation             { get; set; } = "";
        public int?   IdBlessure              { get; set; }
        /// <summary>Nombre de matchs restants avant guérison complète.</summary>
        public int    MatchsRestantsBlessure  { get; set; }
        public string? TypeBlessure           { get; set; }
        /// <summary>Pénalité appliquée au score effectif pendant la blessure (valeur négative).</summary>
        public int?   Penalite               { get; set; }

        // ── Propriétés calculées ──────────────────────────────────

        /// <summary>Texte affiché dans le DataGrid : type de blessure et sa pénalité, ou "Aucune".</summary>
        public string BlessureDisplay =>
            TypeBlessure != null ? $"{TypeBlessure} ({Penalite})" : "Aucune";

        /// <summary>Label court utilisé dans les ComboBox de sélection d'équipe.</summary>
        public string DisplayLabel => $"{Nom} (#{Id})";

        public override string ToString() => DisplayLabel;

        // ── Méthodes statiques (logique métier) ───────────────────

        /// <summary>
        /// Calcule la moyenne des 4 statistiques.
        /// L'endurance vaut 50 par défaut pour rester compatible avec l'ancienne version 3 paramètres.
        /// </summary>
        public static int CalculerScoreGeneral(int defense, int attaque, int vitesse, int endurance = 50)
            => (defense + attaque + vitesse + endurance) / 4;

        /// <summary>
        /// Vérifie si le joueur satisfait les exigences minimales du poste donné.
        /// Utilisé à la création/modification pour bloquer les affectations invalides.
        /// </summary>
        public static bool QualifiePour(string poste, int defense, int attaque, int vitesse, int endurance) =>
            poste switch
            {
                "Poursuiveur" => attaque >= 50 && vitesse >= 40,
                "Batteur"     => defense >= 50 && endurance >= 40,
                "Gardien"     => defense >= 60 && endurance >= 50,
                "Attrapeur"   => vitesse >= 70 && endurance >= 40,
                _             => true,  // poste inconnu : aucune restriction
            };

        /// <summary>Retourne le texte des exigences affiché dans le formulaire joueur.</summary>
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
