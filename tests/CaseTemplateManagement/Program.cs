// P2.3 CaseTemplate Admin Management test suite
// Tests: CRUD, duplicate rejection, geometry validation, deactivation, relation preservation,
//        authorization declarations, AdminShell nav, preview component existence.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CaseShop.Web.Data;
using CaseShop.Web.DTOs;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== P2.3 CaseTemplate Admin Management Test ===");

// ── Database setup ──────────────────────────────────────────────────────────
var settings = JsonDocument.Parse(await File.ReadAllTextAsync(
    Path.Combine("CaseShop.Web", "appsettings.Development.json")));
var connStr = settings.RootElement
    .GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connStr).Options;
await using var db = new AppDbContext(options);
await using var tx = await db.Database.BeginTransactionAsync();

var repo = new CaseTemplateRepository(db);
var svc  = new CaseTemplateService(repo);

try
{
    var suffix = Guid.NewGuid().ToString("N")[..8];

    // ── 1. Setup dependencies ────────────────────────────────────────────────
    var brand = await svc.CreatePhoneBrandAsync(new() { Name = $"P23 Brand {suffix}", Slug = $"p23-brand-{suffix}" });
    var model = await svc.CreatePhoneModelAsync(new() { PhoneBrandId = brand.Id, Name = $"P23 Model {suffix}", Slug = $"p23-model-{suffix}" });
    var ctype = await svc.CreateCaseTypeAsync(new() { Name = $"P23 Type {suffix}", Slug = $"p23-type-{suffix}" });

    // ── 2. Create template ────────────────────────────────────────────────────
    var tmpl = await svc.CreateCaseTemplateAsync(new()
    {
        PhoneModelId = model.Id, CaseTypeId = ctype.Id,
        Name = "P23 Template", Slug = $"p23-tmpl-{suffix}",
        CanvasWidth = 1200, CanvasHeight = 2600, CornerRadius = 40,
        SafeAreaX = 50, SafeAreaY = 80, SafeAreaWidth = 1100, SafeAreaHeight = 2440,
        PrintAreaX = 30, PrintAreaY = 60, PrintAreaWidth = 1140, PrintAreaHeight = 2480,
        BleedTop = 20, BleedRight = 15, BleedBottom = 20, BleedLeft = 15
    });
    Assert(tmpl.Id != Guid.Empty, "Create: template ID must not be empty.");
    Assert(tmpl.Name == "P23 Template", "Create: name mismatch.");
    Assert(tmpl.Slug == $"p23-tmpl-{suffix}", "Create: slug mismatch.");
    Assert(tmpl.PhoneBrandName == brand.Name, "Create: brand name not populated.");
    Assert(tmpl.IsActive, "Create: new template must be active.");
    Console.WriteLine("  ✓ Create template");

    // ── 3. Read back ──────────────────────────────────────────────────────────
    var fetched = await svc.GetCaseTemplateAsync(tmpl.Id);
    Assert(fetched is not null, "GetCaseTemplate returned null for existing template.");
    Assert(fetched!.CanvasWidth == 1200 && fetched.CanvasHeight == 2600, "Read: canvas dimensions mismatch.");
    Console.WriteLine("  ✓ Read template");

    // ── 4. Edit template ──────────────────────────────────────────────────────
    var updated = await svc.UpdateCaseTemplateAsync(tmpl.Id, new()
    {
        PhoneModelId = model.Id, CaseTypeId = ctype.Id,
        Name = "P23 Template Edited", Slug = tmpl.Slug,
        CanvasWidth = 1200, CanvasHeight = 2600, CornerRadius = 50,
        SafeAreaX = 50, SafeAreaY = 80, SafeAreaWidth = 1100, SafeAreaHeight = 2440,
        PrintAreaX = 30, PrintAreaY = 60, PrintAreaWidth = 1140, PrintAreaHeight = 2480,
        BleedTop = 20, BleedRight = 15, BleedBottom = 20, BleedLeft = 15,
        IsActive = true
    });
    Assert(updated.Name == "P23 Template Edited", "Edit: name not updated.");
    Assert(updated.CornerRadius == 50, "Edit: CornerRadius not updated.");
    Console.WriteLine("  ✓ Edit template");

    // ── 5. Duplicate slug rejection ────────────────────────────────────────────
    await Expect<InvalidOperationException>(
        () => svc.CreateCaseTemplateAsync(new()
        {
            PhoneModelId = model.Id, CaseTypeId = ctype.Id,
            Name = "Duplicate", Slug = tmpl.Slug,
            CanvasWidth = 100, CanvasHeight = 200,
            SafeAreaX = 5, SafeAreaY = 5, SafeAreaWidth = 90, SafeAreaHeight = 190,
            PrintAreaX = 5, PrintAreaY = 5, PrintAreaWidth = 90, PrintAreaHeight = 190
        }),
        "Duplicate slug in same model+type must be rejected.");
    Console.WriteLine("  ✓ Duplicate slug rejected");

    // ── 6. Geometry validation (DTO level) ─────────────────────────────────────
    // Zero canvas
    var badDto1 = ValidTemplate(model.Id, ctype.Id, suffix + "x");
    badDto1.CanvasWidth = 0;
    AssertInvalid(badDto1, "CanvasWidth", "Zero canvas width must be rejected.");

    // Safe area outside canvas
    var badDto2 = ValidTemplate(model.Id, ctype.Id, suffix + "y");
    badDto2.SafeAreaX = 1100;
    badDto2.SafeAreaWidth = 200;
    AssertHasError(badDto2, "SafeArea must remain inside canvas.");

    // Print area outside canvas
    var badDto3 = ValidTemplate(model.Id, ctype.Id, suffix + "z");
    badDto3.PrintAreaY = 2500;
    badDto3.PrintAreaHeight = 200;
    AssertHasError(badDto3, "Print area must remain inside canvas.");

    // Negative bleed
    var badDto4 = ValidTemplate(model.Id, ctype.Id, suffix + "w");
    badDto4.BleedLeft = -1;
    AssertHasError(badDto4, "Negative bleed must be rejected.");

    // Negative safe area coordinate
    var badDto5 = ValidTemplate(model.Id, ctype.Id, suffix + "v");
    badDto5.SafeAreaY = -5;
    AssertHasError(badDto5, "Negative safe area coordinate must be rejected.");

    Console.WriteLine("  ✓ Geometry validation");

    // ── 7. Invalid relation rejection ─────────────────────────────────────────
    await Expect<KeyNotFoundException>(
        () => svc.CreateCaseTemplateAsync(new()
        {
            PhoneModelId = Guid.NewGuid(), CaseTypeId = ctype.Id,
            Name = "Bad model", Slug = "bad-model-slug-p23",
            CanvasWidth = 100, CanvasHeight = 200,
            SafeAreaX = 5, SafeAreaY = 5, SafeAreaWidth = 90, SafeAreaHeight = 190,
            PrintAreaX = 5, PrintAreaY = 5, PrintAreaWidth = 90, PrintAreaHeight = 190
        }),
        "Non-existent PhoneModel must be rejected.");
    Console.WriteLine("  ✓ Invalid relation rejected");

    // ── 8. Deactivate template ────────────────────────────────────────────────
    Assert(await svc.DeactivateCaseTemplateAsync(tmpl.Id), "Deactivate must return true.");
    var deactivated = await svc.GetCaseTemplateAsync(tmpl.Id);
    Assert(deactivated is not null, "Deactivated template must still exist (soft delete).");
    Assert(!deactivated!.IsActive, "Template must be inactive after deactivation.");
    Console.WriteLine("  ✓ Deactivate template (soft delete)");

    // ── 9. Deactivating parent doesn't remove template ─────────────────────────
    await svc.DeactivatePhoneModelAsync(model.Id);
    await svc.DeactivateCaseTypeAsync(ctype.Id);
    var stillExists = await svc.GetCaseTemplateAsync(tmpl.Id);
    Assert(stillExists is not null, "Template must survive model/type deactivation.");
    Console.WriteLine("  ✓ Relationship preservation after deactivation");

    // ── 10. Authorization declarations on page files ──────────────────────────
    var adminPages = new[]
    {
        Path.Combine("CaseShop.Web", "Components", "Pages", "Admin", "CaseTemplates", "Index.razor"),
        Path.Combine("CaseShop.Web", "Components", "Pages", "Admin", "CaseTemplates", "Form.razor"),
    };
    foreach (var page in adminPages)
    {
        var src = await File.ReadAllTextAsync(page);
        Assert(src.Contains("[Authorize(Roles = \"Admin\")]"),
            $"Authorization attribute missing from {page}.");
    }
    Console.WriteLine("  ✓ Authorization declarations present");

    // ── 11. AdminShell contains case-templates nav item ───────────────────────
    var shellSrc = await File.ReadAllTextAsync(
        Path.Combine("CaseShop.Web", "Components", "Shared", "AdminShell.razor"));
    Assert(shellSrc.Contains("case-templates"),
        "AdminShell must include case-templates navigation item.");
    Assert(shellSrc.Contains("/admin/case-templates"),
        "AdminShell must include /admin/case-templates href.");
    Console.WriteLine("  ✓ AdminShell nav includes case-templates");

    // ── 12. Preview component exists ──────────────────────────────────────────
    Assert(File.Exists(Path.Combine("CaseShop.Web", "Components", "Shared", "Admin", "CaseTemplatePreview.razor")),
        "CaseTemplatePreview.razor component must exist.");
    var previewSrc = await File.ReadAllTextAsync(
        Path.Combine("CaseShop.Web", "Components", "Shared", "Admin", "CaseTemplatePreview.razor"));
    Assert(previewSrc.Contains("CaseTemplateCreateUpdateDto"), "Preview must accept CaseTemplateCreateUpdateDto.");
    Assert(previewSrc.Contains("<svg"), "Preview must render an SVG element.");
    Console.WriteLine("  ✓ Geometry preview component exists and uses SVG");

    // ── 13. List page: all templates returned (activeOnly=false) ───────────────
    var list = await svc.GetCaseTemplatesAsync(activeOnly: false);
    Assert(list.Any(t => t.Id == tmpl.Id), "GetCaseTemplatesAsync(activeOnly:false) must include deactivated templates.");
    Console.WriteLine("  ✓ List includes inactive templates");

    Console.WriteLine();
    Console.WriteLine("PASS: P2.3 CaseTemplate Admin Management — all assertions passed.");
}
finally
{
    await tx.RollbackAsync();
}

// ── Helpers ──────────────────────────────────────────────────────────────────

static CaseTemplateCreateUpdateDto ValidTemplate(Guid modelId, Guid typeId, string slug) => new()
{
    PhoneModelId = modelId, CaseTypeId = typeId, Name = "Valid", Slug = slug,
    CanvasWidth = 1200, CanvasHeight = 2600,
    SafeAreaX = 50, SafeAreaY = 80, SafeAreaWidth = 1100, SafeAreaHeight = 2440,
    PrintAreaX = 30, PrintAreaY = 60, PrintAreaWidth = 1140, PrintAreaHeight = 2480
};

static List<ValidationResult> Validate(object value)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(value, new ValidationContext(value), results, true);
    return results;
}

static void AssertInvalid(object dto, string memberName, string message)
{
    var results = Validate(dto);
    Assert(results.Any(r => r.MemberNames.Contains(memberName)),
        $"{message} (expected failure for member '{memberName}')");
}

static void AssertHasError(object dto, string message)
{
    var results = Validate(dto);
    Assert(results.Count > 0, message);
}

static async Task Expect<T>(Func<Task> action, string message) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name} but none was thrown. {message}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
