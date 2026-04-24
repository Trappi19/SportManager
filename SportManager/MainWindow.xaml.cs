using SportManager.Services;

namespace SportManager
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db = new();

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
            const string path = @"c:\Users\Utilisateur\Desktop\CESI 2025-2026\SportManager\SportManager\Resources\background.png";
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
            new JoueursWindow().ShowDialog();
            RefreshStats();
        }

        private void OpenEquipes_Click(object sender, RoutedEventArgs e)
        {
            new EquipesWindow().ShowDialog();
            RefreshStats();
        }

        private void OpenMatch_Click(object sender, RoutedEventArgs e)
        {
            new MatchWindow().ShowDialog();
            RefreshStats();
            LoadLastMatch();
        }

        private void Quitter_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

    }
}
