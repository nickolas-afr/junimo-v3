using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace junimo_v3.Models
{
    [Table(name: "User")]
    public class User : IdentityUser
    {
        [Column("profile_picture")]
        public byte[]? ProfilePicture { get; set; }

        [Column("profile_picture_content_type")]
        public string? ProfilePictureContentType { get; set; }

        [NotMapped] // This will keep the original property but won't be stored in DB
        public string? ProfilePictureURL => ProfilePicture != null ? 
            $"data:{ProfilePictureContentType};base64,{Convert.ToBase64String(ProfilePicture)}" : null;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_online")]
        public DateTime? LastOnlineAt { get; set; } = DateTime.UtcNow;

        public ICollection<Order>? Orders { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Ticket>? Tickets { get; set; }
        public ICollection<Game>? Games { get; set; }

        [InverseProperty("User")]
        public ICollection<UserFriendship>? FriendshipsInitiated { get; set; }

        [InverseProperty("Friend")]
        public ICollection<UserFriendship>? FriendshipsReceived { get; set; }
    }
}
