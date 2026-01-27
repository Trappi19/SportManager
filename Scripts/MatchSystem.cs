using System;
using MySql.Data.MySqlClient;


namespace Match_Manager
{
    internal class MatchSystem
    {
        // Pour mémoriser qui a marqué quoi et quand
        private class ButInfo
        {
            public int IdJoueur { get; set; }
            public int IdEquipe { get; set; }
            public int NumeroMiTemps { get; set; } // 1 ou 2
        }

        private static readonly Random rndGlobal = new Random();

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

            var historiqueButs = new System.Collections.Generic.List<ButInfo>();

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

            // 1ère mi-temps

            // Debug affichage des joueurs
            //Console.WriteLine("\nDEBUG joueurs équipe 1 : " + string.Join(",", joueursEq1));
            //Console.WriteLine("DEBUG joueurs équipe 2 : " + string.Join(",", joueursEq2));

            int score1MT1, score2MT1;

            Console.WriteLine("\n--- 1ère mi-temps ---");
            SaisirEtRepartirButsMiTemps("l'équipe 1", equipe1, joueursEq1, historiqueButs, 1, out score1MT1);
            SaisirEtRepartirButsMiTemps("l'équipe 2", equipe2, joueursEq2, historiqueButs, 1, out score2MT1);

            Console.WriteLine($"\nMi-temps : {score1MT1} - {score2MT1}");
            Console.WriteLine("Appuie sur entrée pour passer à la 2ème mi-temps.");
            Console.ReadLine();

            // 2ème mi-temps
            int score1MT2, score2MT2;

            Console.WriteLine("\n--- 2ème mi-temps ---");
            SaisirEtRepartirButsMiTemps("l'équipe 1", equipe1, joueursEq1, historiqueButs, 2, out score1MT2);
            SaisirEtRepartirButsMiTemps("l'équipe 2", equipe2, joueursEq2, historiqueButs, 2, out score2MT2);


            int scoreFinal1 = score1MT1 + score1MT2;
            int scoreFinal2 = score2MT1 + score2MT2;

            Console.WriteLine($"\nScore final : {scoreFinal1} - {scoreFinal2}\n");

            // Récap détaillé des buteurs
            AfficherRecapButs(historiqueButs, equipe1, equipe2);


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
                                    // pénalité numérique directement appliquée au score général
                                    int penalite = 0;
                                    if (!r.IsDBNull(r.GetOrdinal("pénalité")))
                                    {
                                        // la colonne est un INT en BDD
                                        penalite = Convert.ToInt32(r["pénalité"]);
                                    }
                                    score += penalite; // pénalité est négative
                                }

                                if (score < 0) score = 0;
                                if (score > 10) score = 10; // on reste sur 0-10

                                somme += score;
                                count++;
                            }
                        }
                    }
                }
            }

            if (count == 0) return 0;
            return somme / count; // moyenne sur 10
        }

        // Une mi-temps : baseScore + aléatoire
        //private static int SimulerMiTemps(int scoreEquipe, int scoreAdverse, Random rnd)
        //{
        //    // petit bonus/malus selon écart de niveau
        //    int diff = scoreEquipe - scoreAdverse; // peut être négatif
        //    double facteur = 1.0 + (diff * 0.05);  // 5% par point d'écart
        //    if (facteur < 0.5) facteur = 0.5;

        //    int butsDeBase = rnd.Next(0, 4); // 0 à 3 buts
        //    int buts = (int)(butsDeBase * facteur);

        //    if (buts < 0) buts = 0;
        //    return buts;
        //}

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
        // Retourne la liste des id_joueur de l'équipe qui sont des Poursuiveur
        // Retourne la liste des id_joueur de l'équipe qui sont des Poursuiveur
        private static int[] GetPoursuiveursEquipe(int[] joueurs)
        {
            var liste = new System.Collections.Generic.List<int>();

            string sql = "SELECT affectation_joueur FROM joueurs WHERE id_joueur = @id;";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (int id in joueurs)
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", id);
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            // ICI : on nettoie la valeur et on compare sans tenir compte de la casse
                            string affect = res.ToString().Trim();
                            if (string.Equals(affect, "Poursuiveur", StringComparison.OrdinalIgnoreCase))
                            {
                                liste.Add(id);
                                //Debug Poursuiveur
                                //Console.WriteLine($"DEBUG: joueur {id} est Poursuiveur (valeur='{affect}')");
                            }
                        }
                    }
                }
            }

            // Retourne la collonne des poursuiveurs
            //Console.WriteLine($"DEBUG: {liste.Count} poursuiveur(s) trouvés pour cette équipe.");
            return liste.ToArray();
        }



        // Demande combien de buts a mis l'équipe dans cette mi-temps,
        // puis répartit ces buts entre ses poursuiveurs et affiche le détail.
        // Saisie du nombre de buts de l'équipe pour la mi-temps,
        // répartition aléatoire entre les poursuiveurs,
        // stockage dans l'historique + retour du total marqué.
        private static void SaisirEtRepartirButsMiTemps(
            string labelEquipe,
            int idEquipe,
            int[] joueursEquipe,
            System.Collections.Generic.List<ButInfo> historique,
            int numeroMiTemps,
            out int butsEquipe)
        {
            Console.Write($"Nombre de buts de {labelEquipe} pendant cette mi-temps : ");
            if (!int.TryParse(Console.ReadLine(), out butsEquipe) || butsEquipe < 0)
            {
                Console.WriteLine("Entrée invalide, score mis à 0.");
                butsEquipe = 0;
            }

            // Récup les poursuiveurs de cette équipe
            int[] poursuiveurs = GetPoursuiveursEquipe(joueursEquipe);
            if (poursuiveurs.Length == 0 || butsEquipe == 0)
            {   
                Console.WriteLine($"Aucun but à attribuer pour {labelEquipe}.");
                return;
            }

            int index = rndGlobal.Next(0, poursuiveurs.Length);

            // On attribue chaque but à un poursuiveur aléatoire
            var butsParJoueur = new System.Collections.Generic.Dictionary<int, int>();
            foreach (int idJ in poursuiveurs)
                butsParJoueur[idJ] = 0;

            for (int i = 0; i < butsEquipe; i++)
            {
                index = rndGlobal.Next(0, poursuiveurs.Length);
                int idJ = poursuiveurs[index];

                butsParJoueur[idJ]++;

                // On ajoute une entrée dans l'historique pour ce but
                historique.Add(new ButInfo
                {
                    IdJoueur = idJ,
                    IdEquipe = idEquipe,
                    NumeroMiTemps = numeroMiTemps
                });
            }

            // Petit récap pour cette mi-temps
            Console.WriteLine($"\nRécap des buteurs de {labelEquipe} " +
                              (numeroMiTemps == 1 ? "en 1ère mi-temps :" : "en 2ème mi-temps :"));
            foreach (var kvp in butsParJoueur)
            {
                if (kvp.Value > 0)
                {
                    string nom = GetNomJoueurParId(kvp.Key);
                    Console.WriteLine($" - {nom} : {kvp.Value} but(s)");
                }
            }

            Console.WriteLine();
        }

        // Retourne "NomDuJoueur (id)" à partir d'un id_joueur
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
                        return $"Joueur inconnu ({idJoueur})";

                    return $"{res} ({idJoueur})";
                }
            }
        }
        // Récap général du match : pour chaque équipe, quels joueurs ont marqué, et à quelle mi-temps
        private static void AfficherRecapButs(
            System.Collections.Generic.List<ButInfo> historique,
            int idEquipe1,
            int idEquipe2)
        {
            Console.WriteLine("=== RÉCAPITULATIF DES BUTS ===\n");

            AfficherRecapEquipe(historique, idEquipe1, "Equipe 1");
            Console.WriteLine();
            AfficherRecapEquipe(historique, idEquipe2, "Equipe 2");
            Console.WriteLine();
        }

        // Récap pour une seule équipe
        private static void AfficherRecapEquipe(
            System.Collections.Generic.List<ButInfo> historique,
            int idEquipe,
            string labelEquipe)
        {
            // on regroupe par joueur + mi-temps
            var map = new System.Collections.Generic.Dictionary<(int idJoueur, int mt), int>();

            foreach (var but in historique)
            {
                if (but.IdEquipe != idEquipe) continue;

                var key = (but.IdJoueur, but.NumeroMiTemps);
                if (!map.ContainsKey(key))
                    map[key] = 0;
                map[key]++;
            }

            Console.WriteLine($"--- {labelEquipe} (id {idEquipe}) ---");

            if (map.Count == 0)
            {
                Console.WriteLine("Aucun but marqué.");
                return;
            }

            foreach (var kvp in map)
            {
                int idJoueur = kvp.Key.idJoueur;
                int miTemps = kvp.Key.mt;
                int nbButs = kvp.Value;

                string nom = GetNomJoueurParId(idJoueur);
                string texteMT = miTemps == 1 ? "1ère mi-temps" : "2ème mi-temps";

                Console.WriteLine($" - {nom} : {nbButs} but(s) en {texteMT}");
            }
        }
    }
}
