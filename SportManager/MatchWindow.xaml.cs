using SportManager.Models;
using SportManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SportManager
{
    public partial class MatchWindow : Window
    {
        private readonly DatabaseService _db = new();
        private readonly Random _rnd = new();

        // Résultat du dernier match simulé (pour le sauvegarder)
        private int _idEq1, _idEq2, _scoreFinal1, _scoreFinal2;
        private bool _matchSimule;

        public MatchWindow()
        {
            InitializeComponent();
            LoadEquipes();
        }

        private void LoadEquipes()
        {
            try
            {
                var equipes = _db.GetAllEquipes();
                CbEq1.ItemsSource = equipes;
                CbEq2.ItemsSource = equipes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement des équipes : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────── SÉLECTION ÉQUIPES ──────────────────────

        private void Equipe_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            var eq1 = CbEq1.SelectedItem as Equipe;
            var eq2 = CbEq2.SelectedItem as Equipe;

            AfficherInfoEquipe(eq1, PanelEq1, TbEq1Score, GridEq1);
            AfficherInfoEquipe(eq2, PanelEq2, TbEq2Score, GridEq2);

            BtnSimuler.IsEnabled = eq1 != null && eq2 != null && eq1.Id != eq2.Id;
            PanelResultats.Visibility = Visibility.Collapsed;
            _matchSimule = false;
        }

        private void AfficherInfoEquipe(Equipe? eq, Border panel, TextBlock scoreLabel, DataGrid grid)
        {
            if (eq == null) { panel.Visibility = Visibility.Collapsed; return; }
            int scoreEffectif = _db.CalcScoreAvecBlessures(eq.JoueurIds);
            scoreLabel.Text    = $"Score effectif (avec blessures) : {scoreEffectif} / 10";
            grid.ItemsSource   = eq.Joueurs;
            panel.Visibility   = Visibility.Visible;
        }

        // ─────────────────────── SIMULATION ─────────────────────────────

        private void Simuler_Click(object s, RoutedEventArgs e)
        {
            try { SimulerInternal(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la simulation : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimulerInternal()
        {
            var eq1 = (Equipe)CbEq1.SelectedItem;
            var eq2 = (Equipe)CbEq2.SelectedItem;

            int score1 = _db.CalcScoreAvecBlessures(eq1.JoueurIds);
            int score2 = _db.CalcScoreAvecBlessures(eq2.JoueurIds);

            // ── Simulation intelligente ──
            int s1MT1 = SimulerButs(score1, score2);
            int s2MT1 = SimulerButs(score2, score1);
            int s1MT2 = SimulerButs(score1, score2);
            int s2MT2 = SimulerButs(score2, score1);

            _scoreFinal1 = s1MT1 + s1MT2;
            _scoreFinal2 = s2MT1 + s2MT2;
            _idEq1 = eq1.Id;
            _idEq2 = eq2.Id;

            // ── Attribution des buts aux poursuiveurs ──
            var buts = new List<ButInfo>();
            AttribuerButs(buts, s1MT1, eq1.Id, _db.GetPoursuiveurs(eq1.JoueurIds), 1);
            AttribuerButs(buts, s2MT1, eq2.Id, _db.GetPoursuiveurs(eq2.JoueurIds), 1);
            AttribuerButs(buts, s1MT2, eq1.Id, _db.GetPoursuiveurs(eq1.JoueurIds), 2);
            AttribuerButs(buts, s2MT2, eq2.Id, _db.GetPoursuiveurs(eq2.JoueurIds), 2);

            // ── Blessures ──
            var notifsBlessures = _db.GererBlessures(eq1.JoueurIds);
            notifsBlessures.AddRange(_db.GererBlessures(eq2.JoueurIds));

            // ── Affichage résultats ──
            TbScoreFinal.Text = $"{_scoreFinal1}  —  {_scoreFinal2}";
            TbMT1.Text = $"1ère MT : {s1MT1} - {s2MT1}";
            TbMT2.Text = $"2ème MT : {s1MT2} - {s2MT2}";

            string vainqueur = _scoreFinal1 > _scoreFinal2 ? eq1.Nom
                             : _scoreFinal2 > _scoreFinal1 ? eq2.Nom
                             : "Match nul";
            TbVainqueur.Text = vainqueur;

            TbButeurs.Text = BuildButeursText(buts, eq1, eq2);

            if (notifsBlessures.Count > 0)
            {
                TbBlessures.Text = string.Join("\n", notifsBlessures);
                TbBlessures.Visibility        = Visibility.Visible;
                LblBlessuresHeader.Visibility = Visibility.Visible;
            }
            else
            {
                TbBlessures.Visibility        = Visibility.Collapsed;
                LblBlessuresHeader.Visibility = Visibility.Collapsed;
            }

            PanelResultats.Visibility = Visibility.Visible;
            _matchSimule = true;
        }

        // Simulation intelligente : meilleure équipe marque plus
        private int SimulerButs(int scoreEquipe, int scoreAdverse)
        {
            double total  = scoreEquipe + scoreAdverse;
            double ratio  = total > 0 ? scoreEquipe / total : 0.5;
            int base1     = _rnd.Next(0, 6);
            int buts      = (int)Math.Round(base1 * ratio * 2 + _rnd.Next(-1, 2));
            return Math.Max(0, Math.Min(10, buts));
        }

        private void AttribuerButs(List<ButInfo> buts, int nbButs, int idEquipe,
                                   List<int> poursuiveurs, int miTemps)
        {
            if (nbButs == 0 || poursuiveurs.Count == 0) return;
            for (int i = 0; i < nbButs; i++)
            {
                int idJ = poursuiveurs[_rnd.Next(poursuiveurs.Count)];
                buts.Add(new ButInfo
                {
                    IdJoueur   = idJ,
                    NomJoueur  = _db.GetNomJoueur(idJ),
                    IdEquipe   = idEquipe,
                    NumMiTemps = miTemps,
                });
            }
        }

        private static string BuildButeursText(List<ButInfo> buts, Equipe eq1, Equipe eq2)
        {
            var sb = new StringBuilder();
            foreach (var eq in new[] { eq1, eq2 })
            {
                sb.AppendLine($"▶ {eq.Nom}");
                var grouped = buts
                    .Where(b => b.IdEquipe == eq.Id)
                    .GroupBy(b => (b.IdJoueur, b.NomJoueur, b.NumMiTemps))
                    .Select(g => (g.Key.NomJoueur, g.Key.NumMiTemps, Count: g.Count()));

                foreach (var (nom, mt, count) in grouped.OrderBy(x => x.NumMiTemps))
                    sb.AppendLine($"   {nom}  ×{count}  ({(mt == 1 ? "1ère" : "2ème")} MT)");

                if (!buts.Any(b => b.IdEquipe == eq.Id))
                    sb.AppendLine("   Aucun but marqué.");

                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        // ─────────────────────── SAUVEGARDE ─────────────────────────────

        private void Sauver_Click(object s, RoutedEventArgs e)
        {
            if (!_matchSimule) return;
            try
            {
                _db.SaveMatch(_idEq1, _idEq2, _scoreFinal1, _scoreFinal2);
                MessageBox.Show("Match enregistré !", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _matchSimule = false;
                BtnSauver.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────── HISTORIQUE ─────────────────────────────

        private void HistoriqueTab_GotFocus(object s, RoutedEventArgs e)
        {
            try { GridHistorique.ItemsSource = _db.GetAllMatchs(); }
            catch { /* Silencieux au chargement initial */ }
        }

        private void ActualiserHisto_Click(object s, RoutedEventArgs e)
        {
            try { GridHistorique.ItemsSource = _db.GetAllMatchs(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Retour_Click(object s, RoutedEventArgs e) => Close();
    }
}
