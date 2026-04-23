using System;

namespace SportManager.Models
{
    public class MatchResult
    {
        public int      Id           { get; set; }
        public int      IdEquipe1    { get; set; }
        public int      IdEquipe2    { get; set; }
        public string   NomEquipe1   { get; set; } = "";
        public string   NomEquipe2   { get; set; } = "";
        public int      ScoreEquipe1 { get; set; }
        public int      ScoreEquipe2 { get; set; }
        public DateTime DateMatch    { get; set; }

        public string Resultat     => $"{ScoreEquipe1} - {ScoreEquipe2}";
        public string DateDisplay  => DateMatch.ToString("dd/MM/yyyy HH:mm");
        public string Vainqueur    =>
            ScoreEquipe1 > ScoreEquipe2 ? NomEquipe1 :
            ScoreEquipe2 > ScoreEquipe1 ? NomEquipe2 : "Egalité";
    }

    public class ButInfo
    {
        public int    IdJoueur     { get; set; }
        public string NomJoueur   { get; set; } = "";
        public int    IdEquipe     { get; set; }
        public int    NumMiTemps  { get; set; }
    }
}
