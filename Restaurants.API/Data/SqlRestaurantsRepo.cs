using Restaurants.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Restaurants.Data
{
    public class SqlRestaurantsRepo : IRestaurantsRepo
    {
        private readonly RestaurantContext _context;

        public SqlRestaurantsRepo(RestaurantContext context)
        {
            _context = context;
        }

        public void CreateRestaurant(Restaurant restaurant)
        {
            if (restaurant == null)
            {
                throw new ArgumentNullException(nameof(restaurant));
            }

            _context.Restaurants.Add(restaurant);
        }

        public void DeleteRestaurant(Restaurant restaurant)
        {
            if (restaurant == null)
            {
                throw new ArgumentNullException(nameof(restaurant));
            }

            _context.Restaurants.Remove(restaurant);
        }

        public async Task<IEnumerable<Restaurant>> GetAllRestaurantsAsync()
        {
            return await _context.Restaurants.ToListAsync();
        }

        public async Task<Restaurant> GetRestaurantByIdAsync(int id)
        {
            return await _context.Restaurants.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Restaurant> GetRestaurantByNameAsync(string name)
        {
            return await _context.Restaurants.FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync()) >= 0;
        }

        // Implemented for the sake of implementing our interface; do not need to actually implement the update functionality as it works out the box
        public void UpdateRestaurant(Restaurant restaurant)
        {
        }
    }
}
