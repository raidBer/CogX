-- ========================================
-- Script d'exploration de la base CogXDb
-- ========================================

USE CogXDb;
GO

-- ========================================
-- 1. STATISTIQUES GLOBALES
-- ========================================

PRINT '=== STATISTIQUES GLOBALES ===';

SELECT 
    'Total Joueurs' AS Metric,
    COUNT(*) AS Value
FROM Players

UNION ALL

SELECT 
    'Total Lobbies',
    COUNT(*)
FROM Lobbies

UNION ALL

SELECT 
    'Total Sessions de Jeu',
    COUNT(*)
FROM GameSessions

UNION ALL

SELECT 
    'Total Actions',
    COUNT(*)
FROM GameActions

UNION ALL

SELECT 
    'Total Scores Leaderboard',
    COUNT(*)
FROM Leaderboard;

GO

-- ========================================
-- 2. LISTE DES JOUEURS
-- ========================================

PRINT '';
PRINT '=== LISTE DES JOUEURS ===';

SELECT 
    Id,
    Pseudo,
    CreatedAt,
    DATEDIFF(DAY, CreatedAt, GETDATE()) AS DaysAgo
FROM Players
ORDER BY CreatedAt DESC;

GO

-- ========================================
-- 3. LOBBIES ACTIFS
-- ========================================

PRINT '';
PRINT '=== LOBBIES ACTIFS ===';

SELECT 
    L.Id,
    L.GameType,
    P.Pseudo AS HostPseudo,
    L.Status,
    L.MaxPlayers,
    (SELECT COUNT(*) FROM LobbyPlayers LP WHERE LP.LobbyId = L.Id) AS CurrentPlayers,
    CASE WHEN L.Password IS NULL THEN 'Public' ELSE 'Private' END AS Visibility,
    L.CreatedAt
FROM Lobbies L
INNER JOIN Players P ON L.HostId = P.Id
ORDER BY L.CreatedAt DESC;

GO

-- ========================================
-- 4. SESSIONS DE JEU
-- ========================================

PRINT '';
PRINT '=== SESSIONS DE JEU ===';

SELECT 
    GS.Id,
    L.GameType,
    GS.StartedAt,
    GS.FinishedAt,
    CASE 
        WHEN GS.FinishedAt IS NULL THEN 'En cours'
        ELSE CONCAT(DATEDIFF(SECOND, GS.StartedAt, GS.FinishedAt), 's')
    END AS Duration,
    (SELECT COUNT(*) FROM GameActions GA WHERE GA.GameSessionId = GS.Id) AS ActionCount
FROM GameSessions GS
INNER JOIN Lobbies L ON GS.LobbyId = L.Id
ORDER BY GS.StartedAt DESC;

GO

-- ========================================
-- 5. LEADERBOARD PAR JEU
-- ========================================

PRINT '';
PRINT '=== LEADERBOARD - SPEED TYPING ===';

SELECT TOP 10
    ROW_NUMBER() OVER (ORDER BY L.Score DESC, L.Time ASC) AS Rank,
    P.Pseudo,
    L.Score,
    L.Time,
    L.AchievedAt
FROM Leaderboard L
INNER JOIN Players P ON L.PlayerId = P.Id
WHERE L.GameType = 'SpeedTyping'
ORDER BY L.Score DESC, L.Time ASC;

GO

PRINT '';
PRINT '=== LEADERBOARD - MORPION ===';

SELECT TOP 10
    ROW_NUMBER() OVER (ORDER BY L.Score DESC) AS Rank,
    P.Pseudo,
    L.Score,
    L.AchievedAt
FROM Leaderboard L
INNER JOIN Players P ON L.PlayerId = P.Id
WHERE L.GameType = 'Morpion'
ORDER BY L.Score DESC;

GO

PRINT '';
PRINT '=== LEADERBOARD - PUISSANCE 4 ===';

SELECT TOP 10
    ROW_NUMBER() OVER (ORDER BY L.Score DESC) AS Rank,
    P.Pseudo,
    L.Score,
    L.AchievedAt
FROM Leaderboard L
INNER JOIN Players P ON L.PlayerId = P.Id
WHERE L.GameType = 'Puissance4'
ORDER BY L.Score DESC;

GO

-- ========================================
-- 6. ACTIONS LES PLUS RÉCENTES
-- ========================================

PRINT '';
PRINT '=== ACTIONS RÉCENTES ===';

SELECT TOP 20
    GA.Timestamp,
    P.Pseudo AS PlayerPseudo,
    L.GameType,
    GA.ActionType,
    GA.ActionData
FROM GameActions GA
INNER JOIN Players P ON GA.PlayerId = P.Id
INNER JOIN GameSessions GS ON GA.GameSessionId = GS.Id
INNER JOIN Lobbies L ON GS.LobbyId = L.Id
ORDER BY GA.Timestamp DESC;

GO

-- ========================================
-- 7. STATISTIQUES PAR TYPE DE JEU
-- ========================================

PRINT '';
PRINT '=== STATISTIQUES PAR JEU ===';

SELECT 
    L.GameType,
    COUNT(DISTINCT GS.Id) AS TotalGames,
    COUNT(DISTINCT LB.PlayerId) AS UniquePlayers,
    AVG(CAST(LB.Score AS FLOAT)) AS AvgScore,
    MAX(LB.Score) AS HighScore
FROM GameSessions GS
INNER JOIN Lobbies L ON GS.LobbyId = L.Id
LEFT JOIN Leaderboard LB ON LB.GameType = L.GameType
GROUP BY L.GameType
ORDER BY TotalGames DESC;

GO

-- ========================================
-- 8. JOUEURS LES PLUS ACTIFS
-- ========================================

PRINT '';
PRINT '=== JOUEURS LES PLUS ACTIFS ===';

SELECT TOP 10
    P.Pseudo,
    COUNT(DISTINCT GA.GameSessionId) AS GamesPlayed,
    COUNT(GA.Id) AS TotalActions,
    (SELECT COUNT(*) FROM Leaderboard LB WHERE LB.PlayerId = P.Id) AS ScoresRecorded
FROM Players P
LEFT JOIN GameActions GA ON GA.PlayerId = P.Id
GROUP BY P.Id, P.Pseudo
ORDER BY TotalActions DESC;

GO

-- ========================================
-- 9. NETTOYAGE (Optionnel - Décommenter si besoin)
-- ========================================

/*
PRINT '';
PRINT '=== NETTOYAGE DE LA BASE ===';

DELETE FROM GameActions;
DELETE FROM Leaderboard;
DELETE FROM GameSessions;
DELETE FROM LobbyPlayers;
DELETE FROM Lobbies;
DELETE FROM Players;

PRINT 'Base de données nettoyée !';
*/

GO

PRINT '';
PRINT '=== FIN DU SCRIPT ===';
