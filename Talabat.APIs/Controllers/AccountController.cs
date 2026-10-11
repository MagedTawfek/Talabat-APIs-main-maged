using System.Security.Claims;

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.APIs.Extensions;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using Talabat.Core.Models.Identity;

namespace Talabat.APIs.Controllers
{
    [Authorize]
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly AppIdentityDbContext _identityDbContext;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IMapper mapper ,IAuthService authService, IConfiguration config, AppIdentityDbContext identityDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _authService = authService;
            _config = config;
            _identityDbContext = identityDbContext;
        }
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized(new ApiResponse(401));
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);
            if (!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(401));
            }
            return new UserDto
            {
                Email = user.Email,
                Token = await _authService.CreateTokenAsync(user, _userManager),
                RefreshToken = await IssueRefreshTokenAsync(user),
                DisplayName = user.DisplayName
            };
        }
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) is not null)
            {
                return BadRequest(new ApiValidationErrorResponse(){Errors =  new []{"Email address is in use"}});
            }
            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiValidationErrorResponse
                {
                    Errors = result.Errors.Select(error => error.Description)
                });
            }

            var addCustomerRoleResult = await _userManager.AddToRoleAsync(user, "Customer");
            if (!addCustomerRoleResult.Succeeded)
            {
                var cleanupResult = await _userManager.DeleteAsync(user);
                if (!cleanupResult.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(
                        StatusCodes.Status500InternalServerError,
                        "The account could not be assigned its default role or safely removed."));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(
                    StatusCodes.Status500InternalServerError,
                    "The account could not be assigned its default role."));
            }

            return new UserDto
            {
                Email = user.Email,
                Token = await _authService.CreateTokenAsync(user, _userManager),
                RefreshToken = await IssueRefreshTokenAsync(user),
                DisplayName = user.DisplayName
            };
        }   
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            var tokenHash = _authService.HashRefreshToken(refreshTokenDto.RefreshToken);
            var storedToken = await _identityDbContext.UserRefreshTokens
                .Include(token => token.User)
                .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);
            var now = DateTime.UtcNow;
            if (storedToken is null || storedToken.ExpiresAtUtc <= now)
                return Unauthorized(new ApiResponse(401, "Invalid or expired refresh token"));

            if (storedToken.RevokedAtUtc is not null)
            {
                await RevokeRefreshFamilyAsync(storedToken.FamilyId, now);
                return Unauthorized(new ApiResponse(401, "Refresh token reuse detected; sign in again"));
            }

            if (storedToken.UserSecurityStamp != storedToken.User.SecurityStamp)
            {
                await RevokeRefreshFamilyAsync(storedToken.FamilyId, now);
                return Unauthorized(new ApiResponse(401, "Account session has been revoked"));
            }

            var nextRefreshToken = _authService.CreateRefreshToken();
            var nextTokenHash = _authService.HashRefreshToken(nextRefreshToken);
            storedToken.RevokedAtUtc = now;
            storedToken.ReplacedByTokenHash = nextTokenHash;
            _identityDbContext.UserRefreshTokens.Add(new UserRefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = nextTokenHash,
                FamilyId = storedToken.FamilyId,
                UserSecurityStamp = storedToken.User.SecurityStamp!,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(double.Parse(_config["JWT:RefreshTokenLifeTimeDays"]!, System.Globalization.CultureInfo.InvariantCulture))
            });
            try
            {
                await _identityDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await RevokeRefreshFamilyAsync(storedToken.FamilyId, now);
                return Unauthorized(new ApiResponse(401, "Invalid or expired refresh token"));
            }

            return new UserDto
            {
                Email = storedToken.User.Email,
                DisplayName = storedToken.User.DisplayName,
                Token = await _authService.CreateTokenAsync(storedToken.User, _userManager),
                RefreshToken = nextRefreshToken
            };
        }

        private async Task<string> IssueRefreshTokenAsync(AppUser user)
        {
            var rawToken = _authService.CreateRefreshToken();
            var now = DateTime.UtcNow;
            _identityDbContext.UserRefreshTokens.Add(new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = _authService.HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid().ToString("N"),
                UserSecurityStamp = user.SecurityStamp!,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(double.Parse(_config["JWT:RefreshTokenLifeTimeDays"]!, System.Globalization.CultureInfo.InvariantCulture))
            });
            await _identityDbContext.SaveChangesAsync();
            return rawToken;
        }

        [HttpGet]
        public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
            return user is null ? NotFound(new ApiResponse(404, "User not found")) :
                Ok(new UserProfileDto(user.Email!, user.DisplayName));
        }
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetUserAddress()
        {
            var user = await _userManager.FindUserWithAddressAsync(User);
            if (user is null) return NotFound(new ApiResponse(404, "User not found"));
            var address = _mapper.Map<Address, AddressDto>(user.Address);
            return Ok(address);
        }
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto addressDto)
        {
            var user = await _userManager.FindUserWithAddressAsync(User);
            if (user is null) return NotFound(new ApiResponse(404, "User not found"));
            user.Address = _mapper.Map<AddressDto, Address>(addressDto);
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return Ok(_mapper.Map<Address, AddressDto>(user.Address));
            return BadRequest("Problem updating the user");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }
        
        [HttpPut("update")]
        public async Task<ActionResult<UserProfileDto>> UpdateUser(UpdateUserDto updateUserDto)
        {
            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user == null)
            {
                return NotFound(new ApiResponse(404, "User not found"));
            }

            user.DisplayName = updateUserDto.DisplayName;
            user.PhoneNumber = updateUserDto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse(400, "Problem updating the user"));
            }

            return Ok(new UserProfileDto(user.Email!, user.DisplayName));
        }
        [HttpPut("updatepassword")]
        public async Task<ActionResult> UpdateUserPassword([FromBody] Dictionary<string, string> passwords)
        {
            if (!passwords.ContainsKey("currentPassword") || !passwords.ContainsKey("newPassword"))
            {
                return BadRequest(new ApiResponse(400, "Current and new passwords are required"));
            }

            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user == null)
            {
                return NotFound(new ApiResponse(404, "User not found"));
            }

            var result = await _userManager.ChangePasswordAsync(user, passwords["currentPassword"], passwords["newPassword"]);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse(400, "Problem updating the password"));
            }

            await RevokeRefreshTokensAsync(user.Id);

            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is not null)
                {
                    var stampResult = await _userManager.UpdateSecurityStampAsync(user);
                    if (!stampResult.Succeeded)
                        return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(500, "Could not revoke account sessions"));
                }
                await RevokeRefreshTokensAsync(userId);
            }
            return NoContent();
        }

        private Task<int> RevokeRefreshTokensAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return _identityDbContext.UserRefreshTokens
                .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now));
        }

        private Task<int> RevokeRefreshFamilyAsync(string familyId, DateTime revokedAtUtc)
        {
            return _identityDbContext.UserRefreshTokens
                .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc));
        }
    }
}
