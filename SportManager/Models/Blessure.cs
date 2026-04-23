namespace SportManager.Models
{
    public class Blessure
    {
        public int    Id       { get; set; }
        public string Type     { get; set; } = "";
        public int    Penalite { get; set; }

        public override string ToString() => $"{Type} ({Penalite})";
    }
}
