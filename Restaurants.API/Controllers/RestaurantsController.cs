using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Restaurants.Data;
using Restaurants.Dtos;
using Restaurants.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurants.Controllers
{
    // api/restaurants
    [Route("api/v1/restaurants")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RestaurantsController : ControllerBase
    {
        private readonly IRestaurantsRepo _repository;
        private readonly IMapper _mapper;

        // dependency injected repo is passed to the private read only field _repository
        public RestaurantsController(IRestaurantsRepo repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // GET api/v1/restaurants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RestaurantReadDto>>> GetAllRestaurantsAsync()
        {
            var restaurantItems = await _repository.GetAllRestaurantsAsync();

            return Ok(_mapper.Map<IEnumerable<RestaurantReadDto>>(restaurantItems));
        }

        // GET api/v1/restaurants/{id}
        [HttpGet("{id}", Name = "GetRestaurantById")]
        public async Task<ActionResult<RestaurantReadDto>> GetRestaurantByIdAsync(int id)
        {
            var restaurantItem = await _repository.GetRestaurantByIdAsync(id);
            if (restaurantItem != null)
            {
                return Ok(_mapper.Map<RestaurantReadDto>(restaurantItem));
            }
            else
            {
                return NotFound();
            }
        }

        // POST api/v1/restaurants
        [HttpPost]
        public async Task<ActionResult<RestaurantReadDto>> CreateRestaurantAsync(RestaurantCreateDto restaurantCreateDto)
        {
            var restaurantModel = _mapper.Map<Restaurant>(restaurantCreateDto);
            _repository.CreateRestaurant(restaurantModel);
            await _repository.SaveChangesAsync();

            var restaurantReadDto = _mapper.Map<RestaurantReadDto>(restaurantModel);

            // the below return method gives location header too (RESTFUL)
            return CreatedAtRoute(nameof(GetRestaurantByIdAsync), new { Id = restaurantReadDto.Id }, restaurantReadDto);
        }

        // PUT api/v1/restaurants/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateRestaurantAsync(int id, RestaurantUpdateDto restaurantUpdateDto)
        {
            var restaurantModelFromRepo = await _repository.GetRestaurantByIdAsync(id);

            if (restaurantModelFromRepo == null)
            {
                return NotFound();
            }

            _mapper.Map(restaurantUpdateDto, restaurantModelFromRepo);

            _repository.UpdateRestaurant(restaurantModelFromRepo);

            await _repository.SaveChangesAsync();

            return NoContent();
        }

        // PATCH api/v1/restaurants/{id}
        [HttpPatch("{id}")]
        public async Task<ActionResult> PartialRestaurantUpdateAsync(int id, JsonPatchDocument<RestaurantUpdateDto> patchDoc)
        {
            var restaurantModelFromRepo = await _repository.GetRestaurantByIdAsync(id);

            if (restaurantModelFromRepo == null)
            {
                return NotFound();
            }

            var restaurantToPatch = _mapper.Map<RestaurantUpdateDto>(restaurantModelFromRepo);
            patchDoc.ApplyTo(restaurantToPatch, ModelState);

            if (!TryValidateModel(restaurantToPatch))
            {
                return ValidationProblem(ModelState);
            }

            _mapper.Map(restaurantToPatch, restaurantModelFromRepo);

            _repository.UpdateRestaurant(restaurantModelFromRepo);

            await _repository.SaveChangesAsync();

            return NoContent();
        }

        // DELETE api/v1/restaurants/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRestaurantAsync(int id)
        {
            // check if the restaurant id is found, if not return not found
            var restaurantModelFromRepo = await _repository.GetRestaurantByIdAsync(id);
            if (restaurantModelFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteRestaurant(restaurantModelFromRepo);
            await _repository.SaveChangesAsync();

            return NoContent();
        }

    }
}