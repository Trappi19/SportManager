using SportManager.Models;
using SportManager.Services;

namespace SportManager.Views.Dialogs
{
    public partial class CreatePlayerDialog : Window
    {
        private readonly DatabaseService _db = new();

        public CreatePlayerDialog()
        {
            InitializeComponent();
        }

        private void Score_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LblDef == null) return;
            LblDef.Text  = ((int)SlDef.Value).ToString();
            LblAtt.Text  = ((int)SlAtt.Value).ToString();
            LblGoal.Text = ((int)SlGoal.Value).ToString();
            int gen = Joueur.CalculerScoreGeneral((int)SlDef.Value, (int)SlAtt.Value, (int)SlGoal.Value);
            LblGen.Text  = $"{gen} / 10";
        }

        private void Creer_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            {
                MessageBox.Show("Le nom du joueur est obligatoire.", "Erreur",
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
                var j = new Joueur
                {
                    Nom          = TbNom.Text.Trim(),
                    Affectation  = ((System.Windows.Controls.ComboBoxItem)CbPoste.SelectedItem).Content.ToString()!,
                    ScoreDefense = (int)SlDef.Value,
                    ScoreAttaque = (int)SlAtt.Value,
                    ScoreVitesse    = (int)SlGoal.Value,
                    ScoreGeneral = Joueur.CalculerScoreGeneral((int)SlDef.Value, (int)SlAtt.Value, (int)SlGoal.Value),
                };
                _db.CreateJoueur(j);
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
