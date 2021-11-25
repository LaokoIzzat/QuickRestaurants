namespace Restaurants.Dtos
{
    public class RestaurantReadDto
    {
        // I will not be removing any fields in my dto as there is no need however, sensitive info should be
        // removed in the dto if there exists any (e.g. DOB)

        public int Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public int Rating { get; set; }
    }
}