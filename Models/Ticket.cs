using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "Ticket")]
    public class Ticket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ticket_id")]
        public int ticketId { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        [Required]
        public string userId { get; set; }

        [Column("issue_type")]
        [Required]
        public string issueType { get; set; }

        [Column("description")]
        [Required]
        public string description { get; set; }

        [Column("status")]
        public string status { get; set; } = "Open";

        [Column("created_date")]
        public DateOnly createdDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Column("resolved_date")]
        public DateOnly? resolvedDate { get; set; }

        public User? User { get; set; }
    }
}