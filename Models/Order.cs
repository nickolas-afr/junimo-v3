using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "Order")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        [Required]
        public string UserId { get; set; }

        [Column("order_date")]
        [Required]
        public DateTime OrderDate { get; set; }

        [Column("total")]
        [Required]
        public float Total { get; set; }

        [Column("status")]
        [Required]
        public OrderStatus Status { get; set; }

        public User? User { get; set; }
        public ICollection<OrderItems>? OrderItems { get; set; }
    }
}