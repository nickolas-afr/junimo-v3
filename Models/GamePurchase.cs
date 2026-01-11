using Microsoft.ML.Data;

namespace junimo_v3.Models
{
    public class GamePurchase
    {
        [LoadColumn(0)]
        public string UserId { get; set; }

        [LoadColumn(1)]
        public int GameId { get; set; }

        [LoadColumn(2)]
        public string Genre { get; set; }

        [LoadColumn(3)]
        public float Label { get; set; } // 1 if purchased, 0 otherwise
    }
}
