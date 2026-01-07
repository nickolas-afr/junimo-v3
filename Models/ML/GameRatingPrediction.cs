using Microsoft.ML.Data;

namespace junimo_v3.Models.ML
{
    /// <summary>
    /// Represents the prediction output for the recommendation model.
    /// </summary>
    public class GameRatingPrediction
    {
        public float Score { get; set; }
    }
}
