using System;

namespace SportManager.Models
{
    /// <summary>
    /// Résultat d'un match stocké en BDD, utilisé dans le DataGrid historique.
    /// </summary>
    public class MatchResult
    {
        // ── Clés ──────────────────────────────────────────────────
        public int      Id           { get; set; }
        public int      IdEquipe1    { get; set; }
        public int      IdEquipe2    { get; set; }

        // ── Noms (jointure SQL, pas de FK navigable en Dapper) ────
        public string   NomEquipe1   { get; set; } = "";
        public string   NomEquipe2   { get; set; } = "";

        // ── Scores et date ────────────────────────────────────────
        public int      ScoreEquipe1 { get; set; }
        public int      ScoreEquipe2 { get; set; }
        public DateTime DateMatch    { get; set; }

        // ── Propriétés calculées pour l'affichage ─────────────────
        public string Resultat     => $"{ScoreEquipe1} - {ScoreEquipe2}";
        public string DateDisplay  => DateMatch.ToString("dd/MM/yyyy HH:mm");
        /// <summary>Retourne le nom du vainqueur ou "Egalité" si scores identiques.</summary>
        public string Vainqueur    =>
            ScoreEquipe1 > ScoreEquipe2 ? NomEquipe1 :
            ScoreEquipe2 > ScoreEquipe1 ? NomEquipe2 : "Egalité";
    }

    /// <summary>
    /// Enregistrement temporaire d'un but durant la simulation (non persisté directement).
    /// Utilisé pour construire le récapitulatif buteurs et mettre à jour score_goal en BDD.
    /// </summary>
    public class ButInfo
    {
        public int    IdJoueur    { get; set; }
        public string NomJoueur  { get; set; } = "";
        public int    IdEquipe    { get; set; }
        /// <summary>1 = première mi-temps, 2 = deuxième mi-temps.</summary>
        public int    NumMiTemps { get; set; }
    }
}
