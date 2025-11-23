using CogX.Models.Games;

namespace CogX.Services.Games
{
    public interface ISpeedTypingService
    {
        SpeedTypingState InitializeGame(Guid gameSessionId, List<(Guid Id, string Pseudo)> players);
        string GenerateText(int difficulty = 2);
        SpeedTypingState UpdatePlayerProgress(SpeedTypingState state, Guid playerId, int charactersTyped, int errorCount);
        int CalculateWPM(int charactersTyped, TimeSpan elapsed);
        int CalculateAccuracy(int charactersTyped, int errorCount);
    }

    public class SpeedTypingService : ISpeedTypingService
    {
        private static readonly string[] EasyTexts = new[]
        {
            "The quick brown fox jumps over the lazy dog.",
            "A journey of a thousand miles begins with a single step.",
            "To be or not to be, that is the question.",
            "All that glitters is not gold.",
            "Actions speak louder than words."
        };

        private static readonly string[] MediumTexts = new[]
        {
            "Programming is the art of telling another human what one wants the computer to do. The computer follows instructions precisely, but humans must communicate clearly.",
            "In software development, debugging is twice as hard as writing the code in the first place. Therefore, if you write code as cleverly as possible, you are not smart enough to debug it.",
            "The best way to predict the future is to implement it. Technology moves forward when people build solutions to real problems.",
            "Good code is its own best documentation. As you are about to add a comment, ask yourself if there is some way to turn the code itself into the explanation."
        };

        private static readonly string[] HardTexts = new[]
        {
            "Machine learning algorithms can analyze vast amounts of data to identify patterns and make predictions. These systems learn from experience without being explicitly programmed for each specific task, adapting their behavior based on the data they process.",
            "Quantum computing represents a fundamental shift in how we process information. Unlike classical computers that use bits representing either zero or one, quantum computers use quantum bits or qubits that can exist in multiple states simultaneously through superposition.",
            "Cybersecurity professionals must constantly adapt to evolving threats. Attack vectors become more sophisticated as technology advances, requiring comprehensive strategies that include network security, application security, information security, and operational security measures."
        };

        public SpeedTypingState InitializeGame(Guid gameSessionId, List<(Guid Id, string Pseudo)> players)
        {
            var state = new SpeedTypingState
            {
                GameSessionId = gameSessionId,
                TextToType = GenerateText(),
                IsStarted = false,
                IsFinished = false
            };

            foreach (var player in players)
            {
                state.PlayerProgressMap[player.Id] = new PlayerProgress
                {
                    PlayerId = player.Id,
                    Pseudo = player.Pseudo,
                    CharactersTyped = 0,
                    TotalCharacters = state.TextToType.Length,
                    ProgressPercentage = 0,
                    WPM = 0,
                    Accuracy = 100,
                    HasFinished = false
                };
            }

            return state;
        }

        public string GenerateText(int difficulty = 2)
        {
            var random = new Random();
            return difficulty switch
            {
                1 => EasyTexts[random.Next(EasyTexts.Length)],
                2 => MediumTexts[random.Next(MediumTexts.Length)],
                3 => HardTexts[random.Next(HardTexts.Length)],
                _ => MediumTexts[random.Next(MediumTexts.Length)]
            };
        }

        public SpeedTypingState UpdatePlayerProgress(SpeedTypingState state, Guid playerId, int charactersTyped, int errorCount)
        {
            if (!state.PlayerProgressMap.ContainsKey(playerId))
                throw new InvalidOperationException("Player not found in game");

            var progress = state.PlayerProgressMap[playerId];
            var elapsed = DateTime.UtcNow - state.StartTime;

            progress.CharactersTyped = Math.Min(charactersTyped, state.TextToType.Length);
            progress.ProgressPercentage = (double)progress.CharactersTyped / state.TextToType.Length * 100;
            progress.WPM = CalculateWPM(progress.CharactersTyped, elapsed);
            progress.Accuracy = CalculateAccuracy(progress.CharactersTyped, errorCount);

            // Le joueur a terminé
            if (progress.CharactersTyped >= state.TextToType.Length && !progress.HasFinished)
            {
                progress.HasFinished = true;
                progress.FinishTime = elapsed;
                state.FinishedPlayerIds.Add(playerId);
                progress.Rank = state.FinishedPlayerIds.Count;
            }

            // Vérifier si tous les joueurs ont terminé
            if (state.FinishedPlayerIds.Count == state.PlayerProgressMap.Count)
            {
                state.IsFinished = true;
                state.EndTime = DateTime.UtcNow;
            }

            return state;
        }

        public int CalculateWPM(int charactersTyped, TimeSpan elapsed)
        {
            if (elapsed.TotalMinutes == 0)
                return 0;

            // WPM = (caractères tapés / 5) / minutes écoulées
            // Division par 5 car un "mot" moyen = 5 caractères
            return (int)((charactersTyped / 5.0) / elapsed.TotalMinutes);
        }

        public int CalculateAccuracy(int charactersTyped, int errorCount)
        {
            if (charactersTyped == 0)
                return 100;

            var accuracy = (double)(charactersTyped - errorCount) / charactersTyped * 100;
            return Math.Max(0, Math.Min(100, (int)accuracy));
        }
    }
}
