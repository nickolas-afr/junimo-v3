namespace junimo_v3.Models.DocumentSearch
{
    public class DocumentSearchResultPage
    {
        public string Query { get; set; } = string.Empty;
        public string Sort { get; set; } = DocumentSearchSort.ScoreDesc;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalHits { get; set; }
        public string SimilarityModel { get; set; } = "BM25";
        public List<DocumentSearchHit> Hits { get; set; } = [];

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalHits / PageSize);
    }
}
