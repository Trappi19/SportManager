# SportManager

Application de gestion d'equipes et de matchs (univers Quidditch) developpee en C# avec WPF.

Le projet permet de:
- gerer les joueurs (creation, modification, suppression)
- gerer les equipes (selection de 7 joueurs, score global)
- simuler ou saisir manuellement des matchs
- enregistrer les buts, blessures et resultats en base MySQL
- consulter les statistiques globales depuis l'accueil

## Technologies utilisees

- C# / .NET 10 (TargetFramework: net10.0-windows)
- WPF (XAML) pour l'interface utilisateur desktop
- MySQL comme base de donnees
- Dapper (micro-ORM) pour l'acces SQL
- MySql.Data comme driver de connexion MySQL

## Structure du projet

- `SportManager/` : application WPF active
- `SportManager/Models/` : modeles metier (`Joueur`, `Equipe`, `MatchResult`, etc.)
- `SportManager/Services/` : acces donnees et logique DB (`DatabaseService`, `DatabaseInitializer`)
- `SportManager/Views/Windows/` : ecrans principaux (`JoueursWindow`, `EquipesWindow`, `MatchWindow`)
- `SportManager/Views/Dialogs/` : boites de dialogue de creation/edition
- `Legacy/Console/` : ancienne version console (archive, non compilee dans l'app WPF)
- `csv/` : fichiers de donnees de travail
- `saves/` : exports/sauvegardes

## Fonctionnement general

### 1) Demarrage

L'application demarre sur `MainWindow`.

Au lancement:
- chargement de l'UI et du fond
- execution de migrations de donnees (si necessaire)
- affichage des compteurs (joueurs, equipes, matchs)
- chargement du dernier match

### 2) Module Joueurs

Permet de:
- creer un joueur avec ses statistiques
- modifier un joueur
- supprimer un joueur
- suivre les attributs (attaque, defense, vitesse, endurance, score global)
- gerer l'affectation (poste)

### 3) Module Equipes

Permet de:
- creer une equipe a partir de 7 joueurs
- recalculer automatiquement le score general de l'equipe
- modifier/supprimer une equipe

### 4) Module Matchs

Deux modes:
- simulation automatique
- saisie manuelle

Lors d'une simulation:
- calcul des probabilites selon les scores effectifs (avec blessures)
- generation des scores par mi-temps
- attribution des buts a des joueurs
- gestion des blessures de match

Lors de la sauvegarde:
- insertion du match
- insertion du detail des buts (`buts_match`)
- insertion du detail des blessures (`blessures_match`)
- mise a jour des stats des joueurs

## Base de donnees

La base cible est `sportmanager` sur MySQL.

Chaine de connexion actuellement codee en dur dans les services:

```text
Server=localhost;Database=sportmanager;Uid=root;Pwd=rootroot;
```

### Initialisation du schema

Le projet contient une classe `DatabaseInitializer` qui:
- cree la base si absente
- cree les tables (`joueurs`, `equipes`, `matchs`, `blessures`, tables de detail)
- insere les blessures de reference

Important:
- cette initialisation n'est pas branchee automatiquement au demarrage actuel
- les migrations de stats (`MigrateToV2`, `MigrateRandomStats`) sont, elles, executees au lancement de `MainWindow`

## Prerequis

- Windows (application WPF)
- .NET SDK 10 (ou version compatible avec `net10.0-windows`)
- Serveur MySQL local accessible

## Installation et lancement

1. Cloner le depot:

```bash
git clone https://github.com/Trappi19/SportManager.git
cd SportManager
```

2. Restaurer les dependances:

```bash
dotnet restore
```

3. Verifier la configuration MySQL (utilisateur/mot de passe) dans:
- `SportManager/Services/DatabaseService.cs`
- `SportManager/Services/DatabaseInitializer.cs`

4. Initialiser la base (si elle n'existe pas encore):
- soit en appelant temporairement `new DatabaseInitializer().Initialize();` au demarrage
- soit en creant le schema manuellement via SQL equivalent

5. Lancer l'application:

```bash
dotnet run --project SportManager.csproj
```

## Notes importantes

- Le chemin de l'image de fond est absolu dans `MainWindow.xaml.cs`.
  Si le projet est deplace sur un autre poste, il faut adapter ce chemin.
- Le dossier `Legacy/Console/` est conserve pour historique et n'est pas utilise par la version WPF.

## Auteur

Projet realise dans le cadre du cursus CESI.