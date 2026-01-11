using Microsoft.ML.Data;

namespace junimo_v3.Models
{
    public class GameRecommendation
    {
        [ColumnName("Score")]
        public float Score { get; set; }
        
        public int GameId { get; set; }
        public string GameTitle { get; set; }
        public float Price { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Genres { get; set; }
    }
}
