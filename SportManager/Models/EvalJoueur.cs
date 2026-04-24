namespace SportManager.Models
{
    public class EvalJoueur
    {
        public int    Id        { get; set; }
        public string Nom       { get; set; } = "";
        public string NomEquipe { get; set; } = "";
        public string Note      { get; set; } = "Bon";
    }
}
