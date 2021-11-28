using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Restaurants.API.Dtos.Requests;
using Restaurants.API.Dtos.Responses;
using Restaurants.API.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Restaurants.Data;
using Restaurants.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Restaurants.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParams;
        private readonly RestaurantContext _restaurantContext;

        public AuthController(UserManager<IdentityUser> userManager, IOptionsMonitor<JwtSettings> optionsMonitor, TokenValidationParameters tokenValidationParams, RestaurantContext restaurantContext)
        {
            _userManager = userManager;
            _jwtSettings = optionsMonitor.CurrentValue;
            _tokenValidationParams = tokenValidationParams;
            _restaurantContext = restaurantContext;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDto user)
        {
            if (ModelState.IsValid)
            {
                // We can utilise the model
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if(existingUser != null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>
                        {
                        "Email is already in use"
                        },
                        Success = false
                    });
                }

                var newUser = new IdentityUser() { Email = user.Email, UserName = user.Username };
                var isCreated = await _userManager.CreateAsync(newUser, user.Password);

                if (isCreated.Succeeded)
                {
                    var jwtToken = await GenerateJwtTokenAsync(newUser);

                    return Ok(jwtToken);
                } 
                else
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                        Success = false
                    });
                }
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>
                {
                    "Invalid payload"
                },
                Success = false
            });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if(existingUser == null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>
                        {
                            "Invalid login request"
                        },
                        Success = false
                    });
                }

                var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

                if (!isCorrect)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>
                        {
                            "Invalid payload"
                        },
                        Success = false
                    });
                }

                var jwtToken = await GenerateJwtTokenAsync(existingUser);

                return Ok(jwtToken);
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>
                {
                    "Invalid payload"
                },
                Success = false
            });
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                var result = await VerifyAndGenerateTokenAsync(tokenRequest);
                
                if (result == null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>()
                        {
                            "Invalid token"
                        },
                        Success = false
                    });
                }

                return Ok(result);
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid Payload"
                },
                Success = false
            });
        }

        private async Task<AuthResult> VerifyAndGenerateTokenAsync(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // JWT Token format check
                var tokenBeingVerified = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParams, out var validatedToken);

                // Encryption algorithm check
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (result == false)
                    {
                        return null;
                    }
                }

                // Expiry date check
                var utcExpiryDate = long.Parse(tokenBeingVerified.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token not expired yet"
                        }
                    };
                }

                // check that the token exists 
                var storedToken = await _restaurantContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token does not exist"
                        }
                    };
                }

                // check if the token is used

                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token has been used"
                        }
                    };
                }

                // check if token is revoked 
                if (storedToken.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token has been revoked"
                        }
                    };
                }

                // validate the id of the token
                var jti = tokenBeingVerified.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (storedToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token id doesn't match"
                        }
                    };
                }

                // checks passed, update current token

                storedToken.IsUsed = true;
                _restaurantContext.RefreshTokens.Update(storedToken);
                await _restaurantContext.SaveChangesAsync();

                // generate new token
                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                return await GenerateJwtTokenAsync(dbUser);
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("Lifetime validation failed. The token has expired."))
                {

                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token has expired, please re-login"
                        }
                    };

                }
                else
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Something went wrong."
                        }
                    };
                }
            }
        }

        

        private async Task<AuthResult> GenerateJwtTokenAsync(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new [] 
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddSeconds(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            // generates security token
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            // generates the actual token string that we can utilise
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpriryDate = DateTime.UtcNow.AddMonths(6),
                Token = RandomString(35) + Guid.NewGuid()
            };

            await _restaurantContext.RefreshTokens.AddAsync(refreshToken);
            await _restaurantContext.SaveChangesAsync();

            return new AuthResult()
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTimeVal;
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(x => x[random.Next(x.Length)]).ToArray());
        }
    }
}
