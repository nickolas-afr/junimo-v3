using Microsoft.ML.Data;

namespace junimo_v3.Models.ML
{
    /// <summary>
    /// Output model for game rating prediction.
    /// Contains the predicted score from the recommendation model.
    /// </summary>
    public class GameRatingPrediction
    {
        /// <summary>
        /// The predicted rating score.
        /// </summary>
        public float Score { get; set; }
    }
}
