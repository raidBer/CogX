# ?? CogX - Plateforme de Jeux Multijoueurs en Temps Réel

Projet .NET 10 avec SignalR - TP .NET M2 GIL 2025-2026

---

## ?? Jeux Implémentés

- **Morpion (Tic-Tac-Toe)** - 2 joueurs, grille 3x3
- **Speed Typing** - Course de dactylographie en temps réel
- **Puissance 4 (Connect Four)** - 2 joueurs, grille 6x7

---

## ??? Technologies

- **.NET 10**
- **Entity Framework Core 10.0**
- **SignalR** (temps réel)
- **SQL Server LocalDB**
- **Swagger/OpenAPI**

---

## ? Installation Rapide

### Prérequis

- **Visual Studio 2022** (ou Visual Studio Code + .NET SDK 10)
- **SQL Server LocalDB** (inclus avec Visual Studio)

### Étapes

1. **Cloner le repository**
```bash
git clone https://github.com/raidBer/CogX.git
cd CogX
```

2. **Ouvrir le projet**
   - Double-cliquez sur `CogX.sln` (Visual Studio)
   - Ou ouvrez le dossier dans VS Code

3. **Restaurer les packages NuGet**
```bash
dotnet restore
```

4. **Créer la base de données**
```bash
cd CogX
dotnet ef database update
```

5. **Lancer l'application**
   - Appuyez sur **F5** dans Visual Studio
   - Ou en ligne de commande :
```bash
dotnet run --project CogX/CogX.csproj
```

6. **Accéder à Swagger**
```
https://localhost:7049/swagger
```

---

## ?? Tester l'API

### Option 1 : Swagger UI (Recommandé)

1. Ouvrez `https://localhost:7049/swagger`
2. Testez les endpoints dans l'interface graphique

### Option 2 : Fichier Tests.http

1. Ouvrez `CogX/Tests.http` dans Visual Studio
2. Cliquez sur "Send Request" au-dessus de chaque requête

### Option 3 : Ligne de commande

```bash
# Créer un joueur
curl -X POST https://localhost:7049/api/player \
  -H "Content-Type: application/json" \
  -d '{"pseudo":"Alice"}'

# Voir les lobbies
curl https://localhost:7049/api/lobby
```

---

## ?? Endpoints Principaux

### API REST

```
POST   /api/player                    - Créer un joueur
GET    /api/player/{id}               - Récupérer un joueur

GET    /api/lobby                     - Lister les lobbies publics
POST   /api/lobby                     - Créer un lobby
POST   /api/lobby/{id}/join           - Rejoindre un lobby
POST   /api/lobby/{id}/start          - Démarrer la partie (host)

GET    /api/leaderboard/{gameType}    - Voir le classement
POST   /api/leaderboard               - Ajouter un score

GET    /api/admin/game-sessions       - Voir toutes les sessions
GET    /api/admin/game-history/{id}   - Historique d'une partie
GET    /api/admin/platform-stats      - Statistiques globales
```

### SignalR Hubs

```
wss://localhost:7049/lobbyhub          - Gestion des lobbies
wss://localhost:7049/tictactoehub      - Morpion
wss://localhost:7049/speedtypinghub    - Speed Typing
wss://localhost:7049/connect4hub       - Puissance 4
```

---

## ??? Base de Données

### Connexion

- **Serveur :** `(localdb)\mssqllocaldb`
- **Base :** `CogXDb`
- **Auth :** Windows Authentication

### Visualiser avec SSMS

1. Téléchargez [SQL Server Management Studio](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
2. Connectez-vous à `(localdb)\mssqllocaldb`
3. Ouvrez la base `CogXDb`

### Explorer avec Visual Studio

1. **View ? Server Explorer** (Ctrl+Alt+S)
2. Clic droit **Data Connections ? Add Connection**
3. Server: `(localdb)\mssqllocaldb`, Database: `CogXDb`

### Script SQL d'exploration

Exécutez `CogX/Database_Exploration.sql` dans SSMS pour voir toutes les statistiques.

---

## ?? Workflow Typique

### 1. Créer des joueurs

```http
POST /api/player
{
  "pseudo": "Alice"
}
```

### 2. Créer un lobby

```http
POST /api/lobby
{
  "playerId": "alice-id",
  "gameType": "Morpion",
  "maxPlayers": 2,
  "password": null
}
```

### 3. Un autre joueur rejoint

```http
POST /api/lobby/{lobbyId}/join
{
  "playerId": "bob-id",
  "password": null
}
```

### 4. Le host démarre la partie

```http
POST /api/lobby/{lobbyId}/start
"alice-id"
```

### 5. Jouer via SignalR

Connectez-vous au Hub correspondant (`/tictactoehub`, etc.) et envoyez des actions en temps réel.

---

## ?? Architecture

```
CogX/
??? Controllers/          # API REST endpoints
?   ??? PlayerController.cs
?   ??? LobbyController.cs
?   ??? LeaderboardController.cs
?   ??? AdminController.cs
??? Hubs/                 # SignalR temps réel
?   ??? LobbyHub.cs
?   ??? Games/
?       ??? TicTacToeHub.cs
?       ??? SpeedTypingHub.cs
?       ??? Connect4Hub.cs
??? Services/             # Logique métier
?   ??? GameHistoryService.cs
?   ??? Games/
?       ??? TicTacToeService.cs
?       ??? SpeedTypingService.cs
?       ??? Connect4Service.cs
??? Models/               # Entités
?   ??? Player.cs
?   ??? Lobby.cs
?   ??? GameSession.cs
?   ??? Games/
??? DTOs/                 # Data Transfer Objects
??? Data/
?   ??? CogXDbContext.cs  # Entity Framework
??? Migrations/           # Schéma de la BDD
```

---

## ?? Fonctionnalités Implémentées

| Fonctionnalité | Points | Statut |
|----------------|--------|--------|
| **Morpion** | 3 | ? |
| **Speed Typing** | 3 | ? |
| **Puissance 4** | 3 | ? |
| **Leaderboard** | 2 | ? |
| **Historique actions** | 3 | ? |
| **Liste lobbies temps réel** | 3 | ? |
| **Lobbies privés (password)** | - | ? |
| **Total Backend** | **17** | ? |

---

## ?? Dépannage

### Erreur : "Cannot connect to database"

```bash
# Vérifier que LocalDB est démarré
sqllocaldb info
sqllocaldb start MSSQLLocalDB

# Recréer la base
cd CogX
dotnet ef database drop --force
dotnet ef database update
```

### Erreur : "dotnet ef not found"

```bash
dotnet tool install --global dotnet-ef
```

### Erreur de compilation

```bash
dotnet clean
dotnet restore
dotnet build
```

### Port déjà utilisé

Modifiez le port dans `CogX/Properties/launchSettings.json`

---

## ?? Ressources

- [Documentation .NET 10](https://learn.microsoft.com/en-us/dotnet/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Swagger/OpenAPI](https://swagger.io/)

---

## ????? Développement

### Ajouter une migration

```bash
cd CogX
dotnet ef migrations add NomDeLaMigration
dotnet ef database update
```

### Voir les logs

Les logs s'affichent dans la console ou dans **Output ? Debug** dans Visual Studio.

### Modifier la configuration

- Chaîne de connexion : `appsettings.json`
- Ports de l'API : `Properties/launchSettings.json`
- CORS : `Program.cs`

---

## ?? TODO Frontend (Non implémenté)

- [ ] Page d'accueil (saisie pseudo)
- [ ] Liste des jeux disponibles
- [ ] Liste des lobbies en temps réel
- [ ] Pages de jeu (React + Tailwind)
- [ ] Leaderboard UI
- [ ] Responsive design
- [ ] Multilingue (FR/EN)

---

## ?? Licence

Projet académique - M2 GIL 2025-2026

---

## ?? Contributeurs

- Développeur : [raidBer](https://github.com/raidBer)
- Projet : TP .NET - Plateforme de jeux multijoueurs

---

## ?? Support

Pour toute question, créez une [Issue](https://github.com/raidBer/CogX/issues) sur GitHub.

---

**Bon développement ! ??**
