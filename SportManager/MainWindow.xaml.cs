using SportManager.Services;

namespace SportManager
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db = new();

        public MainWindow()
        {
            InitializeComponent();
            RefreshStats();
            LoadLastMatch();
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

        // Mini-cartes panneau droit
        private void OpenJoueurs_Click2(object sender, MouseButtonEventArgs e)
        {
            new JoueursWindow().ShowDialog();
            RefreshStats();
        }

        private void OpenEquipes_Click2(object sender, MouseButtonEventArgs e)
        {
            new EquipesWindow().ShowDialog();
            RefreshStats();
        }

        private void OpenMatch_Click2(object sender, MouseButtonEventArgs e)
        {
            new MatchWindow().ShowDialog();
            RefreshStats();
            LoadLastMatch();
        }
    }
}
