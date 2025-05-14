using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "OrderItem")]
    public class OrderItems
    {
        [Column("order_id")]
        [ForeignKey("Order")]
        [Required]
        public int OrderId { get; set; }

        [Column("game_id")]
        [ForeignKey("Game")]
        [Required]
        public int GameId { get; set; }

        [Column("price")]
        [Required]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between 0.01 and 9999.99")]
        public decimal Price { get; set; }

        [Column("quantity")]
        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }

        // Corrected navigation properties - should be single entities, not collections
        public Order? Order { get; set; }
        public Game? Game { get; set; }
    }
}