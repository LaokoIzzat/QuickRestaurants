using System.ComponentModel.DataAnnotations;

namespace Restaurants.API.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Location { get; set; }

        [Required]
        public int Rating { get; set; }
    }
}