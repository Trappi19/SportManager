using System;
using MySql.Data.MySqlClient;

namespace Match_Manager
{
    internal class MatchSystem
    {
        private static readonly string connectionString =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        public static void MenuMatchs()
        {
            bool quitter = false;
            while (!quitter)
            {
                Console.Clear();
                Console.WriteLine("=== SYSTEME DE MATCH ===");
                Console.WriteLine("1. Jouer un match");
                Console.WriteLine("2. Voir l'historique des matchs");
                Console.WriteLine("3. Retour");
                Console.Write("Ton choix : ");
                string choix = Console.ReadLine();

                switch (choix)
                {
                    case "1":
                        JouerMatch();
                        break;
                    case "2":
                        Historic_Manager.HistoricSystem.MenuHistorique();
                        break;
                    case "3":
                        quitter = true; Menu_Manager.Menu.ShowMenu();
                        break;
                    default:
                        Console.WriteLine("Mauvais choix, entrée pour continuer.");
                        Console.ReadLine();
                        break;
                }
            }
        }

        private static void JouerMatch()
        {
            Console.Clear();
            Console.WriteLine("=== NOUVEAU MATCH ===\n");

            // 1) Choix des deux équipes
            int equipe1 = ChoisirEquipe("Equipe 1");
            int equipe2 = ChoisirEquipe("Equipe 2 (différente de la 1)");

            if (equipe1 == 0 || equipe2 == 0 || equipe1 == equipe2)
            {
                Console.WriteLine("Sélection d'équipes invalide, retour.");
                Console.ReadLine();
                return;
            }

            // 2) Charger les joueurs des 2 équipes
            int[] joueursEq1 = ChargerJoueursEquipe(equipe1);
            int[] joueursEq2 = ChargerJoueursEquipe(equipe2);

            // 3) Appliquer les malus de blessures pour ce match (on calcule un score effectif)
            int scoreTeam1Base = CalculerScoreEquipeAvecBlessures(joueursEq1);
            int scoreTeam2Base = CalculerScoreEquipeAvecBlessures(joueursEq2);

            Console.WriteLine($"\nScore moyen (avec blessures) avant match :");
            Console.WriteLine($"Equipe {equipe1} : {scoreTeam1Base}/10");
            Console.WriteLine($"Equipe {equipe2} : {scoreTeam2Base}/10\n");

            // 4) Simulation des 2 mi-temps
            Random rnd = new Random();

            int score1MT1 = SimulerMiTemps(scoreTeam1Base, scoreTeam2Base, rnd);
            int score2MT1 = SimulerMiTemps(scoreTeam2Base, scoreTeam1Base, rnd);

            Console.WriteLine($"Mi-temps : {score1MT1} - {score2MT1}");
            Console.WriteLine("Appuie sur entrée pour passer à la 2ème mi-temps.");
            Console.ReadLine();

            int score1MT2 = SimulerMiTemps(scoreTeam1Base, scoreTeam2Base, rnd);
            int score2MT2 = SimulerMiTemps(scoreTeam2Base, scoreTeam1Base, rnd);

            int scoreFinal1 = score1MT1 + score1MT2;
            int scoreFinal2 = score2MT1 + score2MT2;

            Console.WriteLine($"\nScore final : {scoreFinal1} - {scoreFinal2}");

            // 5) Enregistrement du match
            EnregistrerMatch(equipe1, equipe2, scoreFinal1, scoreFinal2);

            // 6) Gestion des blessures (disparition + nouvelles)
            GérerBlessuresAprèsMatch(joueursEq1);
            GérerBlessuresAprèsMatch(joueursEq2);

            Console.WriteLine("\nMatch enregistré et blessures mises à jour. Entrée pour continuer.");
            Console.ReadLine();
        }

        private static int ChoisirEquipe(string label)
        {
            Console.WriteLine($"\n{label} :");
            // On liste vite fait les équipes dispos
            string sql = "SELECT id_equipe, nom_equipe FROM equipes ORDER BY nom_equipe;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Console.WriteLine($"{r["id_equipe"]} - {r["nom_equipe"]}");
                    }
                }
            }

            Console.Write("ID de l'équipe : ");
            string saisie = Console.ReadLine();
            if (!int.TryParse(saisie, out int id))
                return 0;
            return id;
        }

        private static int[] ChargerJoueursEquipe(int idEquipe)
        {
            int[] joueurs = new int[7];
            string sql = @"
                SELECT id_joueur1,id_joueur2,id_joueur3,
                       id_joueur4,id_joueur5,id_joueur6,id_joueur7
                FROM equipes WHERE id_equipe = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idEquipe);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            joueurs[0] = r.GetInt32("id_joueur1");
                            joueurs[1] = r.GetInt32("id_joueur2");
                            joueurs[2] = r.GetInt32("id_joueur3");
                            joueurs[3] = r.GetInt32("id_joueur4");
                            joueurs[4] = r.GetInt32("id_joueur5");
                            joueurs[5] = r.GetInt32("id_joueur6");
                            joueurs[6] = r.GetInt32("id_joueur7");
                        }
                    }
                }
            }
            return joueurs;
        }

        // Calcule la moyenne des scores généraux des joueurs en tenant compte des blessures
        private static int CalculerScoreEquipeAvecBlessures(int[] joueurs)
        {
            if (joueurs == null || joueurs.Length != 7) return 0;

            int somme = 0;
            int count = 0;

            string sql = @"
                SELECT j.score_general, j.id_blessure, b.pénalité
                FROM joueurs j
                LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
                WHERE j.id_joueur = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (int id in joueurs)
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int score = r.GetInt32("score_general");

                                if (!r.IsDBNull(r.GetOrdinal("id_blessure")))
                                {
                                    // Ici tu peux parser pénalité genre "-2 score_defense"
                                    // Pour rester simple on applique juste -2 sur le score général
                                    // ou tu crées une pénalité numérique dédiée dans la table.
                                    string pen = r["pénalité"]?.ToString();
                                    if (!string.IsNullOrEmpty(pen) && pen.Contains("-2"))
                                        score -= 2;
                                }

                                if (score < 0) score = 0;
                                somme += score;
                                count++;
                            }
                        }
                    }
                }
            }

            if (count == 0) return 0;
            return somme / count; // /10, puisque les joueurs sont déjà /10
        }

        // Une mi-temps : baseScore + aléatoire
        private static int SimulerMiTemps(int scoreEquipe, int scoreAdverse, Random rnd)
        {
            // petit bonus/malus selon écart de niveau
            int diff = scoreEquipe - scoreAdverse; // peut être négatif
            double facteur = 1.0 + (diff * 0.05);  // 5% par point d'écart
            if (facteur < 0.5) facteur = 0.5;

            int butsDeBase = rnd.Next(0, 4); // 0 à 3 buts
            int buts = (int)(butsDeBase * facteur);

            if (buts < 0) buts = 0;
            return buts;
        }

        private static void EnregistrerMatch(int idEquipe1, int idEquipe2, int score1, int score2)
        {
            string sql = @"
                INSERT INTO matchs (score_equipe1, score_equipe2, id_equipe1, id_equipe2, date_match)
                VALUES (@s1, @s2, @e1, @e2, NOW());";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@s1", score1);
                    cmd.Parameters.AddWithValue("@s2", score2);
                    cmd.Parameters.AddWithValue("@e1", idEquipe1);
                    cmd.Parameters.AddWithValue("@e2", idEquipe2);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static (string nomJoueur, string nomEquipe) GetNomJoueurEtEquipe(MySqlConnection conn, int idJoueur)
        {
            string sql = @"
        SELECT j.nom_joueur, e.nom_equipe
        FROM joueurs j
        LEFT JOIN equipes e
          ON e.id_equipe = (
                SELECT id_equipe
                FROM equipes
                WHERE id_joueur1 = j.id_joueur
                   OR id_joueur2 = j.id_joueur
                   OR id_joueur3 = j.id_joueur
                   OR id_joueur4 = j.id_joueur
                   OR id_joueur5 = j.id_joueur
                   OR id_joueur6 = j.id_joueur
                   OR id_joueur7 = j.id_joueur
                LIMIT 1
          )
        WHERE j.id_joueur = @id;";

            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", idJoueur);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                        return (null, null);

                    string nomJoueur = r["nom_joueur"]?.ToString();
                    string nomEquipe = r["nom_equipe"]?.ToString();
                    return (nomJoueur, nomEquipe);
                }
            }
        }


        // Disparition des blessures + nouvelles blessures
        private static void GérerBlessuresAprèsMatch(int[] joueurs)
        {
            if (joueurs == null) return;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                Random rnd = new Random();

                foreach (int idJoueur in joueurs)
                {
                    // 1) décrémenter l'ancien compteur si blessé
                    string select = "SELECT id_blessure, matchs_restants_blessure FROM joueurs WHERE id_joueur = @id;";
                    int idBlessure = 0;
                    int restant = 0;

                    using (MySqlCommand cmd = new MySqlCommand(select, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idJoueur);
                        using (MySqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                if (!r.IsDBNull(r.GetOrdinal("id_blessure")))
                                    idBlessure = r.GetInt32("id_blessure");
                                restant = r.GetInt32("matchs_restants_blessure");
                            }
                        }
                    }

                    if (idBlessure != 0)
                    {
                        restant--;
                        if (restant <= 0)
                        {
                            // on retire la blessure
                            string clear = "UPDATE joueurs SET id_blessure = NULL, matchs_restants_blessure = 0 WHERE id_joueur = @id;";
                            using (MySqlCommand cmd = new MySqlCommand(clear, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", idJoueur);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string dec = "UPDATE joueurs SET matchs_restants_blessure = @r WHERE id_joueur = @id;";
                            using (MySqlCommand cmd = new MySqlCommand(dec, conn))
                            {
                                cmd.Parameters.AddWithValue("@r", restant);
                                cmd.Parameters.AddWithValue("@id", idJoueur);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // 2) nouvelle blessure avec une proba (ex : 20%)
                    int chance = rnd.Next(0, 100);
                    if (chance < 20) // 20% de chance
                    {
                        var blessure = TirerBlessureAleatoire(conn, rnd);
                        int nouvelleBlessureId = blessure.idBlessure;
                        string typeBlessure = blessure.typeBlessure;

                        if (nouvelleBlessureId != 0)
                        {
                            string apply = "UPDATE joueurs SET id_blessure = @b, matchs_restants_blessure = 1 WHERE id_joueur = @id;";
                            using (MySqlCommand cmd = new MySqlCommand(apply, conn))
                            {
                                cmd.Parameters.AddWithValue("@b", nouvelleBlessureId);
                                cmd.Parameters.AddWithValue("@id", idJoueur);
                                cmd.ExecuteNonQuery();
                            }

                            var infoJoueur = GetNomJoueurEtEquipe(conn, idJoueur);
                            string nomJoueur = infoJoueur.nomJoueur ?? $"Joueur {idJoueur}";
                            string nomEquipe = infoJoueur.nomEquipe ?? "Sans équipe";

                            Console.WriteLine($"Blessure : {nomJoueur} ({nomEquipe}) a subi \"{typeBlessure}\".");
                        }
                    }
                }
            }
        }

        private static (int idBlessure, string typeBlessure) TirerBlessureAleatoire(MySqlConnection conn, Random rnd)
        {
            string sql = "SELECT id_blessure, type_blessure FROM blessures;";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            using (MySqlDataReader r = cmd.ExecuteReader())
            {
                var liste = new System.Collections.Generic.List<(int, string)>();
                while (r.Read())
                {
                    int id = r.GetInt32("id_blessure");
                    string type = r.GetString("type_blessure");
                    liste.Add((id, type));
                }

                if (liste.Count == 0)
                    return (0, null);

                int index = rnd.Next(0, liste.Count);
                return liste[index];
            }
        }
    }
}
