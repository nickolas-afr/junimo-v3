using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "UserFriendship")]
    public class UserFriendship
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        [Required]
        public string UserId { get; set; }

        [Column("friend_id")]
        [ForeignKey("Friend")]
        [Required]
        public string FriendId { get; set; }

        [Column("status")]
        [Required]
        public FriendshipStatus Status { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }

        [ForeignKey("FriendId")]
        public User? Friend { get; set; }
    }

    public enum FriendshipStatus
    {
        Pending,
        Accepted
    }
}
