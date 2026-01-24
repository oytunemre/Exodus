using FarmazonDemo.Models.Dto.CartDto;

namespace FarmazonDemo.Services.Cart;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(int userId);
    Task<CartResponseDto> AddToCartAsync(AddToCartDto dto);
    Task<CartResponseDto> UpdateCartItemQuantityAsync(int userId, int cartItemId, int quantity);
    Task<CartResponseDto> RemoveItemAsync(int userId, int cartItemId);

}
