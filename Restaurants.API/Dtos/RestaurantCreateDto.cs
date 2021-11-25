using System.ComponentModel.DataAnnotations;

namespace Restaurants.Dtos
{
    public class RestaurantCreateDto
    {
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