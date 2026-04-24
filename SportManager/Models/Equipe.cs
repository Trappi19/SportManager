using System.Collections.Generic;
using System.Linq;

namespace SportManager.Models
{
    public class Equipe
    {
        public int           Id           { get; set; }
        public string        Nom          { get; set; } = "";
        public int           ScoreGeneral { get; set; }
        public int[]         JoueurIds    { get; set; } = new int[7];
        public List<Joueur>  Joueurs      { get; set; } = new();

        public string JoueursDisplay =>
            Joueurs.Count > 0
                ? string.Join(", ", Joueurs.Select(j => j.Nom))
                : string.Join(", ", JoueurIds);

        public override string ToString() => Nom;
    }
}
