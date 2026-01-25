using System;
using MySql.Data.MySqlClient;

namespace Historic_Manager
{
    internal class HistoricSystem
    {
        private static readonly string connectionString =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        public static void MenuHistorique()
        {
            bool quitter = false;
            while (!quitter)
            {
                Console.Clear();
                Console.WriteLine("=== HISTORIQUE DES MATCHS ===");
                Console.WriteLine("1. Lister tous les matchs");
                Console.WriteLine("2. Retour");
                Console.Write("Ton choix : ");
                string choix = Console.ReadLine();

                switch (choix)
                {
                    case "1":
                        ListerMatchs();
                        break;
                    case "2":
                        quitter = true;
                        break;
                    default:
                        Console.WriteLine("Mauvais choix, entrée pour continuer.");
                        Console.ReadLine();
                        break;
                }
            }
        }

        private static void ListerMatchs()
        {
            Console.Clear();
            Console.WriteLine("=== LISTE DES MATCHS ===\n");

            string sql = @"
                SELECT m.id_match,
                       m.date_match,
                       e1.nom_equipe AS equipe1,
                       e2.nom_equipe AS equipe2,
                       m.score_equipe1,
                       m.score_equipe2
                FROM matchs m
                LEFT JOIN equipes e1 ON m.id_equipe1 = e1.id_equipe
                LEFT JOIN equipes e2 ON m.id_equipe2 = e2.id_equipe
                ORDER BY m.date_match DESC, m.id_match DESC;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    Console.WriteLine("ID | Date / Heure         | Equipe 1 (score)        vs       Equipe 2 (score)");
                    Console.WriteLine(new string('-', 90));

                    while (r.Read())
                    {
                        int idMatch = r.GetInt32("id_match");
                        DateTime date = r.GetDateTime("date_match");

                        string equipe1 = r["equipe1"]?.ToString() ?? "Inconnue";
                        string equipe2 = r["equipe2"]?.ToString() ?? "Inconnue";

                        int s1 = r.GetInt32("score_equipe1");
                        int s2 = r.GetInt32("score_equipe2");

                        Console.WriteLine(
                            $"{idMatch,2} | {date:dd/MM/yyyy HH:mm} | " +
                            $"{equipe1} ({s1})  vs  {equipe2} ({s2})");
                    }
                }
            }

            Console.WriteLine("\nEntrée pour revenir au menu.");
            Console.ReadLine();
        }
    }
}
