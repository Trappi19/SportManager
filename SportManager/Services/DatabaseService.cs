using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
using SportManager.Models;

namespace SportManager.Services
{
    public class DatabaseService
    {
        private const string ConnStr =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        private MySqlConnection Open()
        {
            var c = new MySqlConnection(ConnStr);
            c.Open();
            return c;
        }

        // ─────────────────────────── MIGRATIONS ─────────────────────────

        public void MigrateRandomStats()
        {
            using var conn = Open();
            conn.Execute("CREATE TABLE IF NOT EXISTS migrations (nom VARCHAR(100) PRIMARY KEY);");

            if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM migrations WHERE nom='random_stats_v1';") > 0)
                return;

            string[] sqls =
            {
                @"UPDATE joueurs SET
                    score_defense   = FLOOR(20 + RAND() * 45),
                    score_attaque   = FLOOR(55 + RAND() * 37),
                    score_vitesse   = FLOOR(45 + RAND() * 43),
                    score_endurance = FLOOR(30 + RAND() * 40)
                  WHERE affectation_joueur = 'Poursuiveur';",

                @"UPDATE joueurs SET
                    score_defense   = FLOOR(55 + RAND() * 37),
                    score_attaque   = FLOOR(30 + RAND() * 40),
                    score_vitesse   = FLOOR(25 + RAND() * 40),
                    score_endurance = FLOOR(45 + RAND() * 40)
                  WHERE affectation_joueur = 'Batteur';",

                @"UPDATE joueurs SET
                    score_defense   = FLOOR(65 + RAND() * 30),
                    score_attaque   = FLOOR(20 + RAND() * 35),
                    score_vitesse   = FLOOR(25 + RAND() * 35),
                    score_endurance = FLOOR(55 + RAND() * 35)
                  WHERE affectation_joueur = 'Gardien';",

                @"UPDATE joueurs SET
                    score_defense   = FLOOR(25 + RAND() * 35),
                    score_attaque   = FLOOR(30 + RAND() * 35),
                    score_vitesse   = FLOOR(72 + RAND() * 25),
                    score_endurance = FLOOR(45 + RAND() * 35)
                  WHERE affectation_joueur = 'Attrapeur';"
            };

            foreach (var sql in sqls)
                conn.Execute(sql);

            conn.Execute("UPDATE joueurs SET score_general = (score_defense + score_attaque + score_vitesse + score_endurance) / 4;");
            conn.Execute("INSERT INTO migrations (nom) VALUES ('random_stats_v1');");
        }

        public void MigrateToV2()
        {
            using var conn = Open();
            bool exists = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = 'joueurs'
                  AND COLUMN_NAME  = 'score_endurance';") > 0;

            if (!exists)
            {
                conn.Execute("ALTER TABLE joueurs ADD COLUMN score_endurance INT NOT NULL DEFAULT 50;");
                conn.Execute(@"UPDATE joueurs SET
                    score_defense   = score_defense  * 10,
                    score_attaque   = score_attaque  * 10,
                    score_vitesse   = score_vitesse  * 10,
                    score_endurance = 50,
                    score_general   = score_general  * 10;");
            }
        }

        // ─────────────────────────── JOUEURS ────────────────────────────

        // Requête de base réutilisée partout — les alias correspondent aux propriétés de Joueur
        private const string JoueurSql = @"
            SELECT j.id_joueur                  AS Id,
                   j.nom_joueur                 AS Nom,
                   j.score_defense              AS ScoreDefense,
                   j.score_attaque              AS ScoreAttaque,
                   j.score_vitesse              AS ScoreVitesse,
                   j.score_endurance            AS ScoreEndurance,
                   j.score_general              AS ScoreGeneral,
                   j.score_goal                 AS ButsMarques,
                   j.affectation_joueur         AS Affectation,
                   j.id_blessure                AS IdBlessure,
                   j.matchs_restants_blessure   AS MatchsRestantsBlessure,
                   b.type_blessure              AS TypeBlessure,
                   b.pénalité                   AS Penalite
            FROM joueurs j
            LEFT JOIN blessures b ON j.id_blessure = b.id_blessure";

        public List<Joueur> GetAllJoueurs()
        {
            using var conn = Open();
            return conn.Query<Joueur>(JoueurSql + " ORDER BY j.nom_joueur;").ToList();
        }

        public List<Joueur> GetJoueursByAffectation(string affectation)
        {
            using var conn = Open();
            return conn.Query<Joueur>(
                JoueurSql + " WHERE j.affectation_joueur = @affectation ORDER BY j.nom_joueur;",
                new { affectation }).ToList();
        }

        public Joueur? GetJoueurById(int id)
        {
            using var conn = Open();
            return conn.QueryFirstOrDefault<Joueur>(
                JoueurSql + " WHERE j.id_joueur = @id;", new { id });
        }

        public void CreateJoueur(Joueur j)
        {
            using var conn = Open();
            conn.Execute(@"
                INSERT INTO joueurs
                    (nom_joueur, score_defense, score_attaque, score_vitesse,
                     score_endurance, score_general, affectation_joueur)
                VALUES (@Nom, @ScoreDefense, @ScoreAttaque, @ScoreVitesse,
                        @ScoreEndurance, @ScoreGeneral, @Affectation);", j);
        }

        public void UpdateJoueur(Joueur j)
        {
            using var conn = Open();
            conn.Execute(@"
                UPDATE joueurs
                SET nom_joueur        = @Nom,
                    score_defense     = @ScoreDefense,
                    score_attaque     = @ScoreAttaque,
                    score_vitesse     = @ScoreVitesse,
                    score_endurance   = @ScoreEndurance,
                    score_general     = @ScoreGeneral,
                    affectation_joueur = @Affectation
                WHERE id_joueur = @Id;", j);
        }

        public bool DeleteJoueur(int id)
        {
            using var conn = Open();
            return conn.Execute("DELETE FROM joueurs WHERE id_joueur = @id;", new { id }) > 0;
        }

        public void UpdateButsMarques(int idJoueur, int buts)
        {
            using var conn = Open();
            conn.Execute("UPDATE joueurs SET score_goal = @buts WHERE id_joueur = @idJoueur;",
                new { buts, idJoueur });
        }

        public void EvaluerJoueur(int idJoueur, int delta)
        {
            if (delta == 0) return;
            using var conn = Open();

            var stats = conn.QueryFirstOrDefault(
                "SELECT score_defense AS D, score_attaque AS A, score_vitesse AS V, score_endurance AS E FROM joueurs WHERE id_joueur = @idJoueur;",
                new { idJoueur });
            if (stats == null) return;

            int def = Math.Clamp((int)stats.D + delta, 0, 100);
            int att = Math.Clamp((int)stats.A + delta, 0, 100);
            int vit = Math.Clamp((int)stats.V + delta, 0, 100);
            int end = Math.Clamp((int)stats.E + delta, 0, 100);
            int gen = (def + att + vit + end) / 4;

            conn.Execute(@"
                UPDATE joueurs
                SET score_defense   = @def,
                    score_attaque   = @att,
                    score_vitesse   = @vit,
                    score_endurance = @end,
                    score_general   = @gen
                WHERE id_joueur = @idJoueur;",
                new { def, att, vit, end, gen, idJoueur });
        }

        public void EnregistrerButs(List<ButInfo> buts)
        {
            if (buts.Count == 0) return;
            using var conn = Open();
            foreach (var groupe in buts.GroupBy(b => b.IdJoueur))
            {
                conn.Execute(
                    "UPDATE joueurs SET score_goal = score_goal + @n WHERE id_joueur = @id;",
                    new { n = groupe.Count(), id = groupe.Key });
            }
        }

        // ─────────────────────────── EQUIPES ────────────────────────────

        public List<Equipe> GetAllEquipes()
        {
            using var conn = Open();
            var rows = conn.Query(@"
                SELECT id_equipe    AS Id,
                       nom_equipe   AS Nom,
                       score_general AS ScoreGeneral,
                       id_joueur1 AS J1, id_joueur2 AS J2, id_joueur3 AS J3,
                       id_joueur4 AS J4, id_joueur5 AS J5, id_joueur6 AS J6, id_joueur7 AS J7
                FROM equipes ORDER BY nom_equipe;").ToList();

            var equipes = rows.Select(r => new Equipe
            {
                Id           = (int)r.Id,
                Nom          = (string)(r.Nom ?? ""),
                ScoreGeneral = (int)(r.ScoreGeneral ?? 0),
                JoueurIds    = new[]
                {
                    (int)(r.J1 ?? 0), (int)(r.J2 ?? 0), (int)(r.J3 ?? 0),
                    (int)(r.J4 ?? 0), (int)(r.J5 ?? 0), (int)(r.J6 ?? 0), (int)(r.J7 ?? 0)
                }
            }).ToList();

            foreach (var e in equipes)
                foreach (int jid in e.JoueurIds.Where(id => id != 0))
                {
                    var j = GetJoueurById(jid);
                    if (j != null) e.Joueurs.Add(j);
                }

            return equipes;
        }

        public void CreateEquipe(Equipe e)
        {
            e.ScoreGeneral = CalcScoreEquipe(e.JoueurIds);
            using var conn = Open();
            conn.Execute(@"
                INSERT INTO equipes
                    (nom_equipe, id_joueur1, id_joueur2, id_joueur3,
                     id_joueur4, id_joueur5, id_joueur6, id_joueur7, score_general)
                VALUES (@Nom, @J1, @J2, @J3, @J4, @J5, @J6, @J7, @ScoreGeneral);",
                new
                {
                    e.Nom, e.ScoreGeneral,
                    J1 = e.JoueurIds[0], J2 = e.JoueurIds[1], J3 = e.JoueurIds[2],
                    J4 = e.JoueurIds[3], J5 = e.JoueurIds[4], J6 = e.JoueurIds[5], J7 = e.JoueurIds[6]
                });
        }

        public void UpdateEquipe(Equipe e)
        {
            e.ScoreGeneral = CalcScoreEquipe(e.JoueurIds);
            using var conn = Open();
            conn.Execute(@"
                UPDATE equipes SET
                    nom_equipe    = @Nom,
                    id_joueur1 = @J1, id_joueur2 = @J2, id_joueur3 = @J3,
                    id_joueur4 = @J4, id_joueur5 = @J5, id_joueur6 = @J6, id_joueur7 = @J7,
                    score_general = @ScoreGeneral
                WHERE id_equipe = @Id;",
                new
                {
                    e.Id, e.Nom, e.ScoreGeneral,
                    J1 = e.JoueurIds[0], J2 = e.JoueurIds[1], J3 = e.JoueurIds[2],
                    J4 = e.JoueurIds[3], J5 = e.JoueurIds[4], J6 = e.JoueurIds[5], J7 = e.JoueurIds[6]
                });
        }

        public bool DeleteEquipe(int id)
        {
            using var conn = Open();
            return conn.Execute("DELETE FROM equipes WHERE id_equipe = @id;", new { id }) > 0;
        }

        // ─────────────────────────── BLESSURES ──────────────────────────

        public List<Blessure> GetAllBlessures()
        {
            using var conn = Open();
            return conn.Query<Blessure>(@"
                SELECT id_blessure  AS Id,
                       type_blessure AS Type,
                       pénalité     AS Penalite
                FROM blessures;").ToList();
        }

        // ─────────────────────────── MATCHS ─────────────────────────────

        public List<MatchResult> GetAllMatchs()
        {
            using var conn = Open();
            return conn.Query<MatchResult>(@"
                SELECT m.id_match                AS Id,
                       m.id_equipe1              AS IdEquipe1,
                       m.id_equipe2              AS IdEquipe2,
                       e1.nom_equipe             AS NomEquipe1,
                       e2.nom_equipe             AS NomEquipe2,
                       m.score_equipe1           AS ScoreEquipe1,
                       m.score_equipe2           AS ScoreEquipe2,
                       m.date_match              AS DateMatch
                FROM matchs m
                JOIN equipes e1 ON m.id_equipe1 = e1.id_equipe
                JOIN equipes e2 ON m.id_equipe2 = e2.id_equipe
                ORDER BY m.date_match DESC;").ToList();
        }

        public void SaveMatch(int idEq1, int idEq2, int s1, int s2)
        {
            using var conn = Open();
            conn.Execute(@"
                INSERT INTO matchs (score_equipe1, score_equipe2, id_equipe1, id_equipe2, date_match)
                VALUES (@s1, @s2, @idEq1, @idEq2, NOW());",
                new { s1, s2, idEq1, idEq2 });
        }

        // ─────────────────────────── SIMULATION ─────────────────────────

        public int[] GetJoueursEquipe(int idEquipe)
        {
            using var conn = Open();
            var row = conn.QueryFirstOrDefault(@"
                SELECT id_joueur1,id_joueur2,id_joueur3,
                       id_joueur4,id_joueur5,id_joueur6,id_joueur7
                FROM equipes WHERE id_equipe = @idEquipe;", new { idEquipe });
            if (row == null) return Array.Empty<int>();
            return new[]
            {
                (int)(row.id_joueur1 ?? 0), (int)(row.id_joueur2 ?? 0), (int)(row.id_joueur3 ?? 0),
                (int)(row.id_joueur4 ?? 0), (int)(row.id_joueur5 ?? 0), (int)(row.id_joueur6 ?? 0),
                (int)(row.id_joueur7 ?? 0)
            };
        }

        // Score pondéré par poste (0-100) avec malus blessure — une seule requête IN pour tous les joueurs
        public int CalcScoreAvecBlessures(int[] ids)
        {
            var validIds = ids.Where(id => id != 0).ToList();
            if (validIds.Count == 0) return 0;

            using var conn = Open();
            var rows = conn.Query(@"
                SELECT j.score_defense       AS Def,
                       j.score_attaque       AS Att,
                       j.score_vitesse       AS Vit,
                       j.score_endurance     AS End_,
                       j.affectation_joueur  AS Poste,
                       j.id_blessure         AS IdBlessure,
                       b.pénalité            AS Penalite
                FROM joueurs j
                LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
                WHERE j.id_joueur IN @validIds;", new { validIds }).ToList();

            double somme = 0;
            foreach (var r in rows)
            {
                int def   = (int)(r.Def   ?? 0);
                int att   = (int)(r.Att   ?? 0);
                int vit   = (int)(r.Vit   ?? 0);
                int end_  = (int)(r.End_  ?? 0);
                string poste = (string)(r.Poste ?? "");

                double score = poste switch
                {
                    "Gardien"     => def * 0.50 + end_ * 0.30 + vit * 0.10 + att  * 0.10,
                    "Poursuiveur" => att * 0.40 + vit  * 0.30 + def * 0.20 + end_ * 0.10,
                    "Batteur"     => end_ * 0.40 + def * 0.30 + att * 0.20 + vit  * 0.10,
                    "Attrapeur"   => vit  * 0.50 + end_ * 0.30 + att * 0.10 + def * 0.10,
                    _             => (def + att + vit + end_) / 4.0,
                };

                if (r.IdBlessure != null && r.Penalite != null)
                    score = Math.Max(0, score + (int)r.Penalite);

                somme += Math.Clamp(score, 0, 100);
            }
            return rows.Count > 0 ? (int)Math.Round(somme / rows.Count) : 0;
        }

        public List<int> GetPoursuiveurs(int[] ids)
        {
            var validIds = ids.Where(id => id != 0).ToList();
            if (validIds.Count == 0) return new List<int>();
            using var conn = Open();
            return conn.Query<int>(@"
                SELECT id_joueur FROM joueurs
                WHERE id_joueur IN @validIds AND affectation_joueur = 'Poursuiveur';",
                new { validIds }).ToList();
        }

        public string GetNomJoueur(int id)
        {
            using var conn = Open();
            return conn.ExecuteScalar<string>("SELECT nom_joueur FROM joueurs WHERE id_joueur = @id;", new { id })
                   ?? $"Joueur {id}";
        }

        public List<string> GererBlessures(int[] ids)
        {
            var notifs   = new List<string>();
            var blessures = GetAllBlessures();
            var rnd       = new Random();
            var validIds  = ids.Where(id => id != 0).ToList();
            if (validIds.Count == 0) return notifs;

            using var conn = Open();
            var etats = conn.Query(@"
                SELECT id_joueur AS Id, id_blessure AS IdBlessure, matchs_restants_blessure AS Restant
                FROM joueurs WHERE id_joueur IN @validIds;",
                new { validIds }).ToList();

            foreach (var etat in etats)
            {
                int idJoueur   = (int)etat.Id;
                int idBlessure = etat.IdBlessure != null ? (int)etat.IdBlessure : 0;
                int restant    = etat.Restant    != null ? (int)etat.Restant    : 0;

                if (idBlessure != 0)
                {
                    restant--;
                    if (restant <= 0)
                        conn.Execute("UPDATE joueurs SET id_blessure=NULL, matchs_restants_blessure=0 WHERE id_joueur=@idJoueur;", new { idJoueur });
                    else
                        conn.Execute("UPDATE joueurs SET matchs_restants_blessure=@restant WHERE id_joueur=@idJoueur;", new { restant, idJoueur });
                }

                if (blessures.Count > 0 && rnd.Next(100) < 10)
                {
                    var b = blessures[rnd.Next(blessures.Count)];
                    conn.Execute("UPDATE joueurs SET id_blessure=@bId, matchs_restants_blessure=3 WHERE id_joueur=@idJoueur;",
                        new { bId = b.Id, idJoueur });
                    notifs.Add($"{GetNomJoueur(idJoueur)} → blessure : {b.Type}");
                }
            }
            return notifs;
        }

        // ─────────────────────────── PRIVÉ ──────────────────────────────

        private int CalcScoreEquipe(int[] ids)
        {
            var validIds = ids.Where(id => id != 0).ToList();
            if (validIds.Count == 0) return 0;
            using var conn = Open();
            var scores = conn.Query<int>(
                "SELECT score_general FROM joueurs WHERE id_joueur IN @validIds;",
                new { validIds }).ToList();
            return scores.Count > 0 ? (int)scores.Average() : 0;
        }
    }
}
