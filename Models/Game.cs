using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "Game")]
    public class Game
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GameId { get; set; }

        [Column("title")]
        [Required]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [Column("description")]
        [Required]
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        [Column("price")]
        [Required]
        [Range(0, 999.99, ErrorMessage = "Price must be between 0 and 999.99")]
        [DataType(DataType.Currency)]
        public float Price { get; set; }

        [Column("release_date")]
        [Required]
        public DateOnly ReleaseDate { get; set; }

        [Column("image_url")]
        [Required]
        public string ImageUrl { get; set; }

        [Column("is_featured")]
        [Required]
        public bool IsFeatureRecommended { get; set; }

        // Navigation properties - initialized as empty collections
        public ICollection<GameGenre>? GameGenres { get; set; }
        public ICollection<GameGenreV2>? GameGenresV2 { get; set; }
        public ICollection<Review>? Reviews { get; set; }
    }
}