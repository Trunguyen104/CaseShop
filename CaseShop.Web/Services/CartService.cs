using CaseShop.Web.DTOs;
using Microsoft.JSInterop;

namespace CaseShop.Web.Services;

public sealed class CartService : ICartService
{
    private readonly IJSRuntime _js;

    public CartService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<IReadOnlyList<ClientCartItem>> GetItemsAsync() =>
        await _js.InvokeAsync<List<ClientCartItem>>("CaseShopCart.getItems");

    public Task AddAsync(ClientCartItem item) =>
        _js.InvokeVoidAsync("CaseShopCart.addItem", item).AsTask();

    public Task UpdateQuantityAsync(string key, int quantity) =>
        _js.InvokeVoidAsync("CaseShopCart.updateQuantity", key, quantity).AsTask();

    public Task RemoveAsync(string key) =>
        _js.InvokeVoidAsync("CaseShopCart.removeItem", key).AsTask();

    public Task ClearAsync() =>
        _js.InvokeVoidAsync("CaseShopCart.clear").AsTask();
}
