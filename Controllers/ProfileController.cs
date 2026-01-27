using FarmazonDemo.Models.Dto;
using FarmazonDemo.Services.Profile;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserProfileResponseDto>> GetProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _profileService.GetProfileAsync(userId);
            return Ok(profile);
        }

        /// <summary>
        /// Update profile
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var profile = await _profileService.UpdateProfileAsync(userId, dto);
            return Ok(profile);
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            await _profileService.ChangePasswordAsync(userId, dto);
            return Ok(new { Message = "Password changed successfully" });
        }

        /// <summary>
        /// Get user stats (orders, addresses, etc.)
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetStats()
        {
            var userId = GetCurrentUserId();
            var stats = await _profileService.GetUserStatsAsync(userId);
            return Ok(stats);
        }

        /// <summary>
        /// Upload avatar
        /// </summary>
        [HttpPost("avatar")]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            var userId = GetCurrentUserId();
            var url = await _profileService.UploadAvatarAsync(userId, file);
            return Ok(new { AvatarUrl = url });
        }

        /// <summary>
        /// Delete avatar
        /// </summary>
        [HttpDelete("avatar")]
        public async Task<ActionResult> DeleteAvatar()
        {
            var userId = GetCurrentUserId();
            await _profileService.DeleteAvatarAsync(userId);
            return NoContent();
        }

        #region Addresses

        /// <summary>
        /// Get all addresses
        /// </summary>
        [HttpGet("addresses")]
        public async Task<ActionResult<IEnumerable<AddressResponseDto>>> GetAddresses()
        {
            var userId = GetCurrentUserId();
            var addresses = await _profileService.GetAddressesAsync(userId);
            return Ok(addresses);
        }

        /// <summary>
        /// Get address by ID
        /// </summary>
        [HttpGet("addresses/{id}")]
        public async Task<ActionResult<AddressResponseDto>> GetAddress(int id)
        {
            var userId = GetCurrentUserId();
            var address = await _profileService.GetAddressByIdAsync(userId, id);
            return Ok(address);
        }

        /// <summary>
        /// Create new address
        /// </summary>
        [HttpPost("addresses")]
        public async Task<ActionResult<AddressResponseDto>> CreateAddress([FromBody] CreateAddressDto dto)
        {
            var userId = GetCurrentUserId();
            var address = await _profileService.CreateAddressAsync(userId, dto);
            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, address);
        }

        /// <summary>
        /// Update address
        /// </summary>
        [HttpPut("addresses/{id}")]
        public async Task<ActionResult<AddressResponseDto>> UpdateAddress(int id, [FromBody] UpdateAddressDto dto)
        {
            var userId = GetCurrentUserId();
            var address = await _profileService.UpdateAddressAsync(userId, id, dto);
            return Ok(address);
        }

        /// <summary>
        /// Delete address
        /// </summary>
        [HttpDelete("addresses/{id}")]
        public async Task<ActionResult> DeleteAddress(int id)
        {
            var userId = GetCurrentUserId();
            await _profileService.DeleteAddressAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Set address as default
        /// </summary>
        [HttpPatch("addresses/{id}/default")]
        public async Task<ActionResult> SetDefaultAddress(int id)
        {
            var userId = GetCurrentUserId();
            await _profileService.SetDefaultAddressAsync(userId, id);
            return Ok(new { Message = "Default address updated" });
        }

        #endregion

        #region Notification Preferences

        /// <summary>
        /// Get notification preferences
        /// </summary>
        [HttpGet("notification-preferences")]
        public async Task<ActionResult<NotificationPreferencesDto>> GetNotificationPreferences()
        {
            var userId = GetCurrentUserId();
            var prefs = await _profileService.GetNotificationPreferencesAsync(userId);
            return Ok(prefs);
        }

        /// <summary>
        /// Update notification preferences
        /// </summary>
        [HttpPut("notification-preferences")]
        public async Task<ActionResult<NotificationPreferencesDto>> UpdateNotificationPreferences(
            [FromBody] UpdateNotificationPreferencesDto dto)
        {
            var userId = GetCurrentUserId();
            var prefs = await _profileService.UpdateNotificationPreferencesAsync(userId, dto);
            return Ok(prefs);
        }

        #endregion

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }
}
