using System.Collections.Generic;
using System.Linq;

namespace SportManager.Models
{
    /// <summary>
    /// Représente une équipe de 7 joueurs (3 Poursuiveurs, 2 Batteurs, 1 Gardien, 1 Attrapeur).
    /// </summary>
    public class Equipe
    {
        // ── Identité ──────────────────────────────────────────────
        public int           Id           { get; set; }
        public string        Nom          { get; set; } = "";

        /// <summary>Score moyen de l'équipe, calculé à partir des scores généraux des joueurs.</summary>
        public int           ScoreGeneral { get; set; }

        // ── Composition ───────────────────────────────────────────

        /// <summary>
        /// Tableau des 7 IDs joueurs (positions 0-6 = P1,P2,P3,B1,B2,G,A).
        /// Un ID à 0 signifie que le slot est vide.
        /// </summary>
        public int[]         JoueurIds    { get; set; } = new int[7];

        /// <summary>Objets Joueur chargés depuis la BDD — rempli par DatabaseService.GetAllEquipes().</summary>
        public List<Joueur>  Joueurs      { get; set; } = new();

        // ── Propriétés calculées ──────────────────────────────────

        /// <summary>
        /// Texte affiché dans le DataGrid : noms des joueurs si déjà chargés, sinon les IDs bruts.
        /// </summary>
        public string JoueursDisplay =>
            Joueurs.Count > 0
                ? string.Join(", ", Joueurs.Select(j => j.Nom))
                : string.Join(", ", JoueurIds);

        public override string ToString() => Nom;
    }
}
