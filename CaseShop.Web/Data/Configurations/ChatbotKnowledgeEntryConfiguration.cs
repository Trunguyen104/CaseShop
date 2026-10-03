using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaseShop.Web.Data.Configurations;

public class ChatbotKnowledgeEntryConfiguration : IEntityTypeConfiguration<ChatbotKnowledgeEntry>
{
    public void Configure(EntityTypeBuilder<ChatbotKnowledgeEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.IntentCode).IsRequired().HasMaxLength(80);
        builder.Property(entry => entry.Question).IsRequired().HasMaxLength(300);
        builder.Property(entry => entry.Answer).IsRequired().HasMaxLength(2000);
        builder.Property(entry => entry.Keywords).IsRequired().HasMaxLength(1000);
        builder.Property(entry => entry.ActionLabel).HasMaxLength(100);
        builder.Property(entry => entry.ActionUrl).HasMaxLength(500);

        builder.HasIndex(entry => entry.IntentCode).IsUnique();
        builder.HasIndex(entry => new { entry.IsActive, entry.Category, entry.Priority });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ChatbotKnowledgeEntries_Priority", "\"Priority\" >= 0 AND \"Priority\" <= 1000");
            table.HasCheckConstraint(
                "CK_ChatbotKnowledgeEntries_Action",
                "(\"ActionType\" = 0 AND \"ActionUrl\" IS NULL) OR (\"ActionType\" = 1 AND \"ActionUrl\" IS NOT NULL)");
        });
    }
}
