using SportManager.Services;
using SportManager.Views.Windows;

namespace SportManager
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db = new();

        // Les UserControl sont créés une seule fois et réutilisés pour conserver leur état (filtre, sélection, etc.)
        private JoueursWindow? _joueursCtrl;
        private EquipesWindow? _equipesCtrl;
        private MatchWindow?   _matchCtrl;

        public MainWindow()
        {
            InitializeComponent();
            LoadBackground();
            // Migrations au démarrage — les erreurs sont ignorées si la BDD est indisponible
            try { _db.MigrateToV2(); } catch { }
            try { _db.MigrateRandomStats(); } catch { }
            // Affiche les compteurs de stats sur l'écran d'accueil
            RefreshStats();
            LoadLastMatch();
        }

        /// <summary>Charge l'image de fond du panneau droit depuis le disque.</summary>
        private void LoadBackground()
        {
            const string path = @"C:\Mes données personnelles\Mes documents persos\CESI\SportManager\SportManager\Resources\background.png";
            var bi = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
            ImgFond.Source = bi;
        }

        /// <summary>Met à jour les mini-cartes de stats (nb joueurs, équipes, matchs) sur l'accueil.</summary>
        private void RefreshStats()
        {
            try
            {
                StatJoueurs.Text = $"{_db.GetAllJoueurs().Count} joueur(s)";
                StatEquipes.Text = $"{_db.GetAllEquipes().Count} équipe(s)";
                StatMatchs.Text  = $"{_db.GetAllMatchs().Count} match(s) joué(s)";
            }
            catch { /* DB non disponible → on laisse les valeurs précédentes */ }
        }

        /// <summary>Affiche le score du dernier match joué dans la mini-carte d'accueil.</summary>
        private void LoadLastMatch()
        {
            try
            {
                var matchs = _db.GetAllMatchs();
                if (matchs.Count > 0)
                {
                    // matchs est trié DESC par date → [^1] est le plus ancien ; [0] est le plus récent
                    var last = matchs[0];
                    TbLastMatch.Text = $"{last.NomEquipe1} {last.ScoreEquipe1} – {last.ScoreEquipe2} {last.NomEquipe2}";
                    TbLastMatch.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xD0, 0xC0, 0xFF));
                }
            }
            catch { /* DB non disponible */ }
        }

        // ── Boutons du menu gauche ────────────────────────────────

        private void OpenJoueurs_Click(object sender, RoutedEventArgs e)
        {
            // Instanciation paresseuse : crée le contrôle seulement au premier clic
            if (_joueursCtrl == null)
            {
                _joueursCtrl = new JoueursWindow();
                // Abonnement à l'événement Retour pour revenir à l'accueil
                _joueursCtrl.Retour += (_, _) => FermerSection();
            }
            OuvrirSection(_joueursCtrl);
        }

        private void OpenEquipes_Click(object sender, RoutedEventArgs e)
        {
            if (_equipesCtrl == null)
            {
                _equipesCtrl = new EquipesWindow();
                _equipesCtrl.Retour += (_, _) => FermerSection();
            }
            OuvrirSection(_equipesCtrl);
        }

        private void OpenMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_matchCtrl == null)
            {
                _matchCtrl = new MatchWindow();
                _matchCtrl.Retour += (_, _) => FermerSection();
            }
            OuvrirSection(_matchCtrl);
        }

        /// <summary>Affiche un UserControl dans le panneau droit et masque l'écran d'accueil.</summary>
        private void OuvrirSection(UserControl ctrl)
        {
            PanelContenu.Content = ctrl;
            PanelAccueil.Visibility = Visibility.Collapsed;
            PanelContenu.Visibility = Visibility.Visible;
        }

        /// <summary>Ferme la section en cours et revient à l'accueil en rafraîchissant les stats.</summary>
        private void FermerSection()
        {
            PanelContenu.Visibility = Visibility.Collapsed;
            PanelAccueil.Visibility = Visibility.Visible;
            RefreshStats();
            LoadLastMatch();
        }

        private void Quitter_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

    }
}
