using SportManager.Models;
using SportManager.Services;
using System.Linq;

namespace SportManager
{
    public partial class EditTeamDialog : Window
    {
        private readonly DatabaseService _db = new();
        private readonly Equipe _equipe;

        public EditTeamDialog(Equipe equipe)
        {
            InitializeComponent();
            _equipe = equipe;
            LoadAndPrefill();
        }

        private void LoadAndPrefill()
        {
            try
            {
                var poursuiveurs = _db.GetJoueursByAffectation("Poursuiveur");
                var batteurs     = _db.GetJoueursByAffectation("Batteur");
                var gardiens     = _db.GetJoueursByAffectation("Gardien");
                var attrapeurs   = _db.GetJoueursByAffectation("Attrapeur");

                CbP1.ItemsSource = CbP2.ItemsSource = CbP3.ItemsSource = poursuiveurs;
                CbB1.ItemsSource = CbB2.ItemsSource = batteurs;
                CbG.ItemsSource  = gardiens;
                CbA.ItemsSource  = attrapeurs;

                TbNom.Text = _equipe.Nom;

                var p = _equipe.Joueurs.Where(j => j.Affectation == "Poursuiveur").ToList();
                var b = _equipe.Joueurs.Where(j => j.Affectation == "Batteur").ToList();
                var g = _equipe.Joueurs.Where(j => j.Affectation == "Gardien").ToList();
                var a = _equipe.Joueurs.Where(j => j.Affectation == "Attrapeur").ToList();

                if (p.Count > 0) CbP1.SelectedItem = poursuiveurs.FirstOrDefault(x => x.Id == p[0].Id);
                if (p.Count > 1) CbP2.SelectedItem = poursuiveurs.FirstOrDefault(x => x.Id == p[1].Id);
                if (p.Count > 2) CbP3.SelectedItem = poursuiveurs.FirstOrDefault(x => x.Id == p[2].Id);
                if (b.Count > 0) CbB1.SelectedItem = batteurs.FirstOrDefault(x => x.Id == b[0].Id);
                if (b.Count > 1) CbB2.SelectedItem = batteurs.FirstOrDefault(x => x.Id == b[1].Id);
                if (g.Count > 0) CbG.SelectedItem  = gardiens.FirstOrDefault(x => x.Id == g[0].Id);
                if (a.Count > 0) CbA.SelectedItem  = attrapeurs.FirstOrDefault(x => x.Id == a[0].Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Enregistrer_Click(object s, RoutedEventArgs e)
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
                _equipe.Nom       = TbNom.Text.Trim();
                _equipe.JoueurIds = ids;
                _db.UpdateEquipe(_equipe);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Annuler_Click(object s, RoutedEventArgs e) => DialogResult = false;
    }
}
