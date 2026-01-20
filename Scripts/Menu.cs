namespace Menu
{
    partial class Menu
    {

        public static void ShowMenu()
        {
            Console.WriteLine("========================== Bienvenue ! ================================");
            Console.WriteLine("1. Lancer un match");
            Console.WriteLine("2. Voir les joueurs disponibles");
            Console.WriteLine("3. Voir l'historique des matchs");
            Console.WriteLine("4. Voir l'équipe actuel et leurs placements");
            Console.WriteLine("5. Exit");

            Console.Write("Select an option: ");
            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    break;
                case "2":
                    Environment.Exit(0);
                    break;
                case "3":
                    break;
                case "4":
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    ShowMenu();
                    break;
            }
        }
    }
}