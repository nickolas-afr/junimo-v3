using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "GameGenreV2")]
    public class GameGenreV2
    {
        [Key]
        [Column("game_id")]
        [ForeignKey("Game")]
        [Required]
        public int GameId { get; set; }

        [Key]
        [Column("genre")]
        [Required]
        public string genre { get; set; }


        public Game? Game { get; set; }
    }
}
