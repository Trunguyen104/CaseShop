namespace CaseShop.Web.Services.Email;

public interface IOrderConfirmationEmailService
{
    Task<bool> SendAsync(Guid orderId, bool allowRetry = false, CancellationToken cancellationToken = default);
}
