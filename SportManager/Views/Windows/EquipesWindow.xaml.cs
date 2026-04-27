using SportManager.Models;
using SportManager.Services;
using System.Collections.Generic;
using System.Linq;

namespace SportManager.Views.Windows
{
    public partial class EquipesWindow : UserControl
    {
        /// <summary>Déclenché quand l'utilisateur clique sur "Retour".</summary>
        public event EventHandler? Retour;

        private readonly DatabaseService _db = new();
        private List<Equipe> _all = new();      // cache local des équipes
        private bool _modeCreation;             // true = création, false = modification
        private Equipe? _equipeEnEdition;       // équipe en cours de modification

        public EquipesWindow()
        {
            InitializeComponent();
            // Recharge à chaque fois que la vue devient visible
            IsVisibleChanged += (_, e) => { if ((bool)e.NewValue) Load(); };
        }

        /// <summary>Charge toutes les équipes (avec leurs joueurs) et remet le DataGrid à jour.</summary>
        private void Load()
        {
            try
            {
                _all = _db.GetAllEquipes();
                GridEquipes.ItemsSource = _all;
                GridCompo.ItemsSource   = null;       // efface la composition affichée
                BtnModifier.IsEnabled  = false;
                BtnSupprimer.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Equipe? Selected => GridEquipes.SelectedItem as Equipe;

        /// <summary>
        /// Met à jour les boutons et affiche la composition de l'équipe sélectionnée dans GridCompo.
        /// </summary>
        private void GridEquipes_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            bool has = Selected != null;
            BtnModifier.IsEnabled  = has;
            BtnSupprimer.IsEnabled = has;
            // Affiche les joueurs de l'équipe sélectionnée dans le panneau inférieur
            GridCompo.ItemsSource  = has ? Selected!.Joueurs : null;
        }

        // ── Panneau form (droite) ──────────────────────────────

        /// <summary>
        /// Remplit les ComboBox du formulaire avec les joueurs disponibles, filtrés par poste.
        /// Chaque slot de l'équipe n'accepte que les joueurs du bon poste.
        /// </summary>
        private void LoadJoueursInForm()
        {
            var poursuiveurs = _db.GetJoueursByAffectation("Poursuiveur");
            var batteurs     = _db.GetJoueursByAffectation("Batteur");
            var gardiens     = _db.GetJoueursByAffectation("Gardien");
            var attrapeurs   = _db.GetJoueursByAffectation("Attrapeur");

            // Les 3 ComboBox Poursuiveur partagent la même source (même liste d'objets en mémoire)
            CbP1.ItemsSource = CbP2.ItemsSource = CbP3.ItemsSource = poursuiveurs;
            CbB1.ItemsSource = CbB2.ItemsSource = batteurs;
            CbG.ItemsSource  = gardiens;
            CbA.ItemsSource  = attrapeurs;
        }

        /// <summary>Initialise le formulaire pour la création d'une nouvelle équipe.</summary>
        private void ShowFormCreate()
        {
            try
            {
                _modeCreation    = true;
                _equipeEnEdition = null;
                TbFormTitle.Text = "NOUVELLE ÉQUIPE";
                TbNom.Text       = string.Empty;
                LoadJoueursInForm();
                // Désélectionne tous les ComboBox
                CbP1.SelectedIndex = CbP2.SelectedIndex = CbP3.SelectedIndex = -1;
                CbB1.SelectedIndex = CbB2.SelectedIndex = -1;
                CbG.SelectedIndex  = CbA.SelectedIndex  = -1;
                FormPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Pré-sélectionne les joueurs actuels de l'équipe dans les ComboBox du formulaire.
        /// Utilise un lambda `find` pour retrouver le bon objet Joueur dans chaque ItemsSource.
        /// </summary>
        private void ShowFormEdit(Equipe eq)
        {
            try
            {
                _modeCreation    = false;
                _equipeEnEdition = eq;
                TbFormTitle.Text = "MODIFIER L'ÉQUIPE";
                TbNom.Text       = eq.Nom;
                LoadJoueursInForm();

                // Sépare les joueurs de l'équipe par poste
                var p = eq.Joueurs.Where(j => j.Affectation == "Poursuiveur").ToList();
                var b = eq.Joueurs.Where(j => j.Affectation == "Batteur").ToList();
                var g = eq.Joueurs.Where(j => j.Affectation == "Gardien").FirstOrDefault();
                var a = eq.Joueurs.Where(j => j.Affectation == "Attrapeur").FirstOrDefault();

                // Cherche l'objet Joueur dans ItemsSource par son Id pour pouvoir le sélectionner
                Func<System.Collections.IEnumerable, int, Joueur?> find = (src, id) =>
                    src.Cast<Joueur>().FirstOrDefault(x => x.Id == id);

                if (p.Count > 0) CbP1.SelectedItem = find(CbP1.ItemsSource, p[0].Id);
                if (p.Count > 1) CbP2.SelectedItem = find(CbP2.ItemsSource, p[1].Id);
                if (p.Count > 2) CbP3.SelectedItem = find(CbP3.ItemsSource, p[2].Id);
                if (b.Count > 0) CbB1.SelectedItem = find(CbB1.ItemsSource, b[0].Id);
                if (b.Count > 1) CbB2.SelectedItem = find(CbB2.ItemsSource, b[1].Id);
                if (g != null)   CbG.SelectedItem  = find(CbG.ItemsSource, g.Id);
                if (a != null)   CbA.SelectedItem  = find(CbA.ItemsSource, a.Id);

                FormPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            _equipeEnEdition = null;
        }

        // ── Boutons footer ────────────────────────────────────

        private void Ajouter_Click(object s, RoutedEventArgs e)  => ShowFormCreate();
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
            try { _db.DeleteEquipe(Selected.Id); HideForm(); Load(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Boutons panneau form ──────────────────────────────

        /// <summary>
        /// Valide et enregistre l'équipe.
        /// Vérifie : nom non vide, 7 joueurs sélectionnés, pas de doublon.
        /// </summary>
        private void Enregistrer_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNom.Text))
            { MessageBox.Show("Le nom est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            // Rassemble les 7 sélections dans l'ordre : P1,P2,P3,B1,B2,G,A
            var selections = new[]
            {
                CbP1.SelectedItem as Joueur, CbP2.SelectedItem as Joueur, CbP3.SelectedItem as Joueur,
                CbB1.SelectedItem as Joueur, CbB2.SelectedItem as Joueur,
                CbG.SelectedItem  as Joueur,
                CbA.SelectedItem  as Joueur
            };

            if (selections.Any(j => j == null))
            { MessageBox.Show("Veuillez sélectionner les 7 joueurs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var ids = selections.Select(j => j!.Id).ToArray();
            // Vérifie qu'aucun joueur n'est sélectionné deux fois (un joueur = un seul poste)
            if (ids.Distinct().Count() != 7)
            { MessageBox.Show("Un même joueur ne peut pas occuper deux postes.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                if (_modeCreation)
                {
                    _db.CreateEquipe(new Equipe { Nom = TbNom.Text.Trim(), JoueurIds = ids });
                }
                else if (_equipeEnEdition != null)
                {
                    _equipeEnEdition.Nom       = TbNom.Text.Trim();
                    _equipeEnEdition.JoueurIds = ids;
                    _db.UpdateEquipe(_equipeEnEdition);
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
