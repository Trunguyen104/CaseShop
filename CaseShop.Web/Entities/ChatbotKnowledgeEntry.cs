namespace CaseShop.Web.Entities;

public class ChatbotKnowledgeEntry
{
    public Guid Id { get; set; }
    public string IntentCode { get; set; } = null!;
    public ChatbotCategory Category { get; set; }
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
    public string Keywords { get; set; } = null!;
    public ChatbotActionType ActionType { get; set; }
    public string? ActionLabel { get; set; }
    public string? ActionUrl { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
