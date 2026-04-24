using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportManager.Models
{
    public class EvalJoueur : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int    Id        { get; set; }
        public string Nom       { get; set; } = "";
        public string NomEquipe { get; set; } = "";

        private string _note = "Bon";
        public string Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(); }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
