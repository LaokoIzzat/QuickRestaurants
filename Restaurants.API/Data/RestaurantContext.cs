using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Restaurants.Models;

namespace Restaurants.Data
{
    public class RestaurantContext : IdentityDbContext
    {
        public RestaurantContext(DbContextOptions<RestaurantContext> opt) : base(opt)
        {

        }

        // Represent Restaurant object down to database as a dbset, called Restaurants
        // we need this mapping of model object down to db with dbset for all models, we just have restaurant rn
        public DbSet<Restaurant> Restaurants { get; set; }

    }
}