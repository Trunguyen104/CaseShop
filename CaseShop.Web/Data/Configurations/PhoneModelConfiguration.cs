using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaseShop.Web.Data.Configurations;

public class PhoneModelConfiguration : IEntityTypeConfiguration<PhoneModel>
{
    public void Configure(EntityTypeBuilder<PhoneModel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.PhoneBrandId, x.Slug }).IsUnique();
        builder.HasOne(x => x.PhoneBrand).WithMany(x => x.PhoneModels)
            .HasForeignKey(x => x.PhoneBrandId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.MaskImageUrl).HasMaxLength(2048);
        builder.Property(x => x.OverlayImageUrl).HasMaxLength(2048);

        foreach (var propertyName in GeometryProperties)
            builder.Property<decimal>(propertyName).HasPrecision(12, 4);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_PhoneModels_Canvas", "\"CanvasWidth\" > 0 AND \"CanvasHeight\" > 0 AND \"CornerRadius\" >= 0");
            // SafeArea and PrintArea may be all-zero (unconfigured). When set, must be valid rectangles inside canvas.
            table.HasCheckConstraint("CK_PhoneModels_SafeArea",
                "(\"SafeAreaWidth\" = 0 AND \"SafeAreaHeight\" = 0) OR " +
                "(\"SafeAreaX\" >= 0 AND \"SafeAreaY\" >= 0 AND \"SafeAreaWidth\" > 0 AND \"SafeAreaHeight\" > 0 AND " +
                "\"SafeAreaX\" + \"SafeAreaWidth\" <= \"CanvasWidth\" AND \"SafeAreaY\" + \"SafeAreaHeight\" <= \"CanvasHeight\")");
            table.HasCheckConstraint("CK_PhoneModels_PrintArea",
                "(\"PrintAreaWidth\" = 0 AND \"PrintAreaHeight\" = 0) OR " +
                "(\"PrintAreaX\" >= 0 AND \"PrintAreaY\" >= 0 AND \"PrintAreaWidth\" > 0 AND \"PrintAreaHeight\" > 0 AND " +
                "\"PrintAreaX\" + \"PrintAreaWidth\" <= \"CanvasWidth\" AND \"PrintAreaY\" + \"PrintAreaHeight\" <= \"CanvasHeight\")");
            table.HasCheckConstraint("CK_PhoneModels_CameraCutout",
                "(\"CameraCutoutWidth\" = 0 AND \"CameraCutoutHeight\" = 0) OR " +
                "(\"CameraCutoutX\" >= 0 AND \"CameraCutoutY\" >= 0 AND \"CameraCutoutWidth\" > 0 AND \"CameraCutoutHeight\" > 0 AND " +
                "\"CameraCutoutRadius\" >= 0 AND " +
                "\"CameraCutoutX\" + \"CameraCutoutWidth\" <= \"CanvasWidth\" AND \"CameraCutoutY\" + \"CameraCutoutHeight\" <= \"CanvasHeight\")");
            table.HasCheckConstraint("CK_PhoneModels_Bleed", "\"BleedTop\" >= 0 AND \"BleedRight\" >= 0 AND \"BleedBottom\" >= 0 AND \"BleedLeft\" >= 0");
        });
    }

    private static readonly string[] GeometryProperties =
    [
        nameof(PhoneModel.CanvasWidth), nameof(PhoneModel.CanvasHeight), nameof(PhoneModel.CornerRadius),
        nameof(PhoneModel.SafeAreaX), nameof(PhoneModel.SafeAreaY), nameof(PhoneModel.SafeAreaWidth), nameof(PhoneModel.SafeAreaHeight),
        nameof(PhoneModel.PrintAreaX), nameof(PhoneModel.PrintAreaY), nameof(PhoneModel.PrintAreaWidth), nameof(PhoneModel.PrintAreaHeight),
        nameof(PhoneModel.BleedTop), nameof(PhoneModel.BleedRight), nameof(PhoneModel.BleedBottom), nameof(PhoneModel.BleedLeft),
        nameof(PhoneModel.CameraCutoutX), nameof(PhoneModel.CameraCutoutY), nameof(PhoneModel.CameraCutoutWidth),
        nameof(PhoneModel.CameraCutoutHeight), nameof(PhoneModel.CameraCutoutRadius)
    ];
}
