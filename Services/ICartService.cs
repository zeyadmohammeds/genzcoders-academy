using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ICartService
{
    Task<CartDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddCourseAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> AddBundleAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto> ApplyPromoAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<CartDto> ApplyReferralAsync(Guid userId, string referralCode, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<Guid> CheckoutAsync(Guid userId, CheckoutCartRequest request, CancellationToken cancellationToken = default);
}
