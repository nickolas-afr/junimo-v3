using Microsoft.ML.Data;

namespace junimo_v3.Models.ML
{
    /// <summary>
    /// Represents a user-genre rating for training the recommendation model.
    /// The rating represents how many times a user has purchased games with this genre.
    /// </summary>
    public class GameRating
    {
        [LoadColumn(0)]
        public string UserId { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Genre { get; set; } = string.Empty;

        [LoadColumn(2)]
        public float Rating { get; set; }
    }
}
