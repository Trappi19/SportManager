using SportManager.Services;
using SportManager.Views.Windows;

namespace SportManager
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db = new();

        // Instances conservées pour préserver l'état entre navigations
        private JoueursWindow? _joueursCtrl;
        private EquipesWindow? _equipesCtrl;
        private MatchWindow?   _matchCtrl;

        public MainWindow()
        {
            InitializeComponent();
            LoadBackground();
            try { _db.MigrateToV2(); } catch { }
            try { _db.MigrateRandomStats(); } catch { }
            RefreshStats();
            LoadLastMatch();
        }

        private void LoadBackground()
        {
            const string path = @"C:\Mes données personnelles\Mes documents persos\CESI\SportManager\SportManager\Resources\background.png";
            var bi = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
            ImgFond.Source = bi;
        }

        private void RefreshStats()
        {
            try
            {
                StatJoueurs.Text = $"{_db.GetAllJoueurs().Count} joueur(s)";
                StatEquipes.Text = $"{_db.GetAllEquipes().Count} équipe(s)";
                StatMatchs.Text  = $"{_db.GetAllMatchs().Count} match(s) joué(s)";
            }
            catch { /* DB non disponible */ }
        }

        private void LoadLastMatch()
        {
            try
            {
                var matchs = _db.GetAllMatchs();
                if (matchs.Count > 0)
                {
                    var last = matchs[^1];
                    TbLastMatch.Text = $"{last.NomEquipe1} {last.ScoreEquipe1} – {last.ScoreEquipe2} {last.NomEquipe2}";
                    TbLastMatch.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xD0, 0xC0, 0xFF));
                }
            }
            catch { /* DB non disponible */ }
        }

        // Boutons menu gauche
        private void OpenJoueurs_Click(object sender, RoutedEventArgs e)
        {
            if (_joueursCtrl == null)
            {
                _joueursCtrl = new JoueursWindow();
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

        private void OuvrirSection(UserControl ctrl)
        {
            PanelContenu.Content = ctrl;
            PanelAccueil.Visibility = Visibility.Collapsed;
            PanelContenu.Visibility = Visibility.Visible;
        }

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
