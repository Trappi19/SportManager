using SportManager.Models;
using SportManager.Services;
using System.Collections.Generic;
using System.Linq;

namespace SportManager.Views.Windows
{
    public partial class JoueursWindow : UserControl
    {
        /// <summary>Déclenché quand l'utilisateur clique sur "Retour" → MainWindow revient à l'accueil.</summary>
        public event EventHandler? Retour;

        private readonly DatabaseService _db = new();
        private List<Joueur> _all = new();   // cache local de tous les joueurs (pour le filtre côté client)
        private bool _modeCreation;          // true = création, false = modification
        private Joueur? _joueurEnEdition;    // référence au joueur en cours de modification

        public JoueursWindow()
        {
            InitializeComponent();
            // Recharge les données chaque fois que le UserControl devient visible (retour depuis une autre section)
            IsVisibleChanged += (_, e) => { if ((bool)e.NewValue) Load(); };
        }

        /// <summary>Charge tous les joueurs depuis la BDD et met à jour le DataGrid.</summary>
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

        /// <summary>
        /// Filtre _all selon le texte de SearchBox et rebind le DataGrid.
        /// Le filtre s'applique sur le nom ET le poste (affectation).
        /// </summary>
        private void Refresh()
        {
            string q = SearchBox.Text.Trim().ToLower();
            Grid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(j =>
                    j.Nom.ToLower().Contains(q) ||
                    j.Affectation.ToLower().Contains(q)).ToList();
        }

        // Raccourci pour obtenir le joueur sélectionné dans le DataGrid
        private Joueur? Selected => Grid.SelectedItem as Joueur;

        /// <summary>
        /// Active/désactive les boutons Modifier et Supprimer selon la sélection.
        /// Si le panneau de modification est déjà ouvert, rafraîchit automatiquement son contenu.
        /// </summary>
        private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            bool has = Selected != null;
            BtnModifier.IsEnabled  = has;
            BtnSupprimer.IsEnabled = has;

            // Si le panneau droit est ouvert en mode modification, on le met à jour avec le nouveau joueur sélectionné
            if (!_modeCreation && FormPanel.Visibility == Visibility.Visible && Selected != null)
                ShowFormEdit(Selected);
        }

        // Déclenche un re-filtre à chaque frappe dans la barre de recherche
        private void SearchBox_TextChanged(object s, TextChangedEventArgs e) => Refresh();

        // ── Panneau form (droite) ──────────────────────────────

        /// <summary>Initialise le formulaire en mode création (champs vides).</summary>
        private void ShowFormCreate()
        {
            _modeCreation    = true;
            _joueurEnEdition = null;
            TbFormTitle.Text  = "NOUVEAU JOUEUR";
            TbNom.Text        = string.Empty;
            CbPoste.SelectedIndex = -1;
            SlDef.Value = SlAtt.Value = SlVitesse.Value = SlEndurance.Value = 0;
            PanelButs.Visibility = Visibility.Collapsed;  // le champ Buts n'est utile qu'en modification
            UpdateScoreLabel();
            FormPanel.Visibility = Visibility.Visible;
        }

        /// <summary>Pré-remplit le formulaire avec les données du joueur à modifier.</summary>
        private void ShowFormEdit(Joueur j)
        {
            _modeCreation    = false;
            _joueurEnEdition = j;
            TbFormTitle.Text  = "MODIFIER LE JOUEUR";
            TbNom.Text        = j.Nom;
            SlDef.Value       = j.ScoreDefense;
            SlAtt.Value       = j.ScoreAttaque;
            SlVitesse.Value   = j.ScoreVitesse;
            SlEndurance.Value = j.ScoreEndurance;
            TbButs.Text       = j.ButsMarques.ToString();
            PanelButs.Visibility = Visibility.Visible;
            // Sélectionne le bon poste dans le ComboBox en comparant le texte
            CbPoste.SelectedIndex = -1;
            foreach (ComboBoxItem item in CbPoste.Items)
                if (item.Content.ToString() == j.Affectation)
                { CbPoste.SelectedItem = item; break; }
            UpdateScoreLabel();
            FormPanel.Visibility = Visibility.Visible;
        }

        /// <summary>Masque le panneau formulaire et réinitialise la référence d'édition.</summary>
        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            _joueurEnEdition = null;
        }

        // Appelé à chaque déplacement d'un slider → met à jour les labels de valeur et le badge de poste
        private void Score_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateScoreLabel();
            UpdatePosteBadge();
        }

        // Appelé quand le poste change → recalcule le badge d'exigences
        private void CbPoste_Changed(object s, SelectionChangedEventArgs e) => UpdatePosteBadge();

        /// <summary>Met à jour les TextBlock de valeur (LblDef, LblAtt, etc.) et le score général.</summary>
        private void UpdateScoreLabel()
        {
            if (LblDef == null) return;  // garde contre les appels avant InitializeComponent
            LblDef.Text      = ((int)SlDef.Value).ToString();
            LblAtt.Text      = ((int)SlAtt.Value).ToString();
            LblVitesse.Text  = ((int)SlVitesse.Value).ToString();
            LblEndurance.Text = ((int)SlEndurance.Value).ToString();
            int gen = Joueur.CalculerScoreGeneral(
                (int)SlDef.Value, (int)SlAtt.Value, (int)SlVitesse.Value, (int)SlEndurance.Value);
            LblGen.Text = $"{gen} / 100";
        }

        /// <summary>
        /// Affiche les exigences du poste sélectionné et colore le badge en vert (OK) ou rouge (insuffisant).
        /// </summary>
        private void UpdatePosteBadge()
        {
            if (TbExigences == null || CbPoste?.SelectedItem == null) return;
            string poste    = ((ComboBoxItem)CbPoste.SelectedItem).Content.ToString()!;
            string exigences = Joueur.ExigencesPoste(poste);
            TbExigences.Text = exigences;

            bool ok = Joueur.QualifiePour(poste,
                (int)SlDef.Value, (int)SlAtt.Value,
                (int)SlVitesse.Value, (int)SlEndurance.Value);
            // Vert si les stats sont suffisantes, rouge sinon
            TbExigences.Foreground = ok
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C));
        }

        // ── Boutons footer ────────────────────────────────────

        private void Ajouter_Click(object s, RoutedEventArgs e)
        {
            // Si le panneau est déjà ouvert en mode création → on le ferme (toggle)
            if (_modeCreation && FormPanel.Visibility == Visibility.Visible)
            { HideForm(); return; }
            ShowFormCreate();
        }

        private void Modifier_Click(object s, RoutedEventArgs e)
        {
            if (Selected == null) return;
            // Si le panneau est déjà ouvert sur ce même joueur en mode modification → on le ferme (toggle)
            if (!_modeCreation && FormPanel.Visibility == Visibility.Visible && _joueurEnEdition?.Id == Selected.Id)
            { HideForm(); return; }
            ShowFormEdit(Selected);
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

        /// <summary>
        /// Valide et enregistre le joueur (création ou modification).
        /// Bloque si le nom est vide, si aucun poste n'est choisi, ou si les stats sont insuffisantes.
        /// </summary>
        private void Enregistrer_Click(object s, RoutedEventArgs e)
        {
            // Validations de base
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            { MessageBox.Show("Le nom est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CbPoste.SelectedItem == null)
            { MessageBox.Show("Veuillez sélectionner un poste.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            string poste = ((ComboBoxItem)CbPoste.SelectedItem).Content.ToString()!;
            int def = (int)SlDef.Value, att = (int)SlAtt.Value;
            int vit = (int)SlVitesse.Value, end = (int)SlEndurance.Value;
            int gen = Joueur.CalculerScoreGeneral(def, att, vit, end);

            // Vérifie que les stats satisfont les exigences du poste
            if (!Joueur.QualifiePour(poste, def, att, vit, end))
            {
                MessageBox.Show(
                    $"Ce joueur ne remplit pas les exigences du poste {poste}.\n\n{Joueur.ExigencesPoste(poste)}",
                    "Stats insuffisantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_modeCreation)
                {
                    _db.CreateJoueur(new Joueur
                    {
                        Nom = TbNom.Text.Trim(), Affectation = poste,
                        ScoreDefense = def, ScoreAttaque = att,
                        ScoreVitesse = vit, ScoreEndurance = end, ScoreGeneral = gen,
                    });
                }
                else if (_joueurEnEdition != null)
                {
                    // Met à jour l'objet en mémoire puis envoie l'UPDATE en BDD
                    _joueurEnEdition.Nom           = TbNom.Text.Trim();
                    _joueurEnEdition.Affectation   = poste;
                    _joueurEnEdition.ScoreDefense  = def;
                    _joueurEnEdition.ScoreAttaque  = att;
                    _joueurEnEdition.ScoreVitesse  = vit;
                    _joueurEnEdition.ScoreEndurance = end;
                    _joueurEnEdition.ScoreGeneral  = gen;
                    _db.UpdateJoueur(_joueurEnEdition);
                    // Les buts sont mis à jour séparément car ils ont leur propre colonne BDD
                    if (int.TryParse(TbButs.Text, out int buts) && buts >= 0)
                        _db.UpdateButsMarques(_joueurEnEdition.Id, buts);
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
        private void Retour_Click(object s, RoutedEventArgs e)        => Retour?.Invoke(this, EventArgs.Empty);
    }
}
