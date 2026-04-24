using MySql.Data.MySqlClient;
using SportManager.Models;
using System;
using System.Collections.Generic;

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

        // Randomise les stats de tous les joueurs selon leur poste (tourne une seule fois)
        public void MigrateRandomStats()
        {
            using var conn = Open();

            using (var cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS migrations (
                    nom VARCHAR(100) PRIMARY KEY
                );", conn))
                cmd.ExecuteNonQuery();

            using (var check = new MySqlCommand(
                "SELECT COUNT(*) FROM migrations WHERE nom = 'random_stats_v1';", conn))
            {
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
            }

            // Poursuiveur : Attaque ≥ 55, Vitesse ≥ 45
            string[] sqls =
            {
                @"UPDATE joueurs SET
                    score_defense   = FLOOR(20 + RAND() * 45),
                    score_attaque   = FLOOR(55 + RAND() * 37),
                    score_vitesse   = FLOOR(45 + RAND() * 43),
                    score_endurance = FLOOR(30 + RAND() * 40)
                  WHERE affectation_joueur = 'Poursuiveur';",

                // Batteur : Défense ≥ 55, Endurance ≥ 45
                @"UPDATE joueurs SET
                    score_defense   = FLOOR(55 + RAND() * 37),
                    score_attaque   = FLOOR(30 + RAND() * 40),
                    score_vitesse   = FLOOR(25 + RAND() * 40),
                    score_endurance = FLOOR(45 + RAND() * 40)
                  WHERE affectation_joueur = 'Batteur';",

                // Gardien : Défense ≥ 65, Endurance ≥ 55
                @"UPDATE joueurs SET
                    score_defense   = FLOOR(65 + RAND() * 30),
                    score_attaque   = FLOOR(20 + RAND() * 35),
                    score_vitesse   = FLOOR(25 + RAND() * 35),
                    score_endurance = FLOOR(55 + RAND() * 35)
                  WHERE affectation_joueur = 'Gardien';",

                // Attrapeur : Vitesse ≥ 72, Endurance ≥ 45
                @"UPDATE joueurs SET
                    score_defense   = FLOOR(25 + RAND() * 35),
                    score_attaque   = FLOOR(30 + RAND() * 35),
                    score_vitesse   = FLOOR(72 + RAND() * 25),
                    score_endurance = FLOOR(45 + RAND() * 35)
                  WHERE affectation_joueur = 'Attrapeur';"
            };

            foreach (var sql in sqls)
            {
                using var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            using (var gen = new MySqlCommand(@"
                UPDATE joueurs
                SET score_general = (score_defense + score_attaque + score_vitesse + score_endurance) / 4;", conn))
                gen.ExecuteNonQuery();

            using (var mark = new MySqlCommand(
                "INSERT INTO migrations (nom) VALUES ('random_stats_v1');", conn))
                mark.ExecuteNonQuery();
        }

        // Ajoute score_endurance si absent et monte les stats de 0-10 à 0-100
        public void MigrateToV2()
        {
            using var conn = Open();
            bool exists;
            using (var cmd = new MySqlCommand(@"
                SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = 'joueurs'
                  AND COLUMN_NAME  = 'score_endurance';", conn))
                exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

            if (!exists)
            {
                using var addCol = new MySqlCommand(
                    "ALTER TABLE joueurs ADD COLUMN score_endurance INT NOT NULL DEFAULT 50;", conn);
                addCol.ExecuteNonQuery();

                using var scale = new MySqlCommand(@"
                    UPDATE joueurs
                    SET score_defense  = score_defense  * 10,
                        score_attaque  = score_attaque  * 10,
                        score_vitesse  = score_vitesse  * 10,
                        score_endurance = 50,
                        score_general  = score_general  * 10;", conn);
                scale.ExecuteNonQuery();
            }
        }

        // ─────────────────────────── JOUEURS ────────────────────────────

        public List<Joueur> GetAllJoueurs()
        {
            var list = new List<Joueur>();
            using var conn = Open();
            const string sql = @"
                SELECT j.id_joueur, j.nom_joueur, j.score_defense, j.score_attaque,
                       j.score_vitesse, j.score_endurance, j.score_general, j.affectation_joueur,
                       j.id_blessure, j.matchs_restants_blessure,
                       j.score_goal AS buts_marques,
                       b.type_blessure, b.pénalité
                FROM joueurs j
                LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
                ORDER BY j.nom_joueur;";
            using var cmd = new MySqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(ReadJoueur(r));
            return list;
        }

        public List<Joueur> GetJoueursByAffectation(string affectation)
        {
            var list = new List<Joueur>();
            using var conn = Open();
            const string sql = @"
                SELECT id_joueur, nom_joueur, score_general, affectation_joueur,
                       0 AS score_defense, 0 AS score_attaque, 0 AS score_vitesse, 0 AS score_endurance,
                       NULL AS id_blessure, 0 AS matchs_restants_blessure,
                       score_goal AS buts_marques,
                       NULL AS type_blessure, NULL AS pénalité
                FROM joueurs
                WHERE affectation_joueur = @a
                ORDER BY nom_joueur;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@a", affectation);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(ReadJoueur(r));
            return list;
        }

        public Joueur? GetJoueurById(int id)
        {
            using var conn = Open();
            const string sql = @"
                SELECT j.id_joueur, j.nom_joueur, j.score_defense, j.score_attaque,
                       j.score_vitesse, j.score_endurance, j.score_general, j.affectation_joueur,
                       j.id_blessure, j.matchs_restants_blessure,
                       j.score_goal AS buts_marques,
                       b.type_blessure, b.pénalité
                FROM joueurs j
                LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
                WHERE j.id_joueur = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadJoueur(r) : null;
        }

        private static Joueur ReadJoueur(MySqlDataReader r)
        {
            static int    SafeInt(MySqlDataReader rd, string col) =>
                rd.IsDBNull(rd.GetOrdinal(col)) ? 0 : rd.GetInt32(col);
            static string SafeStr(MySqlDataReader rd, string col) =>
                rd.IsDBNull(rd.GetOrdinal(col)) ? string.Empty : rd.GetString(col);

            return new Joueur
            {
                Id                     = r.GetInt32("id_joueur"),
                Nom                    = SafeStr(r, "nom_joueur"),
                ScoreDefense           = SafeInt(r, "score_defense"),
                ScoreAttaque           = SafeInt(r, "score_attaque"),
                ScoreVitesse           = SafeInt(r, "score_vitesse"),
                ScoreEndurance         = SafeInt(r, "score_endurance"),
                ScoreGeneral           = SafeInt(r, "score_general"),
                ButsMarques            = SafeInt(r, "buts_marques"),
                Affectation            = SafeStr(r, "affectation_joueur"),
                IdBlessure             = r.IsDBNull(r.GetOrdinal("id_blessure"))   ? null : r.GetInt32("id_blessure"),
                MatchsRestantsBlessure = SafeInt(r, "matchs_restants_blessure"),
                TypeBlessure           = r.IsDBNull(r.GetOrdinal("type_blessure")) ? null : r.GetString("type_blessure"),
                Penalite               = r.IsDBNull(r.GetOrdinal("pénalité"))      ? null : r.GetInt32("pénalité"),
            };
        }

        public void CreateJoueur(Joueur j)
        {
            using var conn = Open();
            const string sql = @"
                INSERT INTO joueurs
                    (nom_joueur, score_defense, score_attaque, score_vitesse, score_endurance, score_general, affectation_joueur)
                VALUES (@nom, @def, @att, @vit, @end, @gen, @affect);";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nom",    j.Nom);
            cmd.Parameters.AddWithValue("@def",    j.ScoreDefense);
            cmd.Parameters.AddWithValue("@att",    j.ScoreAttaque);
            cmd.Parameters.AddWithValue("@vit",    j.ScoreVitesse);
            cmd.Parameters.AddWithValue("@end",    j.ScoreEndurance);
            cmd.Parameters.AddWithValue("@gen",    j.ScoreGeneral);
            cmd.Parameters.AddWithValue("@affect", j.Affectation);
            cmd.ExecuteNonQuery();
        }

        public void UpdateJoueur(Joueur j)
        {
            using var conn = Open();
            const string sql = @"
                UPDATE joueurs
                SET nom_joueur=@nom, score_defense=@def, score_attaque=@att,
                    score_vitesse=@vit, score_endurance=@end, score_general=@gen, affectation_joueur=@affect
                WHERE id_joueur=@id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id",     j.Id);
            cmd.Parameters.AddWithValue("@nom",    j.Nom);
            cmd.Parameters.AddWithValue("@def",    j.ScoreDefense);
            cmd.Parameters.AddWithValue("@att",    j.ScoreAttaque);
            cmd.Parameters.AddWithValue("@vit",    j.ScoreVitesse);
            cmd.Parameters.AddWithValue("@end",    j.ScoreEndurance);
            cmd.Parameters.AddWithValue("@gen",    j.ScoreGeneral);
            cmd.Parameters.AddWithValue("@affect", j.Affectation);
            cmd.ExecuteNonQuery();
        }

        public bool DeleteJoueur(int id)
        {
            using var conn = Open();
            using var cmd = new MySqlCommand("DELETE FROM joueurs WHERE id_joueur=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ─────────────────────────── EQUIPES ────────────────────────────

        public List<Equipe> GetAllEquipes()
        {
            var list = new List<Equipe>();
            using var conn = Open();
            const string sql = @"
                SELECT id_equipe, nom_equipe, score_general,
                       id_joueur1,id_joueur2,id_joueur3,
                       id_joueur4,id_joueur5,id_joueur6,id_joueur7
                FROM equipes ORDER BY nom_equipe;";
            using var cmd = new MySqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                static int SafeCol(MySqlDataReader rd, string col) =>
                    rd.IsDBNull(rd.GetOrdinal(col)) ? 0 : rd.GetInt32(col);
                var e = new Equipe
                {
                    Id           = r.GetInt32("id_equipe"),
                    Nom          = r.IsDBNull(r.GetOrdinal("nom_equipe")) ? string.Empty : r.GetString("nom_equipe"),
                    ScoreGeneral = SafeCol(r, "score_general"),
                    JoueurIds    = new[]
                    {
                        SafeCol(r,"id_joueur1"), SafeCol(r,"id_joueur2"), SafeCol(r,"id_joueur3"),
                        SafeCol(r,"id_joueur4"), SafeCol(r,"id_joueur5"), SafeCol(r,"id_joueur6"),
                        SafeCol(r,"id_joueur7")
                    }
                };
                list.Add(e);
            }
            foreach (var e in list)
                foreach (int jid in e.JoueurIds)
                {
                    var j = GetJoueurById(jid);
                    if (j != null) e.Joueurs.Add(j);
                }
            return list;
        }

        public void CreateEquipe(Equipe e)
        {
            e.ScoreGeneral = CalcScoreEquipe(e.JoueurIds);
            using var conn = Open();
            const string sql = @"
                INSERT INTO equipes
                    (nom_equipe,id_joueur1,id_joueur2,id_joueur3,
                     id_joueur4,id_joueur5,id_joueur6,id_joueur7,score_general)
                VALUES(@nom,@j1,@j2,@j3,@j4,@j5,@j6,@j7,@sc);";
            using var cmd = new MySqlCommand(sql, conn);
            BindEquipeParams(cmd, e);
            cmd.ExecuteNonQuery();
        }

        public void UpdateEquipe(Equipe e)
        {
            e.ScoreGeneral = CalcScoreEquipe(e.JoueurIds);
            using var conn = Open();
            const string sql = @"
                UPDATE equipes SET
                    nom_equipe=@nom,
                    id_joueur1=@j1,id_joueur2=@j2,id_joueur3=@j3,
                    id_joueur4=@j4,id_joueur5=@j5,id_joueur6=@j6,id_joueur7=@j7,
                    score_general=@sc
                WHERE id_equipe=@id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", e.Id);
            BindEquipeParams(cmd, e);
            cmd.ExecuteNonQuery();
        }

        private static void BindEquipeParams(MySqlCommand cmd, Equipe e)
        {
            cmd.Parameters.AddWithValue("@nom", e.Nom);
            cmd.Parameters.AddWithValue("@sc",  e.ScoreGeneral);
            for (int i = 0; i < 7; i++)
                cmd.Parameters.AddWithValue($"@j{i + 1}", e.JoueurIds[i]);
        }

        public bool DeleteEquipe(int id)
        {
            using var conn = Open();
            using var cmd = new MySqlCommand("DELETE FROM equipes WHERE id_equipe=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public void EvaluerJoueur(int idJoueur, int delta)
        {
            if (delta == 0) return;
            int def, att, vit, end;
            using (var conn = Open())
            {
                using var cmd = new MySqlCommand(
                    "SELECT score_defense, score_attaque, score_vitesse, score_endurance FROM joueurs WHERE id_joueur=@id;", conn);
                cmd.Parameters.AddWithValue("@id", idJoueur);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return;
                def = Math.Clamp(r.GetInt32(0) + delta, 0, 100);
                att = Math.Clamp(r.GetInt32(1) + delta, 0, 100);
                vit = Math.Clamp(r.GetInt32(2) + delta, 0, 100);
                end = Math.Clamp(r.GetInt32(3) + delta, 0, 100);
            }
            int gen = (def + att + vit + end) / 4;
            using (var conn = Open())
            {
                using var cmd = new MySqlCommand(
                    "UPDATE joueurs SET score_defense=@d, score_attaque=@a, score_vitesse=@v, score_endurance=@e, score_general=@g WHERE id_joueur=@id;", conn);
                cmd.Parameters.AddWithValue("@d", def);
                cmd.Parameters.AddWithValue("@a", att);
                cmd.Parameters.AddWithValue("@v", vit);
                cmd.Parameters.AddWithValue("@e", end);
                cmd.Parameters.AddWithValue("@g", gen);
                cmd.Parameters.AddWithValue("@id", idJoueur);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateButsMarques(int idJoueur, int buts)
        {
            using var conn = Open();
            using var cmd = new MySqlCommand(
                "UPDATE joueurs SET score_goal=@b WHERE id_joueur=@id;", conn);
            cmd.Parameters.AddWithValue("@b",  buts);
            cmd.Parameters.AddWithValue("@id", idJoueur);
            cmd.ExecuteNonQuery();
        }

        public void EnregistrerButs(List<ButInfo> buts)
        {
            if (buts.Count == 0) return;
            using var conn = Open();
            foreach (var groupe in buts.GroupBy(b => b.IdJoueur))
            {
                using var cmd = new MySqlCommand(
                    "UPDATE joueurs SET score_goal = score_goal + @n WHERE id_joueur = @id;", conn);
                cmd.Parameters.AddWithValue("@n",  groupe.Count());
                cmd.Parameters.AddWithValue("@id", groupe.Key);
                cmd.ExecuteNonQuery();
            }
        }

        private int CalcScoreEquipe(int[] ids)
        {
            using var conn = Open();
            int somme = 0, count = 0;
            foreach (int id in ids)
            {
                using var cmd = new MySqlCommand(
                    "SELECT score_general FROM joueurs WHERE id_joueur=@id;", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value) { somme += Convert.ToInt32(res); count++; }
            }
            return count > 0 ? somme / count : 0;
        }

        // ─────────────────────────── BLESSURES ──────────────────────────

        public List<Blessure> GetAllBlessures()
        {
            var list = new List<Blessure>();
            using var conn = Open();
            using var cmd = new MySqlCommand(
                "SELECT id_blessure, type_blessure, pénalité FROM blessures;", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Blessure
                {
                    Id       = r.GetInt32("id_blessure"),
                    Type     = r.IsDBNull(r.GetOrdinal("type_blessure")) ? string.Empty : r.GetString("type_blessure"),
                    Penalite = r.IsDBNull(r.GetOrdinal("pénalité"))      ? 0            : r.GetInt32("pénalité"),
                });
            return list;
        }

        // ─────────────────────────── MATCHS ─────────────────────────────

        public List<MatchResult> GetAllMatchs()
        {
            var list = new List<MatchResult>();
            using var conn = Open();
            const string sql = @"
                SELECT m.id_match, m.score_equipe1, m.score_equipe2, m.date_match,
                       m.id_equipe1, m.id_equipe2,
                       e1.nom_equipe AS nom1, e2.nom_equipe AS nom2
                FROM matchs m
                JOIN equipes e1 ON m.id_equipe1 = e1.id_equipe
                JOIN equipes e2 ON m.id_equipe2 = e2.id_equipe
                ORDER BY m.date_match DESC;";
            using var cmd = new MySqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MatchResult
                {
                    Id           = r.GetInt32("id_match"),
                    IdEquipe1    = r.GetInt32("id_equipe1"),
                    IdEquipe2    = r.GetInt32("id_equipe2"),
                    NomEquipe1   = r.GetString("nom1"),
                    NomEquipe2   = r.GetString("nom2"),
                    ScoreEquipe1 = r.GetInt32("score_equipe1"),
                    ScoreEquipe2 = r.GetInt32("score_equipe2"),
                    DateMatch    = r.GetDateTime("date_match"),
                });
            return list;
        }

        public void SaveMatch(int idEq1, int idEq2, int s1, int s2)
        {
            using var conn = Open();
            const string sql = @"
                INSERT INTO matchs (score_equipe1, score_equipe2, id_equipe1, id_equipe2, date_match)
                VALUES (@s1,@s2,@e1,@e2,NOW());";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s1", s1);
            cmd.Parameters.AddWithValue("@s2", s2);
            cmd.Parameters.AddWithValue("@e1", idEq1);
            cmd.Parameters.AddWithValue("@e2", idEq2);
            cmd.ExecuteNonQuery();
        }

        // ─────────────────────────── SIMULATION ─────────────────────────

        public int[] GetJoueursEquipe(int idEquipe)
        {
            using var conn = Open();
            const string sql = @"
                SELECT id_joueur1,id_joueur2,id_joueur3,
                       id_joueur4,id_joueur5,id_joueur6,id_joueur7
                FROM equipes WHERE id_equipe=@id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idEquipe);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return Array.Empty<int>();
            return new[] { r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3),
                           r.GetInt32(4),r.GetInt32(5),r.GetInt32(6) };
        }

        public int CalcScoreAvecBlessures(int[] ids)
        {
            using var conn = Open();
            int somme = 0, count = 0;
            const string sql = @"
                SELECT j.score_general, j.id_blessure, b.pénalité
                FROM joueurs j
                LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
                WHERE j.id_joueur=@id;";
            foreach (int id in ids)
            {
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    int score = r.IsDBNull(r.GetOrdinal("score_general")) ? 0 : r.GetInt32("score_general");
                    if (!r.IsDBNull(r.GetOrdinal("id_blessure")) && !r.IsDBNull(r.GetOrdinal("pénalité")))
                        score += Convert.ToInt32(r["pénalité"]);
                    somme += Math.Clamp(score, 0, 10);
                    count++;
                }
            }
            return count > 0 ? somme / count : 0;
        }

        public List<int> GetPoursuiveurs(int[] ids)
        {
            var list = new List<int>();
            using var conn = Open();
            foreach (int id in ids)
            {
                using var cmd = new MySqlCommand(
                    "SELECT affectation_joueur FROM joueurs WHERE id_joueur=@id;", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var val = cmd.ExecuteScalar()?.ToString()?.Trim();
                if (string.Equals(val, "Poursuiveur", StringComparison.OrdinalIgnoreCase))
                    list.Add(id);
            }
            return list;
        }

        public string GetNomJoueur(int id)
        {
            using var conn = Open();
            using var cmd = new MySqlCommand(
                "SELECT nom_joueur FROM joueurs WHERE id_joueur=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar()?.ToString() ?? $"Joueur {id}";
        }

        // Gère la décrémentation des blessures + 10% chance nouvelle blessure.
        // Retourne la liste des messages de blessures à afficher.
        public List<string> GererBlessures(int[] ids)
        {
            var notifs   = new List<string>();
            var blessures = GetAllBlessures();
            var rnd       = new Random();

            foreach (int idJoueur in ids)
            {
                int idBlessure = 0;
                int restant    = 0;

                using (var conn = Open())
                using (var cmd = new MySqlCommand(
                    "SELECT id_blessure, matchs_restants_blessure FROM joueurs WHERE id_joueur=@id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", idJoueur);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        if (!r.IsDBNull(r.GetOrdinal("id_blessure")))
                            idBlessure = r.GetInt32("id_blessure");
                        restant = r.IsDBNull(r.GetOrdinal("matchs_restants_blessure")) ? 0 : r.GetInt32("matchs_restants_blessure");
                    }
                }

                if (idBlessure != 0)
                {
                    restant--;
                    using var conn = Open();
                    if (restant <= 0)
                    {
                        using var cmd = new MySqlCommand(
                            "UPDATE joueurs SET id_blessure=NULL, matchs_restants_blessure=0 WHERE id_joueur=@id;", conn);
                        cmd.Parameters.AddWithValue("@id", idJoueur);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var cmd = new MySqlCommand(
                            "UPDATE joueurs SET matchs_restants_blessure=@r WHERE id_joueur=@id;", conn);
                        cmd.Parameters.AddWithValue("@r",  restant);
                        cmd.Parameters.AddWithValue("@id", idJoueur);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (blessures.Count > 0 && rnd.Next(100) < 10)
                {
                    var b = blessures[rnd.Next(blessures.Count)];
                    using var conn = Open();
                    using var cmd = new MySqlCommand(
                        "UPDATE joueurs SET id_blessure=@b, matchs_restants_blessure=3 WHERE id_joueur=@id;", conn);
                    cmd.Parameters.AddWithValue("@b",  b.Id);
                    cmd.Parameters.AddWithValue("@id", idJoueur);
                    cmd.ExecuteNonQuery();
                    notifs.Add($"{GetNomJoueur(idJoueur)} → blessure : {b.Type}");
                }
            }
            return notifs;
        }
    }
}
