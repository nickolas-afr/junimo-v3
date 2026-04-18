namespace junimo_v3.Models.DocumentSearch
{
    public class DocumentSearchHit
    {
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
