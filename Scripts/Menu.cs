using System;

namespace Menu_Manager
{
    partial class Menu
    {

        public static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("========================== Bienvenue ! ================================");
            Console.WriteLine("1. Lancer un match");
            Console.WriteLine("2. Voir les joueurs disponibles");
            Console.WriteLine("3. Voir l'historique des matchs");
            Console.WriteLine("4. Voir les équipes");
            Console.WriteLine("5. Voir l'équipe actuel et leurs placements");
            Console.WriteLine("6. Exit");

            Console.Write("Select an option: ");
            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    break;
                case "2":
                    Joueurs_Manager.Joueurs.MenuJoueurs();
                    break;
                case "3":
                    break;
                case "4":
                    Team_Manager.Team.MenuTeams();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    ShowMenu();
                    break;
            }
        }
    }
}