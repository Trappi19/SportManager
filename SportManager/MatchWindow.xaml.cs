using SportManager.Models;
using SportManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace SportManager
{
    public partial class MatchWindow : UserControl
    {
        public event EventHandler? Retour;

        private readonly DatabaseService _db  = new();
        private readonly Random          _rnd = new();

        private static readonly string[] _notes = { "Excellent", "Bon", "Insuffisant" };

        private int  _idEq1, _idEq2, _scoreFinal1, _scoreFinal2;
        private bool _matchSimule;
        private bool _modeManuel;
        private List<ButInfo>    _buts       = new();
        private List<EvalJoueur> _evalJoueurs = new();

        public MatchWindow()
        {
            InitializeComponent();
            ((DataGridComboBoxColumn)ColNote).ItemsSource = _notes;
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

        // ─────────────────── MODE ───────────────────────────────

        private void ModeSimulation_Click(object s, RoutedEventArgs e)
        {
            _modeManuel = false;
            BtnModeSimulation.Style = (Style)FindResource("BtnPrimary");
            BtnModeSaisie.Style     = (Style)FindResource("BtnSecondary");
            BtnSimuler.Visibility          = Visibility.Visible;
            PanelSaisieManuelle.Visibility = Visibility.Collapsed;
            PanelResultats.Visibility      = Visibility.Collapsed;
            PanelEvaluation.Visibility     = Visibility.Collapsed;
        }

        private void ModeSaisie_Click(object s, RoutedEventArgs e)
        {
            _modeManuel = true;
            BtnModeSimulation.Style = (Style)FindResource("BtnSecondary");
            BtnModeSaisie.Style     = (Style)FindResource("BtnPrimary");
            BtnSimuler.Visibility          = Visibility.Collapsed;
            PanelResultats.Visibility      = Visibility.Collapsed;
            PanelEvaluation.Visibility     = Visibility.Collapsed;

            var eq1 = CbEq1.SelectedItem as Equipe;
            var eq2 = CbEq2.SelectedItem as Equipe;
            if (eq1 != null && eq2 != null && eq1.Id != eq2.Id)
            {
                TbManuelNom1.Text = eq1.Nom;
                TbManuelNom2.Text = eq2.Nom;
                PanelSaisieManuelle.Visibility = Visibility.Visible;
            }
        }

        // ─────────────────── SÉLECTION ÉQUIPES ─────────────────

        private void Equipe_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            var eq1 = CbEq1.SelectedItem as Equipe;
            var eq2 = CbEq2.SelectedItem as Equipe;

            AfficherInfoEquipe(eq1, PanelEq1, TbEq1Score, GridEq1);
            AfficherInfoEquipe(eq2, PanelEq2, TbEq2Score, GridEq2);

            bool ready = eq1 != null && eq2 != null && eq1.Id != eq2.Id;
            BtnSimuler.IsEnabled = ready && !_modeManuel;

            if (ready)
            {
                AfficherProbabilites(eq1!, eq2!);
            }
            else
            {
                PanelProba.Visibility = Visibility.Collapsed;
            }

            if (_modeManuel && ready)
            {
                TbManuelNom1.Text = eq1!.Nom;
                TbManuelNom2.Text = eq2!.Nom;
                PanelSaisieManuelle.Visibility = Visibility.Visible;
            }
            else if (!ready)
            {
                PanelSaisieManuelle.Visibility = Visibility.Collapsed;
            }

            PanelResultats.Visibility  = Visibility.Collapsed;
            PanelEvaluation.Visibility = Visibility.Collapsed;
            _matchSimule = false;
        }

        private void AfficherInfoEquipe(Equipe? eq, Border panel, TextBlock scoreLabel, DataGrid grid)
        {
            if (eq == null) { panel.Visibility = Visibility.Collapsed; return; }
            int scoreEffectif = _db.CalcScoreAvecBlessures(eq.JoueurIds);
            scoreLabel.Text  = $"Score effectif (avec blessures) : {scoreEffectif} / 100";
            grid.ItemsSource = eq.Joueurs;
            panel.Visibility = Visibility.Visible;
        }

        private void AfficherProbabilites(Equipe eq1, Equipe eq2)
        {
            int s1 = _db.CalcScoreAvecBlessures(eq1.JoueurIds);
            int s2 = _db.CalcScoreAvecBlessures(eq2.JoueurIds);
            double total = s1 + s2;
            int p1 = total > 0 ? (int)Math.Round(s1 / total * 100) : 50;
            int p2 = 100 - p1;

            TbProba1NomEquipe.Text = eq1.Nom.ToUpper();
            TbProba2NomEquipe.Text = eq2.Nom.ToUpper();
            TbProba1.Text = $"{p1}%";
            TbProba2.Text = $"{p2}%";
            PanelProba.Visibility = Visibility.Visible;
        }

        // ─────────────────── SIMULATION ────────────────────────

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

            int s1MT1 = SimulerButs(score1, score2);
            int s2MT1 = SimulerButs(score2, score1);
            int s1MT2 = SimulerButs(score1, score2);
            int s2MT2 = SimulerButs(score2, score1);

            _scoreFinal1 = s1MT1 + s1MT2;
            _scoreFinal2 = s2MT1 + s2MT2;
            _idEq1 = eq1.Id;
            _idEq2 = eq2.Id;

            _buts = new List<ButInfo>();
            AttribuerButs(_buts, s1MT1, eq1.Id, _db.GetPoursuiveurs(eq1.JoueurIds), 1);
            AttribuerButs(_buts, s2MT1, eq2.Id, _db.GetPoursuiveurs(eq2.JoueurIds), 1);
            AttribuerButs(_buts, s1MT2, eq1.Id, _db.GetPoursuiveurs(eq1.JoueurIds), 2);
            AttribuerButs(_buts, s2MT2, eq2.Id, _db.GetPoursuiveurs(eq2.JoueurIds), 2);

            var notifsBlessures = _db.GererBlessures(eq1.JoueurIds);
            notifsBlessures.AddRange(_db.GererBlessures(eq2.JoueurIds));

            TbScoreFinal.Text = $"{_scoreFinal1}  —  {_scoreFinal2}";
            TbMT1.Text = $"1ère MT : {s1MT1} - {s2MT1}";
            TbMT2.Text = $"2ème MT : {s1MT2} - {s2MT2}";

            string vainqueur = _scoreFinal1 > _scoreFinal2 ? eq1.Nom
                             : _scoreFinal2 > _scoreFinal1 ? eq2.Nom
                             : "Match nul";
            TbVainqueur.Text = vainqueur;
            TbButeurs.Text   = BuildButeursText(_buts, eq1, eq2);

            if (notifsBlessures.Count > 0)
            {
                TbBlessures.Text              = string.Join("\n", notifsBlessures);
                TbBlessures.Visibility        = Visibility.Visible;
                LblBlessuresHeader.Visibility = Visibility.Visible;
            }
            else
            {
                TbBlessures.Visibility        = Visibility.Collapsed;
                LblBlessuresHeader.Visibility = Visibility.Collapsed;
            }

            PanelResultats.Visibility = Visibility.Visible;
            BtnSauver.IsEnabled = true;
            _matchSimule = true;
        }

        private int SimulerButs(int scoreEquipe, int scoreAdverse)
        {
            double total = scoreEquipe + scoreAdverse;
            double ratio = total > 0 ? scoreEquipe / total : 0.5;
            int base1    = _rnd.Next(0, 6);
            int buts     = (int)Math.Round(base1 * ratio * 2 + _rnd.Next(-1, 2));
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

        // ─────────────────── SAUVEGARDE SIMULATION ─────────────

        private void Sauver_Click(object s, RoutedEventArgs e)
        {
            if (!_matchSimule) return;
            try
            {
                _db.SaveMatch(_idEq1, _idEq2, _scoreFinal1, _scoreFinal2);
                _db.EnregistrerButs(_buts);
                _matchSimule    = false;
                BtnSauver.IsEnabled = false;
                PrepareEvaluation();
                MessageBox.Show("Match enregistré. Évaluez maintenant les joueurs.", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────── SAISIE MANUELLE ───────────────────

        private void SauverManuel_Click(object s, RoutedEventArgs e)
        {
            var eq1 = CbEq1.SelectedItem as Equipe;
            var eq2 = CbEq2.SelectedItem as Equipe;
            if (eq1 == null || eq2 == null) return;

            if (!int.TryParse(TbScoreManuel1.Text.Trim(), out int s1) || s1 < 0 ||
                !int.TryParse(TbScoreManuel2.Text.Trim(), out int s2) || s2 < 0)
            {
                MessageBox.Show("Les scores doivent être des nombres entiers positifs.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _idEq1 = eq1.Id; _idEq2 = eq2.Id;
                _scoreFinal1 = s1; _scoreFinal2 = s2;

                _db.SaveMatch(_idEq1, _idEq2, _scoreFinal1, _scoreFinal2);

                var notifs = _db.GererBlessures(eq1.JoueurIds);
                notifs.AddRange(_db.GererBlessures(eq2.JoueurIds));

                string notifTxt = notifs.Count > 0
                    ? "\n\nBlessures : " + string.Join(", ", notifs)
                    : "";

                PanelSaisieManuelle.Visibility = Visibility.Collapsed;
                PrepareEvaluation();
                MessageBox.Show(
                    $"Match enregistré ({eq1.Nom} {s1} — {s2} {eq2.Nom}).{notifTxt}\n\nÉvaluez maintenant les joueurs.",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────── EVALUATION ────────────────────────

        private void PrepareEvaluation()
        {
            var eq1 = CbEq1.SelectedItem as Equipe;
            var eq2 = CbEq2.SelectedItem as Equipe;
            if (eq1 == null || eq2 == null) return;

            _evalJoueurs = eq1.Joueurs
                .Select(j => new EvalJoueur { Id = j.Id, Nom = j.Nom, NomEquipe = eq1.Nom, Note = "Bon" })
                .Concat(eq2.Joueurs
                    .Select(j => new EvalJoueur { Id = j.Id, Nom = j.Nom, NomEquipe = eq2.Nom, Note = "Bon" }))
                .ToList();

            GridEval.ItemsSource       = _evalJoueurs;
            PanelEvaluation.Visibility = Visibility.Visible;
        }

        private void ValiderEval_Click(object s, RoutedEventArgs e)
        {
            GridEval.CommitEdit(DataGridEditingUnit.Row, true);
            try
            {
                foreach (var eval in _evalJoueurs)
                {
                    int delta = eval.Note switch
                    {
                        "Excellent"   => +5,
                        "Insuffisant" => -5,
                        _             =>  0,
                    };
                    _db.EvaluerJoueur(eval.Id, delta);
                }
                PanelEvaluation.Visibility = Visibility.Collapsed;
                TbScoreManuel1.Text = TbScoreManuel2.Text = "";
                MessageBox.Show("Évaluation enregistrée. Les compétences des joueurs ont été mises à jour.", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'évaluation : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────── HISTORIQUE ────────────────────────

        private void HistoriqueTab_GotFocus(object s, RoutedEventArgs e)
        {
            try { GridHistorique.ItemsSource = _db.GetAllMatchs(); }
            catch { }
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

        private void Retour_Click(object s, RoutedEventArgs e) => Retour?.Invoke(this, EventArgs.Empty);
    }
}
