using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public interface IChatbotKnowledgeRepository
{
    Task<IReadOnlyList<ChatbotKnowledgeEntry>> GetActiveAsync(CancellationToken cancellationToken = default);
}
