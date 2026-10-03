using CaseShop.Web.Data;
using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseShop.Web.Repositories;

public sealed class ChatbotKnowledgeRepository : IChatbotKnowledgeRepository
{
    private readonly AppDbContext _context;

    public ChatbotKnowledgeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ChatbotKnowledgeEntry>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatbotKnowledgeEntries
            .AsNoTracking()
            .Where(entry => entry.IsActive)
            .OrderByDescending(entry => entry.Priority)
            .ThenBy(entry => entry.IntentCode)
            .Take(500)
            .ToListAsync(cancellationToken);
    }
}
