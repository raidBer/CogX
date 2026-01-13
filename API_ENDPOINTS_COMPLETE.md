# ?? API Endpoints Complets - CogX Platform

## ?? Vue d'ensemble

Tous les endpoints REST et SignalR nécessaires pour le fonctionnement complet de la plateforme.

---

## ?? Player Endpoints

**Base URL:** `/api/player`

| Method | Endpoint | Description | Body | Response |
|--------|----------|-------------|------|----------|
| `GET` | `/api/player` | Liste tous les joueurs | - | `List<PlayerDto>` |
| `POST` | `/api/player` | Créer un joueur | `{ pseudo }` | `PlayerDto` |
| `GET` | `/api/player/{id}` | Récupérer un joueur | - | `PlayerDto` |
| `DELETE` | `/api/player/{id}` | Supprimer un joueur (GDPR) | - | `200 OK` |

---

## ?? Lobby Endpoints

**Base URL:** `/api/lobby`

| Method | Endpoint | Description | Body/Query | Response |
|--------|----------|-------------|------------|----------|
| `GET` | `/api/lobby` | Liste lobbies publics | `?gameType=Morpion` (optionnel) | `List<LobbyDto>` |
| `GET` | `/api/lobby/{id}` | Détails d'un lobby | `?playerId={guid}` | `LobbyDetailsDto` |
| `GET` | `/api/lobby/{id}/players` | Joueurs dans un lobby | - | `List<PlayerDto>` |
| `POST` | `/api/lobby` | Créer un lobby | `CreateLobbyRequest` | `CreateLobbyResponse` |
| `POST` | `/api/lobby/{id}/join` | Rejoindre un lobby | `JoinLobbyRequest` | `200 OK` |
| `POST` | `/api/lobby/{id}/leave` | Quitter un lobby | `Guid playerId` | `200 OK` |
| `POST` | `/api/lobby/{id}/start` | Démarrer la partie | `Guid hostId` | `{ gameSessionId }` |
| `DELETE` | `/api/lobby/{id}` | Supprimer un lobby | `?hostId={guid}` | `200 OK` |

---

## ?? Leaderboard Endpoints

**Base URL:** `/api/leaderboard`

| Method | Endpoint | Description | Query | Response |
|--------|----------|-------------|-------|----------|
| `GET` | `/api/leaderboard/{gameType}` | Classement d'un jeu | `?top=10&playerId={guid}` | `LeaderboardResponse` |
| `GET` | `/api/leaderboard/games` | Stats de tous les jeux | - | `List<GameStatsDto>` |
| `GET` | `/api/leaderboard/player/{playerId}` | Historique d'un joueur | `?gameType=Morpion` | `List<LeaderboardDto>` |
| `POST` | `/api/leaderboard` | Ajouter un score | `AddScoreRequest` | `LeaderboardDto` |
| `DELETE` | `/api/leaderboard/player/{playerId}` | Supprimer scores (GDPR) | - | `{ deleted: count }` |

---

## ??? Admin Endpoints

**Base URL:** `/api/admin`

| Method | Endpoint | Description | Query | Response |
|--------|----------|-------------|-------|----------|
| `GET` | `/api/admin/game-history/{sessionId}` | Historique d'une partie | - | `GameHistoryResponse` |
| `GET` | `/api/admin/game-sessions` | Liste des sessions | `?gameType=Morpion&limit=50` | `List<GameSessionSummaryDto>` |
| `GET` | `/api/admin/player-actions/{playerId}` | Actions d'un joueur | `?gameSessionId={guid}` | `List<GameActionDto>` |
| `GET` | `/api/admin/export/{sessionId}` | Export JSON (téléchargement) | - | `File (JSON)` |
| `GET` | `/api/admin/platform-stats` | Statistiques globales | - | `PlatformStatsDto` |
| `DELETE` | `/api/admin/game-history/{sessionId}` | Supprimer historique (GDPR) | - | `{ deleted: count }` |

---

## ?? SignalR Hubs

### 1?? LobbyHub (`/lobbyhub`)

**Methods (invoquées par le client) :**
- `JoinLobbyGroup(string lobbyId)` - Rejoindre un groupe
- `LeaveLobbyGroup(string lobbyId)` - Quitter un groupe
- `SubscribeToLobbyList()` - S'abonner à la liste des lobbies
- `UnsubscribeFromLobbyList()` - Se désabonner

**Events (reçus par le client) :**
- `LobbyCreated` - Nouveau lobby créé
- `LobbyDeleted` - Lobby supprimé
- `PlayerJoined` - Joueur a rejoint
- `PlayerLeft` - Joueur a quitté
- `GameStarted` - Partie démarrée
- `LobbyClosed` - Lobby fermé

---

### 2?? TicTacToeHub (`/tictactoehub`)

**Methods :**
- `JoinGameRoom(string lobbyId)`
- `LeaveGameRoom(string lobbyId)`
- `InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)`
- `MakeMove(string lobbyId, Guid gameSessionId, Guid playerId, int row, int col)`
- `GetGameState(Guid gameSessionId)` ? Returns `TicTacToeGameDto`
- `Forfeit(string lobbyId, Guid gameSessionId, Guid playerId)`

**Events :**
- `TicTacToeInitialized` - Jeu initialisé
- `MoveMade` - Coup joué
- `GameOver` - Partie terminée
- `InvalidMove` - Coup invalide
- `GameError` - Erreur
- `PlayerForfeited` - Abandon

---

### 3?? SpeedTypingHub (`/speedtypinghub`)

**Methods :**
- `JoinGameRoom(string lobbyId)`
- `LeaveGameRoom(string lobbyId)`
- `InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)`
- `StartRace(string lobbyId, Guid gameSessionId)`
- `UpdateProgress(string lobbyId, Guid gameSessionId, Guid playerId, int charactersTyped, int errorCount)`
- `GetGameState(Guid gameSessionId)` ? Returns `SpeedTypingGameDto`

**Events :**
- `SpeedTypingInitialized` - Jeu initialisé
- `Countdown` - Compte à rebours (3, 2, 1)
- `RaceStarted` - Course démarrée
- `ProgressUpdated` - Progression d'un joueur mise à jour
- `PlayerFinished` - Joueur a terminé
- `RaceEnded` - Course terminée
- `RaceTimeout` - Temps écoulé
- `GameError` - Erreur

---

### 4?? Connect4Hub (`/connect4hub`)

**Methods :**
- `JoinGameRoom(string lobbyId)`
- `LeaveGameRoom(string lobbyId)`
- `InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)`
- `DropPiece(string lobbyId, Guid gameSessionId, Guid playerId, int column)`
- `GetGameState(Guid gameSessionId)` ? Returns `Connect4GameDto`
- `Forfeit(string lobbyId, Guid gameSessionId, Guid playerId)`

**Events :**
- `Connect4Initialized` - Jeu initialisé
- `PieceDropped` - Pion placé
- `GameOver` - Partie terminée
- `InvalidMove` - Coup invalide
- `GameError` - Erreur
- `PlayerForfeited` - Abandon

---

## ?? Workflow complet (Exemple Morpion)

```
1. POST /api/player                          ? Créer Alice
2. POST /api/player                          ? Créer Bob
3. POST /api/lobby                           ? Alice crée lobby Morpion
4. POST /api/lobby/{id}/join                 ? Bob rejoint
5. POST /api/lobby/{id}/start                ? Alice démarre
   
   ? SignalR WebSocket Connection ?
   
6. Connect to wss://localhost:7049/tictactoehub
7. Invoke: JoinGameRoom(lobbyId)
8. Invoke: InitializeGame(lobbyId, sessionId, [aliceId, bobId])
9. Event: TicTacToeInitialized
10. Invoke: MakeMove(lobbyId, sessionId, aliceId, 0, 0)
11. Event: MoveMade
12. ... (boucle de jeu) ...
13. Event: GameOver
14. POST /api/leaderboard                    ? Enregistrer score
```

---

## ?? Checklist Endpoints Frontend

### Avant le jeu
```
? POST /api/player - Créer joueur 1
? POST /api/player - Créer joueur 2
? POST /api/lobby - Créer un lobby
? GET /api/lobby - Lister les lobbies (optionnel)
? POST /api/lobby/{id}/join - Rejoindre
? GET /api/lobby/{id} - Voir détails du lobby
? POST /api/lobby/{id}/start - Démarrer
```

### Pendant le jeu (SignalR)
```
? Connect to /tictactoehub (ou autre)
? Invoke: JoinGameRoom
? Invoke: InitializeGame
? Listen: GameInitialized event
? Invoke: MakeMove (à chaque coup)
? Listen: MoveMade event
? Listen: GameOver event
? Invoke: LeaveGameRoom (à la fin)
```

### Après le jeu
```
? POST /api/leaderboard - Enregistrer score
? GET /api/leaderboard/{gameType} - Voir classement
```

### Admin (optionnel)
```
? GET /api/admin/platform-stats
? GET /api/admin/game-sessions
? GET /api/admin/game-history/{id}
```

---

## ?? Nouveaux Endpoints Ajoutés

### PlayerController
- ? `GET /api/player` - Liste tous les joueurs
- ? `DELETE /api/player/{id}` - Supprimer un joueur

### LobbyController
- ? `GET /api/lobby?gameType={type}` - Filtrage par type de jeu
- ? `GET /api/lobby/{id}/players` - Liste des joueurs dans un lobby
- ? `POST /api/lobby/{id}/leave` - Quitter un lobby avant le démarrage

---

## ?? Authentification

**Actuellement :** Aucune authentification (développement)

**Pour production :**
- Ajouter JWT tokens
- Middleware d'authentification
- Vérifier l'identité dans chaque requête

---

## ?? CORS

**Configuration actuelle :**
```csharp
policy.SetIsOriginAllowed(origin => true) // Accepte toutes les origins en dev
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

**?? À changer en production !**

---

## ?? Types de Jeux Supportés

| GameType | Hub | Max Players |
|----------|-----|-------------|
| `Morpion` | `/tictactoehub` | 2 |
| `SpeedTyping` | `/speedtypinghub` | 2+ |
| `Puissance4` | `/connect4hub` | 2 |

---

## ? Résumé

**Total Endpoints REST :** 23  
**Total SignalR Hubs :** 4  
**Total SignalR Methods :** 20+  
**Total SignalR Events :** 25+

Tous les endpoints nécessaires pour le fonctionnement complet de la plateforme sont **exposés et fonctionnels** ! ??
