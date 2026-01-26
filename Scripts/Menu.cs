using System;

namespace Menu_Manager
{
    partial class Menu
    {

        public static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("========================== Bienvenue ! ================================");
            Console.WriteLine("1. Match");
            Console.WriteLine("2. Joueurs disponibles");
            Console.WriteLine("3. Equipes");
            Console.WriteLine("4. Exit");

            Console.Write("Select an option: ");
            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Match_Manager.MatchSystem.MenuMatchs();
                    break;
                case "2":
                    Joueurs_Manager.Joueurs.MenuJoueurs();
                    break;
                case "3":
                    Team_Manager.Team.MenuTeams();
                    break;
                case "4":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    ShowMenu();
                    break;
            }
        }
    }
}