using AutoMapper;
using Restaurants.Dtos;
using Restaurants.Models;

namespace Restaurants.Profiles
{
    public class RestaurantsProfile : Profile
    {
        public RestaurantsProfile()
        {
            // source -> target  
            CreateMap<Restaurant, RestaurantReadDto>();
            CreateMap<RestaurantCreateDto, Restaurant>();
            CreateMap<RestaurantUpdateDto, Restaurant>();
            CreateMap<Restaurant, RestaurantUpdateDto>();
        }
    }
}
