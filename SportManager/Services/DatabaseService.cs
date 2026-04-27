using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
using SportManager.Models;

namespace SportManager.Services
{
    /// <summary>
    /// Service d'accès aux données MySQL via Dapper.
    /// Centralise toutes les requêtes SQL de l'application WPF.
    /// </summary>
    public class DatabaseService
    {
        // Chaîne de connexion à la BDD locale
        private const string ConnStr =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        /// <summary>Ouvre une nouvelle connexion MySQL. À utiliser dans un bloc using.</summary>
        private MySqlConnection Open()
        {
            var c = new MySqlConnection(ConnStr);
            c.Open();
            return c;
        }

        // ─────────────────────────── MIGRATIONS ─────────────────────────

        /// <summary>
        /// Génère des statistiques aléatoires cohérentes par poste pour tous les joueurs.
        /// Exécutée une seule fois grâce à la table `migrations`.
        /// </summary>
        public void MigrateRandomStats()
        {
            using var conn = Open();
            // Crée la table de suivi des migrations si elle n'existe pas encore
            conn.Execute("CREATE TABLE IF NOT EXISTS migrations (nom VARCHAR(100) PRIMARY KEY);");

            // Si la migration a déjà été appliquée, on ne fait rien
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

        /// <summary>
        /// Migration v2 : ajoute la colonne score_endurance si absente et remet les stats à l'échelle 0-100.
        /// Anciennement les scores étaient sur 0-10 ; cette migration les multiplie par 10.
        /// </summary>
        public void MigrateToV2()
        {
            using var conn = Open();
            // Vérifie si la colonne score_endurance existe déjà dans information_schema
            bool exists = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME   = 'joueurs'
                  AND COLUMN_NAME  = 'score_endurance';") > 0;

            if (!exists)
            {
                // Ajoute la colonne manquante avec une valeur par défaut neutre
                conn.Execute("ALTER TABLE joueurs ADD COLUMN score_endurance INT NOT NULL DEFAULT 50;");
                // Remet tous les scores à l'échelle ×10 pour passer de 0-10 à 0-100
                conn.Execute(@"UPDATE joueurs SET
                    score_defense   = score_defense  * 10,
                    score_attaque   = score_attaque  * 10,
                    score_vitesse   = score_vitesse  * 10,
                    score_endurance = 50,
                    score_general   = score_general  * 10;");
            }
        }

        // ─────────────────────────── JOUEURS ────────────────────────────

        // Requête SELECT réutilisée par GetAllJoueurs, GetJoueursByAffectation, GetJoueurById.
        // Les alias SQL (AS ...) correspondent exactement aux noms des propriétés C# pour que Dapper
        // fasse le mapping automatiquement sans configuration supplémentaire.
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

        /// <summary>
        /// Applique un delta (+5 Excellent / -5 Insuffisant) sur toutes les stats d'un joueur.
        /// Les stats sont clampées entre 0 et 100 pour éviter les dépassements.
        /// </summary>
        public void EvaluerJoueur(int idJoueur, int delta)
        {
            if (delta == 0) return;  // note "Bon" → aucun changement
            using var conn = Open();

            // Récupère les stats actuelles pour calculer les nouvelles valeurs
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

        /// <summary>
        /// Incrémente score_goal pour chaque joueur buteur.
        /// Les buts sont regroupés par joueur pour faire une seule UPDATE par joueur.
        /// </summary>
        public void EnregistrerButs(List<ButInfo> buts)
        {
            if (buts.Count == 0) return;
            using var conn = Open();
            // GroupBy IdJoueur → une seule requête UPDATE par joueur au lieu de N requêtes
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

        /// <summary>Insère le match et retourne son id généré (LAST_INSERT_ID).</summary>
        public int SaveMatch(int idEq1, int idEq2, int s1, int s2)
        {
            using var conn = Open();
            conn.Execute(@"
                INSERT INTO matchs (score_equipe1, score_equipe2, id_equipe1, id_equipe2, date_match)
                VALUES (@s1, @s2, @idEq1, @idEq2, NOW());",
                new { s1, s2, idEq1, idEq2 });
            return conn.ExecuteScalar<int>("SELECT LAST_INSERT_ID();");
        }

        /// <summary>
        /// Persiste les buts dans buts_match en les liant à l'id du match.
        /// id_equipe est stocké directement pour éviter une jointure complexe au moment de la lecture.
        /// </summary>
        public void SaveButsMatch(int idMatch, List<ButInfo> buts)
        {
            if (buts.Count == 0) return;
            using var conn = Open();
            foreach (var b in buts)
                conn.Execute(@"
                    INSERT INTO buts_match (id_match, id_joueur, id_equipe, num_mi_temps)
                    VALUES (@idMatch, @IdJoueur, @IdEquipe, @NumMiTemps);",
                    new { idMatch, b.IdJoueur, b.IdEquipe, b.NumMiTemps });
        }

        /// <summary>Persiste les blessures survenues dans blessures_match en les liant à l'id du match.</summary>
        public void SaveBlessuresMatch(int idMatch, List<(int idJoueur, int idBlessure)> blessures)
        {
            if (blessures.Count == 0) return;
            using var conn = Open();
            foreach (var (idJoueur, idBlessure) in blessures)
                conn.Execute(@"
                    INSERT INTO blessures_match (id_match, id_joueur, id_blessure)
                    VALUES (@idMatch, @idJoueur, @idBlessure);",
                    new { idMatch, idJoueur, idBlessure });
        }

        /// <summary>
        /// Charge le détail complet d'un match (buteurs + blessures) depuis les tables de détail.
        /// Retourne un MatchDetail avec AucunDetail=true si le match est antérieur à la fonctionnalité.
        /// </summary>
        public MatchDetail GetDetailMatch(int idMatch)
        {
            using var conn = Open();

            // id_equipe est stocké directement dans buts_match → jointure simple
            var buts = conn.Query<ButDetail>(@"
                SELECT j.nom_joueur    AS NomJoueur,
                       e.nom_equipe    AS NomEquipe,
                       bm.num_mi_temps AS NumMiTemps
                FROM buts_match bm
                JOIN joueurs j ON bm.id_joueur = j.id_joueur
                JOIN equipes e ON bm.id_equipe = e.id_equipe
                WHERE bm.id_match = @idMatch
                ORDER BY bm.num_mi_temps, e.nom_equipe, j.nom_joueur;",
                new { idMatch }).ToList();

            // Pour les blessures on retrouve l'équipe via la table matchs (equipe1 ou equipe2)
            var blessures = conn.Query<BlessureDetail>(@"
                SELECT j.nom_joueur    AS NomJoueur,
                       e.nom_equipe    AS NomEquipe,
                       b.type_blessure AS TypeBlessure,
                       b.pénalité      AS Penalite
                FROM blessures_match bm
                JOIN joueurs   j ON bm.id_joueur   = j.id_joueur
                JOIN blessures b ON bm.id_blessure = b.id_blessure
                JOIN matchs    m ON bm.id_match    = m.id_match
                JOIN equipes   e ON e.id_equipe    = IF(
                    bm.id_joueur IN (SELECT id_joueur1 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur2 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur3 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur4 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur5 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur6 FROM equipes WHERE id_equipe = m.id_equipe1
                                     UNION SELECT id_joueur7 FROM equipes WHERE id_equipe = m.id_equipe1),
                    m.id_equipe1, m.id_equipe2)
                WHERE bm.id_match = @idMatch
                ORDER BY e.nom_equipe, j.nom_joueur;",
                new { idMatch }).ToList();

            return new MatchDetail { Buts = buts, Blessures = blessures };
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

        /// <summary>
        /// Calcule le score effectif de l'équipe en tenant compte des blessures.
        /// Chaque joueur est pondéré selon son poste (ex: Gardien → 50% défense).
        /// Le malus de blessure est ajouté directement au score pondéré.
        /// Une seule requête SQL IN() récupère tous les joueurs d'un coup.
        /// </summary>
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

        /// <summary>
        /// Gère les blessures après un match pour un groupe de joueurs :
        /// 1. Décrémente le compteur de matchs restants des joueurs déjà blessés.
        /// 2. Guérit les joueurs dont le compteur atteint 0.
        /// 3. Inflige aléatoirement une nouvelle blessure (10% de chance par joueur).
        /// Retourne les notifications texte ET les tuples (idJoueur, idBlessure) pour la persistance en BDD.
        /// </summary>
        public (List<string> Notifs, List<(int idJoueur, int idBlessure)> Nouvelles) GererBlessures(int[] ids)
        {
            var notifs    = new List<string>();
            var nouvelles = new List<(int, int)>();
            var blessures = GetAllBlessures();
            var rnd       = new Random();
            var validIds  = ids.Where(id => id != 0).ToList();
            if (validIds.Count == 0) return (notifs, nouvelles);

            using var conn = Open();
            // Récupère l'état de blessure actuel de tous les joueurs de l'équipe en une seule requête
            var etats = conn.Query(@"
                SELECT id_joueur AS Id, id_blessure AS IdBlessure, matchs_restants_blessure AS Restant
                FROM joueurs WHERE id_joueur IN @validIds;",
                new { validIds }).ToList();

            foreach (var etat in etats)
            {
                int idJoueur   = (int)etat.Id;
                int idBlessure = etat.IdBlessure != null ? (int)etat.IdBlessure : 0;
                int restant    = etat.Restant    != null ? (int)etat.Restant    : 0;

                // Si le joueur est déjà blessé, on décrémente son compteur
                if (idBlessure != 0)
                {
                    restant--;
                    if (restant <= 0)
                        conn.Execute("UPDATE joueurs SET id_blessure=NULL, matchs_restants_blessure=0 WHERE id_joueur=@idJoueur;", new { idJoueur });
                    else
                        conn.Execute("UPDATE joueurs SET matchs_restants_blessure=@restant WHERE id_joueur=@idJoueur;", new { restant, idJoueur });
                }

                // 10% de chance de contracter une nouvelle blessure à chaque match
                if (blessures.Count > 0 && rnd.Next(100) < 10)
                {
                    var b = blessures[rnd.Next(blessures.Count)];
                    conn.Execute("UPDATE joueurs SET id_blessure=@bId, matchs_restants_blessure=3 WHERE id_joueur=@idJoueur;",
                        new { bId = b.Id, idJoueur });
                    notifs.Add($"{GetNomJoueur(idJoueur)} → blessure : {b.Type}");
                    // On retourne le tuple pour pouvoir l'insérer dans blessures_match
                    nouvelles.Add((idJoueur, b.Id));
                }
            }
            return (notifs, nouvelles);
        }

        // ─────────────────────────── PRIVÉ ──────────────────────────────

        /// <summary>
        /// Calcule le score général moyen d'une équipe à partir des score_general des joueurs.
        /// Utilisé à la création/modification d'une équipe pour stocker une valeur en BDD.
        /// Contrairement à CalcScoreAvecBlessures, il ne tient pas compte des blessures ni de la pondération par poste.
        /// </summary>
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
