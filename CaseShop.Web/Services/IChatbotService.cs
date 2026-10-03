using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services;

public interface IChatbotService
{
    Task<ChatbotResponseDto> GetResponseAsync(
        ChatbotRequestDto request,
        string clientKey,
        CancellationToken cancellationToken = default);
}
