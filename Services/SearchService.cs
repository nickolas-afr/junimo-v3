using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class SearchService : ISearchService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public SearchService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        /// <summary>
        /// Returns up to <paramref name="limit"/> game title suggestions for the given query.
        /// Scoring priority: exact > prefix > word-prefix > substring > trigram similarity > Levenshtein (typo tolerance).
        /// </summary>
        public async Task<IEnumerable<string>> GetTitleSuggestionsAsync(string query, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return [];

            // Load all titles into memory — game catalogs are small enough for in-process scoring.
            var allTitles = await _repositoryWrapper.Game
                .FindAll()
                .Select(g => g.Title)
                .ToListAsync();

            string q = query.ToLowerInvariant();

            return allTitles
                .Select(title => (title, score: ScoreTitle(q, title.ToLowerInvariant())))
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Take(limit)
                .Select(x => x.title);
        }

        private static double ScoreTitle(string query, string title)
        {
            // Exact match
            if (title == query) return 1.0;

            // Prefix match: "mine" → "minecraft"
            if (title.StartsWith(query))
                return 0.9 + 0.05 * ((double)query.Length / title.Length);

            // Word-prefix match: "craft" → "minecraft" (first char of any word)
            var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Any(w => w.StartsWith(query)))
                return 0.7 + 0.05 * ((double)query.Length / title.Length);

            // Substring match: "land" → "borderlands"
            if (title.Contains(query))
                return 0.5 + 0.05 * ((double)query.Length / title.Length);

            // Fuzzy matching — trigram similarity (Jaccard on character trigrams)
            double score = 0;
            double trigramScore = TrigramSimilarity(query, title);
            if (trigramScore >= 0.2)
                score += trigramScore * 0.4;

            // Levenshtein distance against the title prefix of the same length as the query
            // catches single/double-character typos (e.g. "minecaft" → "minecraft")
            if (query.Length >= 3)
            {
                string titlePrefix = title.Length >= query.Length
                    ? title[..query.Length]
                    : title;
                int lev = LevenshteinDistance(query, titlePrefix);
                if (lev == 1) score += 0.25;
                else if (lev == 2 && query.Length >= 5) score += 0.10;
            }

            return score;
        }

        // Jaccard similarity on padded character trigrams.
        private static double TrigramSimilarity(string a, string b)
        {
            var trigramsA = GetTrigrams(a);
            var trigramsB = GetTrigrams(b);
            if (trigramsA.Count == 0 || trigramsB.Count == 0) return 0;

            int intersection = trigramsA.Intersect(trigramsB).Count();
            int union = trigramsA.Union(trigramsB).Count();
            return union == 0 ? 0 : (double)intersection / union;
        }

        private static HashSet<string> GetTrigrams(string s)
        {
            string padded = " " + s + " ";
            var trigrams = new HashSet<string>();
            for (int i = 0; i <= padded.Length - 3; i++)
                trigrams.Add(padded.Substring(i, 3));
            return trigrams;
        }

        // Classic two-row DP Levenshtein distance.
        private static int LevenshteinDistance(string a, string b)
        {
            int m = a.Length, n = b.Length;
            int[] prev = new int[n + 1];
            int[] curr = new int[n + 1];

            for (int j = 0; j <= n; j++) prev[j] = j;

            for (int i = 1; i <= m; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= n; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                }
                (prev, curr) = (curr, prev);
            }

            return prev[n];
        }
    }
}
