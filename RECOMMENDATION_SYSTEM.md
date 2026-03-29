# Game Recommendation System

## Overview
This project implements a game recommendation system that provides personalized game recommendations based on a user's purchase history and game genres. The system uses ML.NET infrastructure but implements a custom genre-based preference scoring algorithm.

## How It Works

### Algorithm
The recommendation system uses a **genre-based preference scoring** approach:

1. **Genre Preference Calculation**
   - Analyzes all completed orders for the user
   - Counts purchases per game genre
   - Normalizes scores to a 0-1 scale based on the most purchased genre

2. **Recommendation Scoring**
   - For each game the user doesn't own, calculates a match score
   - Score is based on how well the game's genres match the user's genre preferences
   - Higher scores indicate better matches

3. **Filtering & Ranking**
   - Excludes games the user already owns
   - Returns top N games sorted by match score

### Example
If a user has purchased 3 FPS games and 2 RPG games:
- FPS genre preference: 1.0 (100%)
- RPG genre preference: 0.67 (67%)
- An FPS game would get a high recommendation score
- An FPS/RPG hybrid game would get an average of the two genre scores

## API Endpoints

### GET /recommendations
Returns a view displaying personalized recommendations for the logged-in user.
- **Authentication:** Required
- **Returns:** HTML view with game recommendations

### GET /api/recommendations?count=10
Returns JSON array of game recommendations.
- **Authentication:** Required
- **Query Parameters:**
  - `count` (optional, default: 10): Number of recommendations to return
- **Returns:** JSON array of `GameRecommendation` objects

### GET /api/recommendations/genre-preferences
Returns the user's genre preferences based on purchase history.
- **Authentication:** Required
- **Returns:** JSON dictionary with genre names as keys and preference scores (0-1) as values

## Models

### GameRecommendation
```csharp
{
    "gameId": 123,
    "gameTitle": "Example Game",
    "price": 59.99,
    "imageUrl": "https://...",
    "score": 0.85,  // Match score (0-1)
    "genres": ["FPS", "Action"]
}
```

### Genre Preferences
```csharp
{
    "FPS": 1.0,
    "RPG": 0.67,
    "Strategy": 0.33
}
```

## Usage in Views

To add a recommendations link to your navigation:

```html
<a asp-controller="Recommendation" asp-action="Index" class="nav-link">
    <i class="bi bi-lightbulb"></i> Recommendations
</a>
```

## Service Methods

### GetRecommendationsForUserAsync(string userId, int topN = 10)
Gets game recommendations for a specific user.
- **Parameters:**
  - `userId`: The user's ID
  - `topN`: Number of recommendations to return (default: 10)
- **Returns:** List of `GameRecommendation` objects

### GetUserGenrePreferencesAsync(string userId)
Calculates genre preferences for a user.
- **Parameters:**
  - `userId`: The user's ID
- **Returns:** Dictionary of genre names and preference scores

## Technical Details

- **Framework:** ML.NET 4.0.0
- **Pattern:** Repository pattern with dependency injection
- **Database:** Uses existing EntityFramework Core context
- **Performance:** Calculates recommendations on-demand (no pre-training required)

## Future Enhancements

Possible improvements:
1. Cache genre preferences to reduce database queries
2. Add collaborative filtering (recommend based on similar users)
3. Include game ratings/reviews in scoring
4. Consider recency of purchases (weight recent purchases higher)
5. Add diversity to recommendations (avoid recommending only one genre)
