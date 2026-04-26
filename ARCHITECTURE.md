# Architecture du projet

## Vue d'ensemble

- `SportManager/` contient l'application WPF active, organisée par domaine.
- `SportManager/Models/` regroupe les objets métier (`Joueur`, `Equipe`, `MatchResult`, `Blessure`, `EvalJoueur`).
- `SportManager/Services/` contient l'accès aux données (`DatabaseService` - MySQL).
- `SportManager/Views/` regroupe toutes les interfaces utilisateur XAML :
  - `Views/Dialogs/` : Fenêtres de dialogue pour créer/éditer des joueurs et équipes
  - `Views/Windows/` : Panneaux principaux (Joueurs, Équipes, Matchs)
- `SportManager/Resources/` contient les assets UI (images, etc.).
- `Legacy/Console/` contient l'ancienne version console, conservée uniquement comme archive (exclue du build WPF).
- `csv/` et `saves/` contiennent des données de travail et des exports.

## Ce qui est important pour développer

- L'écran principal WPF est dans `SportManager/MainWindow.xaml.cs`.
- Les interfaces utilisateur sont organisées logiquement dans `Views/Dialogs/` et `Views/Windows/`.
- La logique métier est centralisée dans `DatabaseService` (base de données MySQL) et les modèles dans `Models/`.
- Tous les namespaces des views reflètent leur structure : `SportManager.Views.Dialogs.*` et `SportManager.Views.Windows.*`

## Ce qui peut être ignoré au quotidien

- Les dossiers `bin/` et `obj/` sont générés automatiquement par .NET.
- Les fichiers de la version console n'existent que dans `Legacy/Console/` et ne participent plus au build WPF.
- `Scripts/` est actuellement vide et peut être utilisé pour des utilitaires.

## Structure du projet consolidé

```
SportManager/
├── SportManager.csproj          (UN SEUL fichier projet WPF)
├── MainWindow.xaml/cs           (Entrée principale)
├── App.xaml/cs                  (Ressources et styles partagés)
├── GlobalUsings.cs              (Imports globaux)
├── Models/
│   ├── Joueur.cs
│   ├── Equipe.cs
│   ├── MatchResult.cs
│   ├── Blessure.cs
│   └── EvalJoueur.cs
├── Services/
│   └── DatabaseService.cs       (Accès MySQL)
├── Views/
│   ├── Dialogs/
│   │   ├── CreatePlayerDialog.xaml/cs
│   │   ├── EditPlayerDialog.xaml/cs
│   │   ├── CreateTeamDialog.xaml/cs
│   │   └── EditTeamDialog.xaml/cs
│   └── Windows/
│       ├── EquipesWindow.xaml/cs
│       ├── JoueursWindow.xaml/cs
│       └── MatchWindow.xaml/cs
├── Resources/
│   └── background.png
├── Scripts/                     (Vide - utilitaires futurs)
├── bin/                         (Auto-généré)
└── obj/                         (Auto-généré)

Legacy/
└── Console/
    ├── Program.cs
    ├── Menu.cs
    ├── MatchSystem.cs
    ├── HistoricSystem.cs
    ├── Team.cs
    ├── Joueurs.cs
    └── README.md
```