using System;
using MySql.Data.MySqlClient;

namespace Joueurs_Manager
{
    internal class Joueurs
    {
        private static readonly string connectionString =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        private static MySqlConnection connection;

        public static void MenuJoueurs()
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();

            bool quitter = false;
            while (!quitter)
            {
                Console.Clear();
                Console.WriteLine("=== GESTION DES JOUEURS ===");
                Console.WriteLine("1. Lister les joueurs");
                Console.WriteLine("2. Créer un joueur");
                Console.WriteLine("3. Modifier un joueur");
                Console.WriteLine("4. Supprimer un joueur");
                Console.WriteLine("5. Retour");
                Console.Write("Ton choix : ");
                string choix = Console.ReadLine();

                switch (choix)
                {
                    case "1": ListerJoueurs(); break;
                    case "2": CreerJoueur(); break;
                    case "3": ModifierJoueur(); break;
                    case "4": SupprimerJoueur(); break;
                    case "5": quitter = true; Menu_Manager.Menu.ShowMenu(); break;
                    default:
                        Console.WriteLine("Choix invalide, entrée pour continuer.");
                        Console.ReadLine();
                        break;
                }
            }

            connection.Close();
        }

        private static void ListerJoueurs()
        {
            Console.Clear();
            Console.WriteLine("=== LISTE DES JOUEURS ===\n");

            string sql = @"
        SELECT j.id_joueur,
               j.nom_joueur,
               j.score_defense,
               j.score_attaque,
               j.score_goal,
               j.score_general,
               j.affectation_joueur,
               b.type_blessure,
               b.pénalité
        FROM joueurs j
        LEFT JOIN blessures b ON j.id_blessure = b.id_blessure
        ORDER BY j.nom_joueur;";

            using (MySqlCommand cmd = new MySqlCommand(sql, connection))
            using (MySqlDataReader r = cmd.ExecuteReader())
            {
                Console.WriteLine("ID | Nom | Def | Att | Goal | General | Affectation     | Blessure (pénalité)");
                Console.WriteLine(new string('-', 115));

                while (r.Read())
                {
                    string blessure;
                    if (r["type_blessure"] == DBNull.Value)
                    {
                        blessure = "Aucune";
                    }
                    else
                    {
                        string type = r["type_blessure"].ToString();
                        int pen = Convert.ToInt32(r["pénalité"]);
                        blessure = $"{type} ({pen})";
                    }

                    Console.WriteLine(
                        $"{r["id_joueur"],2} | {r["nom_joueur"],-20} | " +
                        $"{r["score_defense"],3} | {r["score_attaque"],3} | " +
                        $"{r["score_goal"],3} | {r["score_general"],3} | " +
                        $"{r["affectation_joueur"],-15} | {blessure}");
                }
            }

            Console.WriteLine("\nEntrée pour revenir au menu.");
            Console.ReadLine();
        }



        private static void CreerJoueur()
        {
            Console.Clear();
            Console.WriteLine("=== CREATION D'UN JOUEUR ===\n");

            Console.Write("Nom du joueur : ");
            string nom = Console.ReadLine();

            int scoreDef = LireScoreSur10("Score défense / 10");
            int scoreAtt = LireScoreSur10("Score attaque / 10");
            int scoreGoal = LireScoreSur10("Score goal / 10");

            int scoreGen = (scoreDef + scoreAtt + scoreGoal) / 3;
            Console.WriteLine($"Score général calculé : {scoreGen}");

            Console.Write("Affectation (Avec une majuscule) : ");
            string affectation = Console.ReadLine();


        string sql = @"
                INSERT INTO joueurs
                    (nom_joueur, score_defense, score_attaque,
                     score_goal, score_general, affectation_joueur)
                VALUES
                    (@nom, @def, @att, @goal, @gen, @affect);";

            using (MySqlCommand cmd = new MySqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@def", scoreDef);
                cmd.Parameters.AddWithValue("@att", scoreAtt);
                cmd.Parameters.AddWithValue("@goal", scoreGoal);
                cmd.Parameters.AddWithValue("@gen", scoreGen);
                cmd.Parameters.AddWithValue("@affect", affectation);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("\nJoueur créé ! Entrée pour continuer.");
            Console.ReadLine();
        }

        private static void ModifierJoueur()
        {
            Console.Clear();
            Console.WriteLine("=== MODIFICATION D'UN JOUEUR ===\n");
            Console.Write("ID du joueur à modifier : ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            string select = @"
        SELECT nom_joueur, score_defense, score_attaque,
               score_goal, score_general, affectation_joueur
        FROM joueurs WHERE id_joueur = @id;";

            string nom = "";
            int def = 0, att = 0, goal = 0, gen = 0;
            string affect = "";

            using (MySqlCommand cmd = new MySqlCommand(select, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                    {
                        Console.WriteLine("Aucun joueur avec cet ID. Entrée pour continuer.");
                        Console.ReadLine();
                        return;
                    }

                    nom = r.GetString("nom_joueur");
                    def = r.GetInt32("score_defense");
                    att = r.GetInt32("score_attaque");
                    goal = r.GetInt32("score_goal");
                    gen = r.GetInt32("score_general");
                    affect = r.GetString("affectation_joueur");
                }
            }

            Console.WriteLine($"\nNom actuel : {nom}");
            Console.Write("Nouveau nom (laisser vide pour garder) : ");
            string newNom = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newNom)) nom = newNom;

            Console.WriteLine($"Score défense actuel : {def}");
            def = LireScoreSur10("Nouveau score défense", def);

            Console.WriteLine($"Score attaque actuel : {att}");
            att = LireScoreSur10("Nouveau score attaque", att);

            Console.WriteLine($"Score goal actuel : {goal}");
            goal = LireScoreSur10("Nouveau score goal", goal);

            // Recalcul auto du score général
            gen = (def + att + goal) / 3;
            Console.WriteLine($"Nouveau score général recalculé : {gen}");

            Console.WriteLine($"Affectation actuelle : {affect}");
            Console.Write("Nouvelle affectation (vide pour garder) : ");
            string newAffect = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newAffect)) affect = newAffect;

            string update = @"
        UPDATE joueurs
        SET nom_joueur = @nom,
            score_defense = @def,
            score_attaque = @att,
            score_goal = @goal,
            score_general = @gen,
            affectation_joueur = @affect
        WHERE id_joueur = @id;";

            using (MySqlCommand cmd = new MySqlCommand(update, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@def", def);
                cmd.Parameters.AddWithValue("@att", att);
                cmd.Parameters.AddWithValue("@goal", goal);
                cmd.Parameters.AddWithValue("@gen", gen);
                cmd.Parameters.AddWithValue("@affect", affect);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("\nJoueur modifié ! Entrée pour continuer.");
            Console.ReadLine();
        }


        private static void SupprimerJoueur()
        {
            Console.Clear();
            Console.WriteLine("=== SUPPRESSION D'UN JOUEUR ===\n");
            Console.Write("ID du joueur à supprimer : ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            string sql = "DELETE FROM joueurs WHERE id_joueur = @id;";
            using (MySqlCommand cmd = new MySqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine(rows > 0
                    ? "Joueur supprimé."
                    : "Aucun joueur avec cet ID.");
            }

            Console.WriteLine("Entrée pour continuer.");
            Console.ReadLine();
        }
        private static int LireScoreSur10(string label, int valeurDefaut = 0)
        {
            while (true)
            {
                Console.Write($"{label} (0-10, vide = {valeurDefaut}) : ");
                string saisie = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(saisie))
                    return valeurDefaut;

                if (!int.TryParse(saisie, out int valeur))
                {
                    Console.WriteLine("Ce n'est pas un nombre, recommence.");
                    continue;
                }

                if (valeur < 0 || valeur > 10)
                {
                    Console.WriteLine("Le score doit être entre 0 et 10, recommence.");
                    continue;
                }

                return valeur;
            }
        }

    }
}
