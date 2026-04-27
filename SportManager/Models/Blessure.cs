namespace SportManager.Models
{
    /// <summary>
    /// Représente un type de blessure possible (chargé depuis la table `blessures` en BDD).
    /// La pénalité est un entier négatif ajouté au score effectif du joueur pendant sa blessure.
    /// </summary>
    public class Blessure
    {
        public int    Id       { get; set; }
        public string Type     { get; set; } = "";
        /// <summary>Malus appliqué au score effectif du joueur (ex : -10).</summary>
        public int    Penalite { get; set; }

        public override string ToString() => $"{Type} ({Penalite})";
    }
}
