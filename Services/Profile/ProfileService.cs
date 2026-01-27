using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Files;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Profile
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public ProfileService(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<UserProfileResponseDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            return MapToProfileDto(user);
        }

        public async Task<UserProfileResponseDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Username))
            {
                // Check if username is taken
                var usernameExists = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower() && u.Id != userId);
                if (usernameExists)
                    throw new ConflictException("Username already taken");
                user.Username = dto.Username;
            }

            if (dto.Phone != null)
                user.Phone = dto.Phone;

            if (dto.AvatarUrl != null)
                user.AvatarUrl = dto.AvatarUrl;

            await _context.SaveChangesAsync();
            return MapToProfileDto(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
                throw new BadRequestException("Current password is incorrect");

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
        }

        public async Task<UserStatsDto> GetUserStatsAsync(int userId)
        {
            var stats = new UserStatsDto
            {
                TotalOrders = await _context.Orders.CountAsync(o => o.BuyerId == userId),
                PendingOrders = await _context.Orders.CountAsync(o => o.BuyerId == userId &&
                    (o.Status == Models.Enums.OrderStatus.Pending || o.Status == Models.Enums.OrderStatus.Processing)),
                TotalSpent = await _context.Orders
                    .Where(o => o.BuyerId == userId && o.Status == Models.Enums.OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount),
                AddressCount = await _context.Addresses.CountAsync(a => a.UserId == userId),
                UnreadNotifications = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead)
            };

            return stats;
        }

        public async Task<string> UploadAvatarAsync(int userId, IFormFile file)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await _fileService.DeleteAsync(user.AvatarUrl);
            }

            var result = await _fileService.UploadAsync(file, $"avatars/{userId}");
            user.AvatarUrl = result.Url;
            await _context.SaveChangesAsync();

            return result.Url;
        }

        public async Task DeleteAvatarAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await _fileService.DeleteAsync(user.AvatarUrl);
                user.AvatarUrl = null;
                await _context.SaveChangesAsync();
            }
        }

        #region Address Management

        public async Task<IEnumerable<AddressResponseDto>> GetAddressesAsync(int userId)
        {
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return addresses.Select(MapToAddressDto);
        }

        public async Task<AddressResponseDto> GetAddressByIdAsync(int userId, int addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                throw new NotFoundException("Address not found");

            return MapToAddressDto(address);
        }

        public async Task<AddressResponseDto> CreateAddressAsync(int userId, CreateAddressDto dto)
        {
            var address = new Address
            {
                UserId = userId,
                Title = dto.Title,
                FullName = dto.FullName,
                Phone = dto.Phone,
                City = dto.City,
                District = dto.District,
                Neighborhood = dto.Neighborhood,
                AddressLine = dto.AddressLine,
                PostalCode = dto.PostalCode,
                IsDefault = dto.IsDefault,
                Type = dto.Type
            };

            // If this is set as default, unset other defaults
            if (dto.IsDefault)
            {
                await UnsetDefaultAddresses(userId);
            }

            // If this is the first address, make it default
            var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
            if (!hasAddresses)
            {
                address.IsDefault = true;
            }

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return MapToAddressDto(address);
        }

        public async Task<AddressResponseDto> UpdateAddressAsync(int userId, int addressId, UpdateAddressDto dto)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                throw new NotFoundException("Address not found");

            if (dto.Title != null) address.Title = dto.Title;
            if (dto.FullName != null) address.FullName = dto.FullName;
            if (dto.Phone != null) address.Phone = dto.Phone;
            if (dto.City != null) address.City = dto.City;
            if (dto.District != null) address.District = dto.District;
            if (dto.Neighborhood != null) address.Neighborhood = dto.Neighborhood;
            if (dto.AddressLine != null) address.AddressLine = dto.AddressLine;
            if (dto.PostalCode != null) address.PostalCode = dto.PostalCode;
            if (dto.Type.HasValue) address.Type = dto.Type.Value;

            if (dto.IsDefault == true)
            {
                await UnsetDefaultAddresses(userId);
                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return MapToAddressDto(address);
        }

        public async Task DeleteAddressAsync(int userId, int addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                throw new NotFoundException("Address not found");

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            // If deleted address was default, set another as default
            if (address.IsDefault)
            {
                var firstAddress = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .FirstOrDefaultAsync();

                if (firstAddress != null)
                {
                    firstAddress.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                throw new NotFoundException("Address not found");

            await UnsetDefaultAddresses(userId);
            address.IsDefault = true;
            await _context.SaveChangesAsync();
        }

        private async Task UnsetDefaultAddresses(int userId)
        {
            var defaultAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            foreach (var addr in defaultAddresses)
            {
                addr.IsDefault = false;
            }
        }

        #endregion

        #region Notification Preferences

        public async Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(int userId)
        {
            var prefs = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (prefs == null)
            {
                // Create default preferences
                prefs = new NotificationPreferences { UserId = userId };
                _context.NotificationPreferences.Add(prefs);
                await _context.SaveChangesAsync();
            }

            return MapToPreferencesDto(prefs);
        }

        public async Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(int userId, UpdateNotificationPreferencesDto dto)
        {
            var prefs = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (prefs == null)
            {
                prefs = new NotificationPreferences { UserId = userId };
                _context.NotificationPreferences.Add(prefs);
            }

            // Email
            if (dto.EmailOrderUpdates.HasValue) prefs.EmailOrderUpdates = dto.EmailOrderUpdates.Value;
            if (dto.EmailPaymentUpdates.HasValue) prefs.EmailPaymentUpdates = dto.EmailPaymentUpdates.Value;
            if (dto.EmailShipmentUpdates.HasValue) prefs.EmailShipmentUpdates = dto.EmailShipmentUpdates.Value;
            if (dto.EmailPromotions.HasValue) prefs.EmailPromotions = dto.EmailPromotions.Value;
            if (dto.EmailNewsletter.HasValue) prefs.EmailNewsletter = dto.EmailNewsletter.Value;
            if (dto.EmailPriceAlerts.HasValue) prefs.EmailPriceAlerts = dto.EmailPriceAlerts.Value;
            if (dto.EmailStockAlerts.HasValue) prefs.EmailStockAlerts = dto.EmailStockAlerts.Value;

            // Push
            if (dto.PushOrderUpdates.HasValue) prefs.PushOrderUpdates = dto.PushOrderUpdates.Value;
            if (dto.PushPaymentUpdates.HasValue) prefs.PushPaymentUpdates = dto.PushPaymentUpdates.Value;
            if (dto.PushShipmentUpdates.HasValue) prefs.PushShipmentUpdates = dto.PushShipmentUpdates.Value;
            if (dto.PushPromotions.HasValue) prefs.PushPromotions = dto.PushPromotions.Value;
            if (dto.PushPriceAlerts.HasValue) prefs.PushPriceAlerts = dto.PushPriceAlerts.Value;
            if (dto.PushStockAlerts.HasValue) prefs.PushStockAlerts = dto.PushStockAlerts.Value;

            // SMS
            if (dto.SmsOrderUpdates.HasValue) prefs.SmsOrderUpdates = dto.SmsOrderUpdates.Value;
            if (dto.SmsShipmentUpdates.HasValue) prefs.SmsShipmentUpdates = dto.SmsShipmentUpdates.Value;
            if (dto.SmsPromotions.HasValue) prefs.SmsPromotions = dto.SmsPromotions.Value;

            await _context.SaveChangesAsync();
            return MapToPreferencesDto(prefs);
        }

        #endregion

        #region Mappers

        private static UserProfileResponseDto MapToProfileDto(Users user)
        {
            return new UserProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                EmailVerified = user.EmailVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        private static AddressResponseDto MapToAddressDto(Address address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                Title = address.Title,
                FullName = address.FullName,
                Phone = address.Phone,
                City = address.City,
                District = address.District,
                Neighborhood = address.Neighborhood,
                AddressLine = address.AddressLine,
                PostalCode = address.PostalCode,
                IsDefault = address.IsDefault,
                Type = address.Type
            };
        }

        private static NotificationPreferencesDto MapToPreferencesDto(NotificationPreferences prefs)
        {
            return new NotificationPreferencesDto
            {
                EmailOrderUpdates = prefs.EmailOrderUpdates,
                EmailPaymentUpdates = prefs.EmailPaymentUpdates,
                EmailShipmentUpdates = prefs.EmailShipmentUpdates,
                EmailPromotions = prefs.EmailPromotions,
                EmailNewsletter = prefs.EmailNewsletter,
                EmailPriceAlerts = prefs.EmailPriceAlerts,
                EmailStockAlerts = prefs.EmailStockAlerts,
                PushOrderUpdates = prefs.PushOrderUpdates,
                PushPaymentUpdates = prefs.PushPaymentUpdates,
                PushShipmentUpdates = prefs.PushShipmentUpdates,
                PushPromotions = prefs.PushPromotions,
                PushPriceAlerts = prefs.PushPriceAlerts,
                PushStockAlerts = prefs.PushStockAlerts,
                SmsOrderUpdates = prefs.SmsOrderUpdates,
                SmsShipmentUpdates = prefs.SmsShipmentUpdates,
                SmsPromotions = prefs.SmsPromotions
            };
        }

        #endregion
    }
}
