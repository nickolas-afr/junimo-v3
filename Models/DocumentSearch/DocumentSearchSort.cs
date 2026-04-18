namespace junimo_v3.Models.DocumentSearch
{
    public static class DocumentSearchSort
    {
        public const string ScoreDesc = "score_desc";
        public const string ScoreAsc = "score_asc";

        public static string Normalize(string? sort)
        {
            return string.Equals(sort, ScoreAsc, StringComparison.OrdinalIgnoreCase)
                ? ScoreAsc
                : ScoreDesc;
        }
    }
}
