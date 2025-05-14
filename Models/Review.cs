using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "Review")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("review_id")]
        public int ReviewId { get; set; }

        [Column("game_id")]
        [ForeignKey("Game")]
        [Required]
        public int GameId { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        [Required]
        public string UserId { get; set; }

        [Column("rating")]
        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        [Display(Name = "Rating (1-10)")]
        public int Rating { get; set; }

        [Column("comment")]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }

        [Column("is_recommended")]
        [Required]
        public bool IsRecommended { get; set; }

        [Column("created_date")]
        [Required]
        public DateOnly CreatedDate { get; set; }

        // Navigation properties
        public Game? Game { get; set; }
        public User? User { get; set; }
    }
}