using Microsoft.ML.Data;

namespace junimo_v3.Models.ML
{
    /// <summary>
    /// Input data model for game rating prediction.
    /// Used to train the ML.NET Matrix Factorization model.
    /// </summary>
    public class GameRating
    {
        /// <summary>
        /// User identifier (converted to KeyType for ML.NET).
        /// </summary>
        [LoadColumn(0)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Game identifier (converted to KeyType for ML.NET).
        /// </summary>
        [LoadColumn(1)]
        public uint GameId { get; set; }

        /// <summary>
        /// The rating value (1-10 scale from reviews, normalized to 0-1 for predictions).
        /// </summary>
        [LoadColumn(2)]
        public float Label { get; set; }
    }
}
