using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services;

public interface ICartService
{
    Task<IReadOnlyList<ClientCartItem>> GetItemsAsync();
    Task AddAsync(ClientCartItem item);
    Task UpdateQuantityAsync(string key, int quantity);
    Task RemoveAsync(string key);
    Task ClearAsync();
}
