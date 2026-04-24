using SportManager.Models;
using SportManager.Services;

namespace SportManager
{
    public partial class EditPlayerDialog : Window
    {
        private readonly DatabaseService _db = new();
        private readonly Joueur _joueur;

        public EditPlayerDialog(Joueur joueur)
        {
            InitializeComponent();
            _joueur = joueur;
            Prefill();
        }

        private void Prefill()
        {
            TbNom.Text   = _joueur.Nom;
            SlDef.Value  = _joueur.ScoreDefense;
            SlAtt.Value  = _joueur.ScoreAttaque;
            SlGoal.Value = _joueur.ScoreVitesse;

            foreach (ComboBoxItem item in CbPoste.Items)
                if (item.Content.ToString() == _joueur.Affectation)
                { CbPoste.SelectedItem = item; break; }

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (LblDef == null) return;
            LblDef.Text  = ((int)SlDef.Value).ToString();
            LblAtt.Text  = ((int)SlAtt.Value).ToString();
            LblGoal.Text = ((int)SlGoal.Value).ToString();
            int gen = Joueur.CalculerScoreGeneral((int)SlDef.Value, (int)SlAtt.Value, (int)SlGoal.Value);
            LblGen.Text  = $"{gen} / 10";
        }

        private void Score_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
            => UpdateLabels();

        private void Enregistrer_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CbPoste.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un poste.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _joueur.Nom          = TbNom.Text.Trim();
                _joueur.Affectation  = ((ComboBoxItem)CbPoste.SelectedItem).Content.ToString()!;
                _joueur.ScoreDefense = (int)SlDef.Value;
                _joueur.ScoreAttaque = (int)SlAtt.Value;
                _joueur.ScoreVitesse    = (int)SlGoal.Value;
                _joueur.ScoreGeneral = Joueur.CalculerScoreGeneral(
                    _joueur.ScoreDefense, _joueur.ScoreAttaque, _joueur.ScoreVitesse);
                _db.UpdateJoueur(_joueur);
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
