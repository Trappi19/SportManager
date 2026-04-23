using SportManager.Models;
using SportManager.Services;
using System.Collections.Generic;
using System.Linq;

namespace SportManager
{
    public partial class JoueursWindow : Window
    {
        private readonly DatabaseService _db = new();
        private List<Joueur> _all = new();
        private bool _modeCreation;
        private Joueur? _joueurEnEdition;

        public JoueursWindow()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            try
            {
                _all = _db.GetAllJoueurs();
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh()
        {
            string q = SearchBox.Text.Trim().ToLower();
            Grid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(j =>
                    j.Nom.ToLower().Contains(q) ||
                    j.Affectation.ToLower().Contains(q)).ToList();
        }

        private Joueur? Selected => Grid.SelectedItem as Joueur;

        private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            bool has = Selected != null;
            BtnModifier.IsEnabled  = has;
            BtnSupprimer.IsEnabled = has;
        }

        private void SearchBox_TextChanged(object s, TextChangedEventArgs e) => Refresh();

        // ── Panneau form ──────────────────────────────────────

        private void ShowFormCreate()
        {
            _modeCreation    = true;
            _joueurEnEdition = null;
            TbFormTitle.Text  = "NOUVEAU JOUEUR";
            TbNom.Text        = string.Empty;
            CbPoste.SelectedIndex = -1;
            SlDef.Value = SlAtt.Value = SlGoal.Value = 0;
            UpdateScoreLabel();
            FormPanel.Visibility = Visibility.Visible;
        }

        private void ShowFormEdit(Joueur j)
        {
            _modeCreation    = false;
            _joueurEnEdition = j;
            TbFormTitle.Text  = "MODIFIER LE JOUEUR";
            TbNom.Text        = j.Nom;
            SlDef.Value       = j.ScoreDefense;
            SlAtt.Value       = j.ScoreAttaque;
            SlGoal.Value      = j.ScoreGoal;
            CbPoste.SelectedIndex = -1;
            foreach (ComboBoxItem item in CbPoste.Items)
                if (item.Content.ToString() == j.Affectation)
                { CbPoste.SelectedItem = item; break; }
            UpdateScoreLabel();
            FormPanel.Visibility = Visibility.Visible;
        }

        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            _joueurEnEdition = null;
        }

        private void Score_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
            => UpdateScoreLabel();

        private void UpdateScoreLabel()
        {
            if (LblDef == null) return;
            LblDef.Text  = ((int)SlDef.Value).ToString();
            LblAtt.Text  = ((int)SlAtt.Value).ToString();
            LblGoal.Text = ((int)SlGoal.Value).ToString();
            LblGen.Text  = $"{Joueur.CalculerScoreGeneral((int)SlDef.Value, (int)SlAtt.Value, (int)SlGoal.Value)} / 10";
        }

        // ── Boutons footer ────────────────────────────────────

        private void Ajouter_Click(object s, RoutedEventArgs e)   => ShowFormCreate();
        private void Modifier_Click(object s, RoutedEventArgs e)
        {
            if (Selected != null) ShowFormEdit(Selected);
        }

        private void Supprimer_Click(object s, RoutedEventArgs e)
        {
            if (Selected == null) return;
            var res = MessageBox.Show($"Supprimer « {Selected.Nom} » ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try { _db.DeleteJoueur(Selected.Id); HideForm(); Load(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Boutons panneau form ──────────────────────────────

        private void Enregistrer_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            { MessageBox.Show("Le nom est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CbPoste.SelectedItem == null)
            { MessageBox.Show("Veuillez sélectionner un poste.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            string poste = ((ComboBoxItem)CbPoste.SelectedItem).Content.ToString()!;
            int def = (int)SlDef.Value, att = (int)SlAtt.Value, goal = (int)SlGoal.Value;
            int gen = Joueur.CalculerScoreGeneral(def, att, goal);

            try
            {
                if (_modeCreation)
                {
                    _db.CreateJoueur(new Joueur
                    {
                        Nom = TbNom.Text.Trim(), Affectation = poste,
                        ScoreDefense = def, ScoreAttaque = att,
                        ScoreGoal = goal, ScoreGeneral = gen,
                    });
                }
                else if (_joueurEnEdition != null)
                {
                    _joueurEnEdition.Nom = TbNom.Text.Trim();
                    _joueurEnEdition.Affectation  = poste;
                    _joueurEnEdition.ScoreDefense = def;
                    _joueurEnEdition.ScoreAttaque = att;
                    _joueurEnEdition.ScoreGoal    = goal;
                    _joueurEnEdition.ScoreGeneral = gen;
                    _db.UpdateJoueur(_joueurEnEdition);
                }
                HideForm();
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AnnulerForm_Click(object s, RoutedEventArgs e) => HideForm();
        private void Actualiser_Click(object s, RoutedEventArgs e)   => Load();
        private void Retour_Click(object s, RoutedEventArgs e)        => Close();
    }
}
