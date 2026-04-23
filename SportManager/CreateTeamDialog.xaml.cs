using SportManager.Models;
using SportManager.Services;
using System.Collections.Generic;
using System.Linq;

namespace SportManager
{
    public partial class CreateTeamDialog : Window
    {
        private readonly DatabaseService _db = new();

        public CreateTeamDialog()
        {
            InitializeComponent();
            LoadJoueurs();
        }

        private void LoadJoueurs()
        {
            try
            {
                CbP1.ItemsSource = CbP2.ItemsSource = CbP3.ItemsSource = _db.GetJoueursByAffectation("Poursuiveur");
                CbB1.ItemsSource = CbB2.ItemsSource                    = _db.GetJoueursByAffectation("Batteur");
                CbG.ItemsSource                                         = _db.GetJoueursByAffectation("Gardien");
                CbA.ItemsSource                                         = _db.GetJoueursByAffectation("Attrapeur");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement des joueurs : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Creer_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selections = new[]
            {
                CbP1.SelectedItem as Joueur, CbP2.SelectedItem as Joueur, CbP3.SelectedItem as Joueur,
                CbB1.SelectedItem as Joueur, CbB2.SelectedItem as Joueur,
                CbG.SelectedItem  as Joueur,
                CbA.SelectedItem  as Joueur
            };

            if (selections.Any(j => j == null))
            {
                MessageBox.Show("Veuillez sélectionner les 7 joueurs.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ids = selections.Select(j => j!.Id).ToArray();
            if (ids.Distinct().Count() != 7)
            {
                MessageBox.Show("Un même joueur ne peut pas occuper deux postes.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var equipe = new Equipe { Nom = TbNom.Text.Trim(), JoueurIds = ids };
                _db.CreateEquipe(equipe);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Annuler_Click(object s, RoutedEventArgs e) => DialogResult = false;
    }
}
