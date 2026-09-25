using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Data.Configurations;

public class CustomDesignConfiguration : IEntityTypeConfiguration<CustomDesign>
{
    public void Configure(EntityTypeBuilder<CustomDesign> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviewImageUrl).IsRequired();
        builder.Property(x => x.DesignData).IsRequired();
    }
}
