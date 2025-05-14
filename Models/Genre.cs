using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace junimo_v3.Models
{
    [Table(name: "Genre")]
    public class Genre
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("genre_id")]
        public int GenreId { get; set; }

        [Column("name")]
        [Required]
        public string Name { get; set; }
    }
}