using System.Collections.Generic;
using Dapper;
using MySql.Data.MySqlClient;

namespace SportManager.Services
{
    /// <summary>
    /// Initialise la base de données sportmanager depuis zéro via Dapper.
    ///
    /// Pourquoi Dapper ici ?
    /// Dapper est le micro-ORM du projet. Il ne génère pas le SQL lui-même
    /// (contrairement à Entity Framework), mais il exécute le SQL qu'on lui
    /// fournit et mappe les résultats vers des objets C#.
    /// Ici on l'utilise pour exécuter les CREATE TABLE et les INSERT de données
    /// de référence — exactement comme il exécute les requêtes métier dans DatabaseService.
    ///
    /// Utilisation : appeler Initialize() au démarrage si la BDD est vide.
    /// Toutes les instructions sont idempotentes (IF NOT EXISTS / INSERT IGNORE).
    /// </summary>
    public class DatabaseInitializer
    {
        // Chaîne de connexion sans base de données spécifiée — on la crée nous-mêmes
        private const string ConnStrSansDb =
            "Server=localhost;Uid=root;Pwd=rootroot;";

        // Chaîne de connexion avec la BDD cible
        private const string ConnStr =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        // ─────────────────────────────────────────────────────────────────
        //  POINT D'ENTRÉE
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crée la base de données, toutes ses tables et insère les données de référence.
        /// Peut être appelé plusieurs fois sans risque (opérations idempotentes).
        /// </summary>
        public void Initialize()
        {
            CreerBase();
            CreerTables();
            InsererDonneesReference();
        }

        // ─────────────────────────────────────────────────────────────────
        //  ÉTAPE 1 — Création de la base de données
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crée la base de données sportmanager si elle n'existe pas encore.
        /// On ouvre la connexion sans sélectionner de BDD pour pouvoir exécuter CREATE DATABASE.
        /// </summary>
        private void CreerBase()
        {
            using var conn = new MySqlConnection(ConnStrSansDb);
            conn.Open();
            conn.Execute(@"
                CREATE DATABASE IF NOT EXISTS sportmanager
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_uca1400_ai_ci;");
        }

        // ─────────────────────────────────────────────────────────────────
        //  ÉTAPE 2 — Création des tables (dans l'ordre des dépendances FK)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crée toutes les tables dans le bon ordre pour respecter les clés étrangères :
        /// blessures → joueurs → equipes → matchs → migrations → buts_match → blessures_match
        /// </summary>
        private void CreerTables()
        {
            using var conn = new MySqlConnection(ConnStr);
            conn.Open();

            // Désactive temporairement les vérifications FK pour simplifier l'ordre de création
            conn.Execute("SET FOREIGN_KEY_CHECKS = 0;");

            foreach (var sql in GetCreateTableStatements())
                conn.Execute(sql);

            conn.Execute("SET FOREIGN_KEY_CHECKS = 1;");
        }

        /// <summary>
        /// Retourne les instructions CREATE TABLE dans l'ordre de dépendance.
        /// Chaque instruction utilise IF NOT EXISTS pour être idempotente.
        /// </summary>
        private static IEnumerable<string> GetCreateTableStatements()
        {
            // ── 1. blessures (pas de FK entrantes) ──────────────────────
            yield return @"
                CREATE TABLE IF NOT EXISTS blessures (
                    id_blessure  INT(11)      NOT NULL AUTO_INCREMENT,
                    type_blessure VARCHAR(50) DEFAULT NULL,
                    pénalité      INT(11)     DEFAULT NULL,
                    PRIMARY KEY (id_blessure)
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 2. joueurs (FK → blessures) ──────────────────────────────
            yield return @"
                CREATE TABLE IF NOT EXISTS joueurs (
                    id_joueur                INT(11)     NOT NULL AUTO_INCREMENT,
                    nom_joueur               VARCHAR(50) DEFAULT NULL,
                    score_defense            INT(11)     DEFAULT NULL,
                    score_attaque            INT(11)     DEFAULT NULL,
                    score_vitesse            INT(11)     NOT NULL DEFAULT 0,
                    score_endurance          INT(11)     NOT NULL DEFAULT 50,
                    score_general            INT(11)     DEFAULT NULL,
                    score_goal               INT(11)     DEFAULT 0,
                    affectation_joueur       VARCHAR(50) DEFAULT NULL,
                    id_blessure              INT(11)     DEFAULT NULL,
                    matchs_restants_blessure INT(11)     DEFAULT NULL,
                    PRIMARY KEY (id_joueur),
                    CONSTRAINT joueurs_blessures_fk
                        FOREIGN KEY (id_blessure) REFERENCES blessures (id_blessure)
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 3. equipes (FK → joueurs ×7) ────────────────────────────
            yield return @"
                CREATE TABLE IF NOT EXISTS equipes (
                    id_equipe     INT(11)     NOT NULL AUTO_INCREMENT,
                    nom_equipe    VARCHAR(50) DEFAULT NULL,
                    score_general INT(11)     DEFAULT NULL,
                    id_joueur1    INT(11)     DEFAULT NULL,
                    id_joueur2    INT(11)     DEFAULT NULL,
                    id_joueur3    INT(11)     DEFAULT NULL,
                    id_joueur4    INT(11)     DEFAULT NULL,
                    id_joueur5    INT(11)     DEFAULT NULL,
                    id_joueur6    INT(11)     DEFAULT NULL,
                    id_joueur7    INT(11)     DEFAULT NULL,
                    PRIMARY KEY (id_equipe),
                    CONSTRAINT equipes_joueurs_fk_1 FOREIGN KEY (id_joueur1) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_2 FOREIGN KEY (id_joueur2) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_3 FOREIGN KEY (id_joueur3) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_4 FOREIGN KEY (id_joueur4) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_5 FOREIGN KEY (id_joueur5) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_6 FOREIGN KEY (id_joueur6) REFERENCES joueurs (id_joueur),
                    CONSTRAINT equipes_joueurs_fk_7 FOREIGN KEY (id_joueur7) REFERENCES joueurs (id_joueur)
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 4. matchs (FK → equipes) ─────────────────────────────────
            // date_match est VARCHAR(50) comme dans la BDD existante
            yield return @"
                CREATE TABLE IF NOT EXISTS matchs (
                    id_match      INT(11)     NOT NULL AUTO_INCREMENT,
                    score_equipe1 INT(11)     DEFAULT NULL,
                    score_equipe2 INT(11)     DEFAULT NULL,
                    id_equipe1    INT(11)     DEFAULT NULL,
                    id_equipe2    INT(11)     DEFAULT NULL,
                    date_match    VARCHAR(50) DEFAULT NULL,
                    PRIMARY KEY (id_match),
                    CONSTRAINT matchs_equipes_fk   FOREIGN KEY (id_equipe1) REFERENCES equipes (id_equipe),
                    CONSTRAINT matchs_equipes_fk_1 FOREIGN KEY (id_equipe2) REFERENCES equipes (id_equipe)
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 5. migrations (suivi des migrations appliquées) ──────────
            yield return @"
                CREATE TABLE IF NOT EXISTS migrations (
                    nom VARCHAR(100) NOT NULL,
                    PRIMARY KEY (nom)
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 6. buts_match (FK → matchs, joueurs, equipes) ───────────
            yield return @"
                CREATE TABLE IF NOT EXISTS buts_match (
                    id           INT(11) NOT NULL AUTO_INCREMENT,
                    id_match     INT(11) NOT NULL,
                    id_joueur    INT(11) NOT NULL,
                    id_equipe    INT(11) NOT NULL,
                    num_mi_temps TINYINT(4) NOT NULL,
                    PRIMARY KEY (id),
                    CONSTRAINT buts_match_ibfk_1 FOREIGN KEY (id_match)  REFERENCES matchs  (id_match)  ON DELETE CASCADE,
                    CONSTRAINT buts_match_ibfk_2 FOREIGN KEY (id_joueur) REFERENCES joueurs (id_joueur) ON DELETE CASCADE,
                    CONSTRAINT buts_match_ibfk_3 FOREIGN KEY (id_equipe) REFERENCES equipes (id_equipe) ON DELETE CASCADE
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";

            // ── 7. blessures_match (FK → matchs, joueurs, blessures) ─────
            yield return @"
                CREATE TABLE IF NOT EXISTS blessures_match (
                    id          INT(11) NOT NULL AUTO_INCREMENT,
                    id_match    INT(11) NOT NULL,
                    id_joueur   INT(11) NOT NULL,
                    id_blessure INT(11) NOT NULL,
                    PRIMARY KEY (id),
                    CONSTRAINT blessures_match_ibfk_1 FOREIGN KEY (id_match)    REFERENCES matchs    (id_match)    ON DELETE CASCADE,
                    CONSTRAINT blessures_match_ibfk_2 FOREIGN KEY (id_joueur)   REFERENCES joueurs   (id_joueur)   ON DELETE CASCADE,
                    CONSTRAINT blessures_match_ibfk_3 FOREIGN KEY (id_blessure) REFERENCES blessures (id_blessure) ON DELETE CASCADE
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_uca1400_ai_ci;";
        }

        // ─────────────────────────────────────────────────────────────────
        //  ÉTAPE 3 — Données de référence
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Insère les données de référence (types de blessures).
        /// INSERT IGNORE ignore les doublons si les données existent déjà.
        /// </summary>
        private void InsererDonneesReference()
        {
            using var conn = new MySqlConnection(ConnStr);
            conn.Open();

            // Les 16 types de blessures de référence du jeu
            // On insère avec id explicite pour garantir la cohérence avec les FK existantes
            var blessures = new[]
            {
                new { id = 1,  type = "fracture",              penalite = -3  },
                new { id = 2,  type = "malade",                penalite = -1  },
                new { id = 3,  type = "trouble de la vision",  penalite = -2  },
                new { id = 4,  type = "tendinite",             penalite = -2  },
                new { id = 5,  type = "entorse",               penalite = -2  },
                new { id = 6,  type = "tournis",               penalite = -1  },
                new { id = 7,  type = "poigné casssé",         penalite = -4  },
                new { id = 8,  type = "aveugle",               penalite = -8  },
                new { id = 9,  type = "déchirure musculaire",  penalite = -6  },
                new { id = 10, type = "contusion musculaire",  penalite = -3  },
                new { id = 11, type = "leger saignement",      penalite = -1  },
                new { id = 12, type = "moyen saignement",      penalite = -2  },
                new { id = 13, type = "gros saignement",       penalite = -3  },
                new { id = 14, type = "hémorragie",            penalite = -4  },
                new { id = 15, type = "ongle cassé",           penalite = -1  },
                new { id = 16, type = "mort.",                 penalite = -10 },
            };

            foreach (var b in blessures)
                conn.Execute(@"
                    INSERT IGNORE INTO blessures (id_blessure, type_blessure, pénalité)
                    VALUES (@id, @type, @penalite);", b);
        }
    }
}
