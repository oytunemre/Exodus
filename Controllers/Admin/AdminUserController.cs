using FarmazonDemo.Data;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Audit;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AdminUserController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAllUsers(
            [FromQuery] string? search,
            [FromQuery] UserRole? role,
            [FromQuery] bool? emailVerified,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.Username.ToLower().Contains(search));
            }

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (emailVerified.HasValue)
                query = query.Where(u => u.EmailVerified == emailVerified.Value);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Username,
                    u.Role,
                    u.EmailVerified,
                    u.TwoFactorEnabled,
                    IsLocked = u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Get user by ID with detailed stats
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Username,
                    u.Phone,
                    u.Role,
                    u.IsActive,
                    u.EmailVerified,
                    u.TwoFactorEnabled,
                    IsLocked = u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow,
                    u.LockoutEndTime,
                    u.FailedLoginAttempts,
                    u.LastLoginAt,
                    u.CreatedAt,
                    u.UpdatedAt,
                    // Stats
                    OrderCount = _context.Orders.Count(o => o.BuyerId == u.Id),
                    TotalSpent = _context.Orders
                        .Where(o => o.BuyerId == u.Id && o.Status == OrderStatus.Completed)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0,
                    ListingCount = u.Role == UserRole.Seller
                        ? _context.Listings.Count(l => l.SellerId == u.Id)
                        : 0,
                    AddressCount = _context.Addresses.Count(a => a.UserId == u.Id)
                })
                .FirstOrDefaultAsync();

            if (user == null)
                throw new NotFoundException("User not found");

            return Ok(user);
        }

        /// <summary>
        /// Update user information
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists)
                    throw new BadRequestException("Email already in use");
                user.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.Phone))
                user.Phone = dto.Phone;

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            if (dto.EmailVerified.HasValue)
                user.EmailVerified = dto.EmailVerified.Value;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User updated successfully", UserId = id });
        }

        /// <summary>
        /// Toggle user active status
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            // Prevent deactivating last admin
            if (user.Role == UserRole.Admin && user.IsActive)
            {
                var activeAdminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);
                if (activeAdminCount == 0)
                    throw new BadRequestException("Cannot deactivate the last active admin");
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = user.IsActive ? "User activated" : "User deactivated", UserId = id, IsActive = user.IsActive });
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            var tempPassword = Guid.NewGuid().ToString("N")[..8];
            user.Password = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.LockoutEndTime = null;
            user.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            // In production, send via email
            return Ok(new
            {
                Message = "Password reset successfully",
                UserId = id,
                TemporaryPassword = tempPassword
            });
        }

        /// <summary>
        /// Change user role
        /// </summary>
        [HttpPatch("{id}/role")]
        public async Task<ActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            var oldRole = user.Role.ToString();
            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            // Log the role change
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditService.LogRoleChangeAsync(id, oldRole, dto.Role.ToString(), adminId);

            return Ok(new { Message = $"User role changed to {dto.Role}" });
        }

        /// <summary>
        /// Unlock user account
        /// </summary>
        [HttpPost("{id}/unlock")]
        public async Task<ActionResult> UnlockAccount(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            user.LockoutEndTime = null;
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Account unlocked successfully" });
        }

        /// <summary>
        /// Lock user account
        /// </summary>
        [HttpPost("{id}/lock")]
        public async Task<ActionResult> LockAccount(int id, [FromBody] LockAccountDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            user.LockoutEndTime = DateTime.UtcNow.AddMinutes(dto.DurationMinutes);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Account locked until {user.LockoutEndTime}" });
        }

        /// <summary>
        /// Delete user (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            // Prevent deleting yourself
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId)
                throw new BadRequestException("Cannot delete your own account");

            _context.Users.Remove(user); // Soft delete via SaveChanges override
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get user statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ByRole = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync(),
                VerifiedEmails = await _context.Users.CountAsync(u => u.EmailVerified),
                TwoFactorEnabled = await _context.Users.CountAsync(u => u.TwoFactorEnabled),
                LockedAccounts = await _context.Users.CountAsync(u => u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow),
                NewUsersToday = await _context.Users.CountAsync(u => u.CreatedAt.Date == DateTime.UtcNow.Date),
                NewUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            };

            return Ok(stats);
        }
    }

    public class ChangeRoleDto
    {
        public UserRole Role { get; set; }
    }

    public class LockAccountDto
    {
        public int DurationMinutes { get; set; } = 60;
    }

    public class AdminUpdateUserDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public bool? EmailVerified { get; set; }
    }
}
