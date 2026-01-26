using System;
using MySql.Data.MySqlClient;

namespace Team_Manager
{
    internal class Team
    {
        // Chaîne de connexion à ta BDD
        private static readonly string connectionString =
            "Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;";

        private static MySqlConnection connection;

        // Menu principal de gestion des équipes
        public static void MenuTeams()
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();

            bool quitter = false;
            while (!quitter)
            {
                Console.Clear();
                Console.WriteLine("=== GESTION DES EQUIPES ===");
                Console.WriteLine("1. Lister les équipes");
                Console.WriteLine("2. Créer une équipe");
                Console.WriteLine("3. Modifier une équipe");
                Console.WriteLine("4. Supprimer une équipe");
                Console.WriteLine("5. Retour");
                Console.Write("Ton choix : ");
                string choix = Console.ReadLine();

                switch (choix)
                {
                    case "1": ListerEquipes(); break;
                    case "2": CreerEquipe(); break;
                    case "3": ModifierEquipe(); break;
                    case "4": SupprimerEquipe(); break;
                    case "5": quitter = true; Menu_Manager.Menu.ShowMenu(); break;
                    default:
                        Console.WriteLine("Choix invalide, entrée pour continuer.");
                        Console.ReadLine();
                        break;
                }
            }

            connection.Close();
        }

        // Affiche toutes les équipes, avec leurs joueurs (Nom + id)
        private static void ListerEquipes()
        {
            Console.Clear();
            Console.WriteLine("=== LISTE DES EQUIPES ===\n");

            string sql = @"
        SELECT id_equipe, nom_equipe, score_general,
               id_joueur1, id_joueur2, id_joueur3,
               id_joueur4, id_joueur5, id_joueur6, id_joueur7
        FROM equipes
        ORDER BY nom_equipe;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    Console.WriteLine("ID | Nom équipe         | Score | Joueurs");
                    Console.WriteLine(new string('-', 120));

                    while (r.Read())
                    {
                        // Récupère les ids des 7 joueurs
                        int j1 = r.GetInt32("id_joueur1");
                        int j2 = r.GetInt32("id_joueur2");
                        int j3 = r.GetInt32("id_joueur3");
                        int j4 = r.GetInt32("id_joueur4");
                        int j5 = r.GetInt32("id_joueur5");
                        int j6 = r.GetInt32("id_joueur6");
                        int j7 = r.GetInt32("id_joueur7");

                        // Convertit id -> "Nom (id)"
                        string sJ1 = GetNomJoueurParId(j1);
                        string sJ2 = GetNomJoueurParId(j2);
                        string sJ3 = GetNomJoueurParId(j3);
                        string sJ4 = GetNomJoueurParId(j4);
                        string sJ5 = GetNomJoueurParId(j5);
                        string sJ6 = GetNomJoueurParId(j6);
                        string sJ7 = GetNomJoueurParId(j7);

                        Console.WriteLine(
                            $"{r["id_equipe"],2} | {r["nom_equipe"],-18} | {r["score_general"],3}/10 | " +
                            $"{sJ1}, {sJ2}, {sJ3}, {sJ4}, {sJ5}, {sJ6}, {sJ7}");
                    }
                }
            }

            Console.WriteLine("\nEntrée pour revenir au menu.");
            Console.ReadLine();
        }

        // Création d'une nouvelle équipe (7 joueurs avec les bons postes)
        private static void CreerEquipe()
        {
            Console.Clear();
            Console.WriteLine("=== CREATION D'UNE EQUIPE ===\n");

            Console.Write("Nom de l'équipe : ");
            string nom = Console.ReadLine();

            Console.WriteLine("\nOn va choisir les 7 joueurs :");
            Console.WriteLine("- 3 poursuiveurs");
            Console.WriteLine("- 2 batteurs");
            Console.WriteLine("- 1 gardien");
            Console.WriteLine("- 1 attrapeur\n");

            int[] joueurs = new int[7];

            int nbPoursuiveurs = 0;
            int nbBatteurs = 0;
            int nbGardiens = 0;
            int nbAttrapeurs = 0;

            // On boucle pour remplir les 7 slots
            for (int i = 0; i < 7; i++)
            {
                bool ok = false;
                while (!ok)
                {
                    Console.Write($"ID joueur {i + 1} : ");
                    string saisie = Console.ReadLine();
                    if (!int.TryParse(saisie, out int idJoueur))
                    {
                        Console.WriteLine("ID invalide, recommence.");
                        continue;
                    }

                    // 1) Vérif doublon dans l'équipe
                    bool dejaPris = false;
                    for (int k = 0; k < i; k++)
                    {
                        if (joueurs[k] == idJoueur)
                        {
                            dejaPris = true;
                            break;
                        }
                    }
                    if (dejaPris)
                    {
                        Console.WriteLine("Ce joueur est déjà dans l'équipe, choisis-en un autre.");
                        continue;
                    }

                    // 2) Récup poste + nom
                    var info = GetPosteEtNomJoueur(idJoueur);
                    string poste = info.poste;
                    string nomJoueur = info.nom;

                    if (poste == null)
                    {
                        Console.WriteLine("Aucun joueur trouvé avec cet ID, recommence.");
                        continue;
                    }

                    // 3) Compte combien on a de chaque poste
                    switch (poste)
                    {
                        case "Poursuiveur":
                            if (nbPoursuiveurs >= 3)
                            {
                                Console.WriteLine("Tu as déjà 3 poursuiveurs, prends un autre poste.");
                                continue;
                            }
                            nbPoursuiveurs++;
                            ok = true;
                            break;

                        case "Batteur":
                            if (nbBatteurs >= 2)
                            {
                                Console.WriteLine("Tu as déjà 2 batteurs, prends un autre poste.");
                                continue;
                            }
                            nbBatteurs++;
                            ok = true;
                            break;

                        case "Gardien":
                            if (nbGardiens >= 1)
                            {
                                Console.WriteLine("Tu as déjà un gardien, prends un autre poste.");
                                continue;
                            }
                            nbGardiens++;
                            ok = true;
                            break;

                        case "Attrapeur":
                            if (nbAttrapeurs >= 1)
                            {
                                Console.WriteLine("Tu as déjà un attrapeur, prends un autre poste.");
                                continue;
                            }
                            nbAttrapeurs++;
                            ok = true;
                            break;

                        default:
                            Console.WriteLine($"Poste '{poste}' non valide pour le quidditch, choisis un autre joueur.");
                            break;
                    }

                    if (ok)
                    {
                        joueurs[i] = idJoueur;
                        Console.WriteLine($"OK, {nomJoueur} ({poste}) ajouté.");
                    }
                }
            }

            // Score général = moyenne des scores généraux des joueurs
            int scoreGeneral = CalculerScoreMoyenEquipe(joueurs);

            string sql = @"
        INSERT INTO equipes
            (nom_equipe, id_joueur1, id_joueur2, id_joueur3,
             id_joueur4, id_joueur5, id_joueur6, id_joueur7, score_general)
        VALUES
            (@nom, @j1, @j2, @j3, @j4, @j5, @j6, @j7, @score);";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@j1", joueurs[0]);
                    cmd.Parameters.AddWithValue("@j2", joueurs[1]);
                    cmd.Parameters.AddWithValue("@j3", joueurs[2]);
                    cmd.Parameters.AddWithValue("@j4", joueurs[3]);
                    cmd.Parameters.AddWithValue("@j5", joueurs[4]);
                    cmd.Parameters.AddWithValue("@j6", joueurs[5]);
                    cmd.Parameters.AddWithValue("@j7", joueurs[6]);
                    cmd.Parameters.AddWithValue("@score", scoreGeneral);
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"\nEquipe créée avec score général : {scoreGeneral}/10");
            Console.WriteLine("Entrée pour continuer.");
            Console.ReadLine();
        }

        // Modification d'une équipe existante
        public static void ModifierEquipe()
        {
            Console.Clear();
            Console.WriteLine("=== MODIFICATION D'UNE EQUIPE ===\n");
            Console.Write("ID de l'équipe à modifier : ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            string select = @"
        SELECT nom_equipe, id_joueur1, id_joueur2, id_joueur3,
               id_joueur4, id_joueur5, id_joueur6, id_joueur7
        FROM equipes WHERE id_equipe = @id;";

            string nom = "";
            int[] joueurs = new int[7];

            // Récupère l'équipe actuelle
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(select, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            Console.WriteLine("Aucune équipe avec cet ID. Entrée pour continuer.");
                            Console.ReadLine();
                            return;
                        }

                        nom = r.GetString("nom_equipe");
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

            Console.WriteLine($"\nNom actuel : {nom}");
            Console.Write("Nouveau nom (vide pour garder) : ");
            string newNom = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newNom)) nom = newNom;

            // Affiche la compo actuelle avec Nom (id)
            Console.WriteLine("\nActuellement joueurs :");
            for (int i = 0; i < 7; i++)
            {
                var info = GetPosteEtNomJoueur(joueurs[i]);
                string nomJoueur = info.nom ?? "Inconnu";
                Console.WriteLine($" - Joueur {i + 1} : {nomJoueur} ({joueurs[i]})");
            }
            Console.WriteLine("\nOn doit garder : 3 poursuiveurs, 2 batteurs, 1 gardien, 1 attrapeur.");

            int[] nouveauxJoueurs = new int[7];

            int nbPoursuiveurs = 0;
            int nbBatteurs = 0;
            int nbGardiens = 0;
            int nbAttrapeurs = 0;

            // Même logique que création, mais on propose de garder le joueur actuel si on laisse vide
            for (int i = 0; i < 7; i++)
            {
                bool ok = false;
                while (!ok)
                {
                    var infoActuel = GetPosteEtNomJoueur(joueurs[i]);
                    string nomActuel = infoActuel.nom ?? "Inconnu";

                    Console.Write($"ID joueur {i + 1} (ancien {nomActuel} ({joueurs[i]})) : ");
                    string saisie = Console.ReadLine();

                    int idJoueur;
                    if (string.IsNullOrWhiteSpace(saisie))
                    {
                        idJoueur = joueurs[i]; // garde l'ancien
                    }
                    else if (!int.TryParse(saisie, out idJoueur))
                    {
                        Console.WriteLine("ID invalide, recommence.");
                        continue;
                    }

                    bool dejaPris = false;
                    for (int k = 0; k < i; k++)
                    {
                        if (nouveauxJoueurs[k] == idJoueur)
                        {
                            dejaPris = true;
                            break;
                        }
                    }
                    if (dejaPris)
                    {
                        Console.WriteLine("Ce joueur est déjà dans l'équipe, choisis-en un autre.");
                        continue;
                    }

                    var info2 = GetPosteEtNomJoueur(idJoueur);
                    string poste = info2.poste;
                    string nomJoueur2 = info2.nom;

                    if (poste == null)
                    {
                        Console.WriteLine("Aucun joueur trouvé avec cet ID, recommence.");
                        continue;
                    }

                    switch (poste)
                    {
                        case "Poursuiveur":
                            if (nbPoursuiveurs >= 3)
                            {
                                Console.WriteLine("Tu as déjà 3 poursuiveurs, prends un autre poste.");
                                continue;
                            }
                            nbPoursuiveurs++;
                            ok = true;
                            break;

                        case "Batteur":
                            if (nbBatteurs >= 2)
                            {
                                Console.WriteLine("Tu as déjà 2 batteurs, prends un autre poste.");
                                continue;
                            }
                            nbBatteurs++;
                            ok = true;
                            break;

                        case "Gardien":
                            if (nbGardiens >= 1)
                            {
                                Console.WriteLine("Tu as déjà un gardien, prends un autre poste.");
                                continue;
                            }
                            nbGardiens++;
                            ok = true;
                            break;

                        case "Attrapeur":
                            if (nbAttrapeurs >= 1)
                            {
                                Console.WriteLine("Tu as déjà un attrapeur, prends un autre poste.");
                                continue;
                            }
                            nbAttrapeurs++;
                            ok = true;
                            break;

                        default:
                            Console.WriteLine($"Poste '{poste}' non valide pour le quidditch, choisis un autre joueur.");
                            break;
                    }

                    if (ok)
                    {
                        nouveauxJoueurs[i] = idJoueur;
                        Console.WriteLine($"OK, {nomJoueur2} ({poste}) ajouté.");
                    }
                }
            }

            int scoreGeneral = CalculerScoreMoyenEquipe(nouveauxJoueurs);

            string update = @"
        UPDATE equipes SET
            nom_equipe = @nom,
            id_joueur1 = @j1, id_joueur2 = @j2, id_joueur3 = @j3,
            id_joueur4 = @j4, id_joueur5 = @j5, id_joueur6 = @j6, id_joueur7 = @j7,
            score_general = @score
        WHERE id_equipe = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(update, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@j1", nouveauxJoueurs[0]);
                    cmd.Parameters.AddWithValue("@j2", nouveauxJoueurs[1]);
                    cmd.Parameters.AddWithValue("@j3", nouveauxJoueurs[2]);
                    cmd.Parameters.AddWithValue("@j4", nouveauxJoueurs[3]);
                    cmd.Parameters.AddWithValue("@j5", nouveauxJoueurs[4]);
                    cmd.Parameters.AddWithValue("@j6", nouveauxJoueurs[5]);
                    cmd.Parameters.AddWithValue("@j7", nouveauxJoueurs[6]);
                    cmd.Parameters.AddWithValue("@score", scoreGeneral);
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"\nEquipe modifiée, nouveau score général : {scoreGeneral}/10");
            Console.WriteLine("Entrée pour continuer.");
            Console.ReadLine();
        }

        // Suppression simple d'une équipe
        private static void SupprimerEquipe()
        {
            Console.Clear();
            Console.WriteLine("=== SUPPRESSION D'UNE EQUIPE ===\n");
            Console.Write("ID de l'équipe à supprimer : ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            string sql = "DELETE FROM equipes WHERE id_equipe = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Equipe supprimée." : "Aucune équipe avec cet ID.");
                }
            }

            Console.WriteLine("Entrée pour continuer.");
            Console.ReadLine();
        }

        // Calcule le score général d'une équipe = moyenne des scores généraux des joueurs
        private static int CalculerScoreMoyenEquipe(int[] joueurs)
        {
            if (joueurs == null || joueurs.Length != 7) return 0;

            int somme = 0;
            int count = 0;

            string sql = "SELECT score_general FROM joueurs WHERE id_joueur = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (int id in joueurs)
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            somme += Convert.ToInt32(res);
                            count++;
                        }
                    }
                }
            }

            if (count == 0) return 0;

            int moyenne = somme / count;
            return moyenne;
        }

        // Retourne (poste, nom) d'un joueur à partir de son id
        public static (string poste, string nom) GetPosteEtNomJoueur(int idJoueur)
        {
            string sql = "SELECT affectation_joueur, nom_joueur FROM joueurs WHERE id_joueur = @id;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idJoueur);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            return (null, null);

                        string poste = r["affectation_joueur"]?.ToString();
                        string nom = r["nom_joueur"]?.ToString();
                        return (poste, nom);
                    }
                }
            }
        }

        // Retourne un string "Nom (id)" pour l'affichage
        private static string GetNomJoueurParId(int idJoueur)
        {
            string sql = "SELECT nom_joueur FROM joueurs WHERE id_joueur = @id;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idJoueur);
                    object res = cmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                        return $"Inconnu ({idJoueur})";
                    return $"{res} ({idJoueur})";
                }
            }
        }
    }
}
