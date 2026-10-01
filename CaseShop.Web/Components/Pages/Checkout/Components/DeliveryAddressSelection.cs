namespace CaseShop.Web.Components.Pages.Checkout.Components;

public sealed record DeliveryAddressSelection(
    int ProvinceCode,
    string ProvinceName,
    int WardCode,
    string WardName,
    string AddressLine);
