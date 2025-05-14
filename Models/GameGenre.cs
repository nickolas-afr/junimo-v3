using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "GameGenre")]
    public class GameGenre
    {
        [Column("game_id")]
        [ForeignKey("Game")]
        [Required]
        public int GameId { get; set; }

        [Column("genre_id")]
        [ForeignKey("Genre")]
        [Required]
        public int GenreId { get; set; }


        public Game? Game { get; set; }
        public Genre? Genre { get; set; }
    }
}