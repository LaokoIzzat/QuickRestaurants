using Restaurants.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurants.Data
{
    public interface IRestaurantsRepo
    {
        Task<bool> SaveChangesAsync();
        Task<IEnumerable<Restaurant>> GetAllRestaurantsAsync();
        Task<Restaurant> GetRestaurantByIdAsync(int id);
        Task<Restaurant> GetRestaurantByNameAsync(string name);
        void CreateRestaurant(Restaurant restaurant);
        void UpdateRestaurant(Restaurant restaurant);
        void DeleteRestaurant(Restaurant restaurant);
    }
}