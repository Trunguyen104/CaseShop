using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaseShop.Web.Data.Configurations;

public class PhoneBrandConfiguration : IEntityTypeConfiguration<PhoneBrand>
{
    public void Configure(EntityTypeBuilder<PhoneBrand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
