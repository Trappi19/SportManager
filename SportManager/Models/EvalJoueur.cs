using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportManager.Models
{
    /// <summary>
    /// Modèle utilisé dans le DataGrid d'évaluation post-match.
    /// Implémente INotifyPropertyChanged pour que la ComboBox se mette à jour en temps réel.
    /// </summary>
    public class EvalJoueur : INotifyPropertyChanged
    {
        // Déclenché automatiquement quand une propriété change (requis par WPF data binding)
        public event PropertyChangedEventHandler? PropertyChanged;

        public int    Id        { get; set; }
        public string Nom       { get; set; } = "";
        public string NomEquipe { get; set; } = "";

        // La note est en champ privé + propriété pour notifier le binding WPF à chaque changement
        private string _note = "Bon";
        public string Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Notifie le binding WPF que la propriété [name] a changé.
        /// CallerMemberName injecte automatiquement le nom de la propriété appelante.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
