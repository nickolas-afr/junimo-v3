using System.Text.RegularExpressions;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class SimilarGamesService : ISimilarGamesService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public SimilarGamesService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        /// <summary>
        /// Returns the top <paramref name="topN"/> games most similar to the given game,
        /// ranked by TF-IDF cosine similarity over title, genres, and description.
        /// </summary>
        public async Task<IEnumerable<GameRecommendation>> GetSimilarGamesAsync(int gameId, int topN = 4)
        {
            var allGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .ToListAsync();

            int targetIdx = allGames.FindIndex(g => g.GameId == gameId);
            if (targetIdx < 0) return [];

            // Build one text document per game
            var documents = allGames.Select(BuildDocument).ToList();

            // Compute sparse TF-IDF vectors for the whole corpus
            var vectors = ComputeTfIdfVectors(documents);

            var targetVec = vectors[targetIdx];

            return allGames
                .Select((g, i) => (game: g, score: i == targetIdx
                    ? -1.0
                    : CosineSimilarity(targetVec, vectors[i])))
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Take(topN)
                .Select(x => new GameRecommendation
                {
                    GameId      = x.game.GameId,
                    GameTitle   = x.game.Title,
                    Price       = x.game.Price,
                    ImageUrl    = x.game.GamePictureURL,
                    Score       = (float)x.score,
                    Genres      = x.game.GameGenresV2?.Select(gg => gg.genre).ToList() ?? []
                });
        }

        // ── Document construction ────────────────────────────────────────────

        /// <summary>
        /// Builds the text document for a game.
        /// Title and genres are repeated to boost their weight in TF-IDF.
        /// </summary>
        private static string BuildDocument(Game game)
        {
            var genres = game.GameGenresV2?.Select(gg => gg.genre) ?? [];
            var parts = new List<string>();

            // Title × 3 — strongest signal for similarity
            for (int i = 0; i < 3; i++) parts.Add(game.Title);

            // Genres × 3 — strong categorical signal
            foreach (var g in genres)
                for (int i = 0; i < 3; i++) parts.Add(g);

            // Description × 1
            parts.Add(game.Description);

            return string.Join(" ", parts);
        }

        // ── TF-IDF ──────────────────────────────────────────────────────────

        private static List<Dictionary<string, double>> ComputeTfIdfVectors(List<string> documents)
        {
            var tokenized = documents.Select(Tokenize).ToList();
            int N = tokenized.Count;

            // Document frequency: how many documents contain each term
            var df = new Dictionary<string, int>();
            foreach (var tokens in tokenized)
            {
                foreach (var term in tokens.Distinct())
                    df[term] = df.GetValueOrDefault(term) + 1;
            }

            // Smoothed IDF: log((N+1)/(df+1)) + 1
            var idf = df.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Log((N + 1.0) / (kvp.Value + 1.0)) + 1.0);

            // Sparse TF-IDF vector per document (only non-zero values stored)
            return tokenized.Select(tokens =>
            {
                int docLen = tokens.Count;
                if (docLen == 0) return new Dictionary<string, double>();

                var tf = new Dictionary<string, double>();
                foreach (var term in tokens)
                    tf[term] = tf.GetValueOrDefault(term) + 1.0;

                return tf.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value / docLen) * idf[kvp.Key]);
            }).ToList();
        }

        // ── Cosine similarity on sparse vectors ──────────────────────────────

        private static double CosineSimilarity(
            Dictionary<string, double> v1,
            Dictionary<string, double> v2)
        {
            double dot = 0;
            foreach (var (term, w1) in v1)
            {
                if (v2.TryGetValue(term, out double w2))
                    dot += w1 * w2;
            }

            double norm1 = Math.Sqrt(v1.Values.Sum(w => w * w));
            double norm2 = Math.Sqrt(v2.Values.Sum(w => w * w));

            return (norm1 == 0 || norm2 == 0) ? 0 : dot / (norm1 * norm2);
        }

        // ── Tokenization ─────────────────────────────────────────────────────

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a","an","the","and","or","but","in","on","at","to","for","of","with",
            "by","from","is","are","was","were","be","been","being","have","has",
            "had","do","does","did","will","would","shall","should","may","might",
            "can","could","its","it","this","that","these","those","as","not","no",
            "up","out","if","so","we","you","he","she","they","their","our","your",
            "his","her","my","me","us","them","who","which","what","when","where",
            "how","all","each","both","few","more","most","other","into","through",
            "during","before","after","above","below","between","own","same","than",
            "too","very","just","over","such","while","about","against","once"
        };

        private static List<string> Tokenize(string text)
        {
            return Regex
                .Replace(text.ToLowerInvariant(), @"[^a-z0-9\s]", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1 && !StopWords.Contains(t))
                .ToList();
        }
    }
}
