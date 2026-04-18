using junimo_v3.Models;
using junimo_v3.Models.DocumentSearch;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class LuceneDocumentSearchService : IDocumentSearchService
    {
        private static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_48;
        private const int MaxSearchResults = 1000;

        private readonly IRepositoryWrapper _repository;
        private readonly string _indexPath;
        private readonly StandardAnalyzer _analyzer;
        private readonly object _sync = new();

        public LuceneDocumentSearchService(IRepositoryWrapper repository, IWebHostEnvironment environment)
        {
            _repository = repository;
            _indexPath = Path.Combine(environment.ContentRootPath, "App_Data", "Lucene", "GameDocuments");
            _analyzer = new StandardAnalyzer(LuceneVersion);
        }

        public async Task RebuildIndexAsync()
        {
            var games = await _repository.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Include(g => g.Reviews)
                .ToListAsync();

            lock (_sync)
            {
                using var directory = OpenDirectory();
                var config = new IndexWriterConfig(LuceneVersion, _analyzer)
                {
                    OpenMode = OpenMode.CREATE,
                    Similarity = new BM25Similarity()
                };

                using var writer = new IndexWriter(directory, config);
                foreach (var game in games)
                {
                    writer.AddDocument(BuildDocument(game));
                }

                writer.Flush(triggerMerge: false, applyAllDeletes: true);
                writer.Commit();
            }
        }

        public async Task UpsertGameAsync(int gameId)
        {
            var game = await _repository.Game
                .FindByCondition(g => g.GameId == gameId)
                .Include(g => g.GameGenresV2)
                .Include(g => g.Reviews)
                .FirstOrDefaultAsync();

            if (game == null)
            {
                await DeleteGameAsync(gameId);
                return;
            }

            lock (_sync)
            {
                using var directory = OpenDirectory();
                var config = new IndexWriterConfig(LuceneVersion, _analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND,
                    Similarity = new BM25Similarity()
                };

                using var writer = new IndexWriter(directory, config);
                writer.UpdateDocument(new Term("gameId", gameId.ToString()), BuildDocument(game));
                writer.Flush(triggerMerge: false, applyAllDeletes: true);
                writer.Commit();
            }
        }

        public Task DeleteGameAsync(int gameId)
        {
            lock (_sync)
            {
                using var directory = OpenDirectory();
                if (!DirectoryReader.IndexExists(directory))
                {
                    return Task.CompletedTask;
                }

                var config = new IndexWriterConfig(LuceneVersion, _analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND,
                    Similarity = new BM25Similarity()
                };

                using var writer = new IndexWriter(directory, config);
                writer.DeleteDocuments(new Term("gameId", gameId.ToString()));
                writer.Flush(triggerMerge: false, applyAllDeletes: true);
                writer.Commit();
            }

            return Task.CompletedTask;
        }

        public Task<DocumentSearchResultPage> SearchAsync(string query, string sort, int page, int pageSize)
        {
            string normalizedSort = DocumentSearchSort.Normalize(sort);
            int normalizedPage = Math.Max(page, 1);
            int normalizedPageSize = Math.Clamp(pageSize, 1, 50);

            var result = new DocumentSearchResultPage
            {
                Query = query?.Trim() ?? string.Empty,
                Sort = normalizedSort,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                SimilarityModel = "BM25"
            };

            if (string.IsNullOrWhiteSpace(result.Query))
            {
                return Task.FromResult(result);
            }

            lock (_sync)
            {
                using var directory = OpenDirectory();
                if (!DirectoryReader.IndexExists(directory))
                {
                    return Task.FromResult(result);
                }

                using var reader = DirectoryReader.Open(directory);
                var searcher = new IndexSearcher(reader)
                {
                    Similarity = new BM25Similarity()
                };

                var parser = new MultiFieldQueryParser(
                    LuceneVersion,
                    ["content", "title", "description", "genres", "reviews"],
                    _analyzer);

                Query parsedQuery;
                try
                {
                    parsedQuery = parser.Parse(QueryParser.Escape(result.Query));
                }
                catch (ParseException)
                {
                    parsedQuery = parser.Parse(QueryParser.Escape(result.Query.Replace('"', ' ')));
                }

                int requested = Math.Min(
                    MaxSearchResults,
                    Math.Max(normalizedPage * normalizedPageSize, normalizedPageSize));

                var topDocs = searcher.Search(parsedQuery, requested);
                var scoreDocs = topDocs.ScoreDocs.ToList();

                if (normalizedSort == DocumentSearchSort.ScoreAsc)
                {
                    scoreDocs = scoreDocs.OrderBy(sd => sd.Score).ToList();
                }

                result.TotalHits = topDocs.TotalHits;

                int skip = (normalizedPage - 1) * normalizedPageSize;
                var pageDocs = scoreDocs.Skip(skip).Take(normalizedPageSize);

                result.Hits = pageDocs.Select(sd =>
                {
                    var doc = searcher.Doc(sd.Doc);
                    string title = doc.Get("title") ?? "Untitled";
                    string content = doc.Get("content") ?? string.Empty;

                    return new DocumentSearchHit
                    {
                        GameId = int.TryParse(doc.Get("gameId"), out int gameId) ? gameId : 0,
                        Title = title,
                        Score = sd.Score,
                        Snippet = BuildSnippet(content, result.Query)
                    };
                }).Where(hit => hit.GameId > 0).ToList();
            }

            return Task.FromResult(result);
        }

        private FSDirectory OpenDirectory()
        {
            System.IO.Directory.CreateDirectory(_indexPath);
            return FSDirectory.Open(new DirectoryInfo(_indexPath));
        }

        private static Document BuildDocument(Game game)
        {
            string title = game.Title ?? string.Empty;
            string description = game.Description ?? string.Empty;
            string genres = string.Join(" ", game.GameGenresV2?.Select(g => g.genre) ?? []);
            string reviews = string.Join(" ", game.Reviews?
                .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                .Select(r => r.Comment!) ?? []);

            string content = string.Join(" ",
            [
                // Title repeated to intentionally boost title-term relevance in BM25 scoring.
                title,
                title,
                description,
                genres,
                reviews
            ]);

            return new Document
            {
                new StringField("gameId", game.GameId.ToString(), Field.Store.YES),
                new TextField("title", title, Field.Store.YES),
                new TextField("description", description, Field.Store.YES),
                new TextField("genres", genres, Field.Store.YES),
                new TextField("reviews", reviews, Field.Store.YES),
                new TextField("content", content, Field.Store.YES)
            };
        }

        private static string BuildSnippet(string content, string query)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            const int radius = 110;
            string text = content.Trim();
            int index = FindBestSnippetStartIndex(text, query);

            if (index < 0)
            {
                return text.Length <= 220 ? text : text[..220] + "...";
            }

            int start = Math.Max(0, index - radius);
            int length = Math.Min(text.Length - start, radius * 2);
            string snippet = text.Substring(start, length).Trim();

            if (start > 0)
            {
                snippet = "..." + snippet;
            }

            if (start + length < text.Length)
            {
                snippet += "...";
            }

            return snippet;
        }

        private static int FindBestSnippetStartIndex(string text, string query)
        {
            var queryTerms = ExtractQueryTerms(query);
            int? bestIndex = null;

            foreach (var term in queryTerms)
            {
                int termIndex = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (termIndex >= 0 && (!bestIndex.HasValue || termIndex < bestIndex.Value))
                {
                    bestIndex = termIndex;
                }
            }

            if (bestIndex.HasValue)
            {
                return bestIndex.Value;
            }

            return text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ExtractQueryTerms(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            return Regex.Matches(query, @"[A-Za-z0-9]{2,}")
                .Select(m => m.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
