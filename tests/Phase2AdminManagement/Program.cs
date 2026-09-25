using System.Text.Json;
using CaseShop.Web.Data;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services;
using Microsoft.EntityFrameworkCore;

var settings = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine("CaseShop.Web", "appsettings.Development.json")));
var connectionString = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
await using var db = new AppDbContext(options);
await using var transaction = await db.Database.BeginTransactionAsync();
var repository = new CaseTemplateRepository(db);
var service = new CaseTemplateService(repository);

try
{
    var suffix = Guid.NewGuid().ToString("N")[..8];
    var brandA = await service.CreatePhoneBrandAsync(new() { Name = "P2 test brand A", Slug = $"P2-{suffix}-A" });
    var brandB = await service.CreatePhoneBrandAsync(new() { Name = "P2 test brand B", Slug = $"p2-{suffix}-b" });
    Assert(brandA.Slug == $"p2-{suffix}-a", "Brand slug was not normalized.");
    await Expect<InvalidOperationException>(() => service.CreatePhoneBrandAsync(new() { Name = "Duplicate", Slug = brandA.Slug }), "Duplicate brand slug was accepted.");
    brandA = await service.UpdatePhoneBrandAsync(brandA.Id, new() { Name = "P2 edited brand", Slug = brandA.Slug, IsActive = true });
    Assert(brandA.Name == "P2 edited brand", "Brand edit failed.");

    var modelA = await service.CreatePhoneModelAsync(new() { PhoneBrandId = brandA.Id, Name = "P2 model A", Slug = "same-model" });
    await Expect<KeyNotFoundException>(() => service.CreatePhoneModelAsync(new() { PhoneBrandId = Guid.NewGuid(), Name = "Invalid", Slug = "invalid" }), "Invalid brand reference was accepted.");
    await Expect<InvalidOperationException>(() => service.CreatePhoneModelAsync(new() { PhoneBrandId = brandA.Id, Name = "Duplicate", Slug = "same-model" }), "Duplicate model slug in one brand was accepted.");
    var modelB = await service.CreatePhoneModelAsync(new() { PhoneBrandId = brandB.Id, Name = "P2 model B", Slug = "same-model" });
    Assert(modelA.Id != modelB.Id, "Same model slug under different brands should be allowed.");
    modelA = await service.UpdatePhoneModelAsync(modelA.Id, new() { PhoneBrandId = brandA.Id, Name = "P2 edited model", Slug = modelA.Slug, IsActive = true });
    Assert(modelA.Name == "P2 edited model", "Model edit failed.");

    var type = await service.CreateCaseTypeAsync(new() { Name = "P2 test type", Slug = $"p2-{suffix}" });
    await Expect<InvalidOperationException>(() => service.CreateCaseTypeAsync(new() { Name = "Duplicate", Slug = type.Slug }), "Duplicate case type slug was accepted.");
    type = await service.UpdateCaseTypeAsync(type.Id, new() { Name = "P2 edited type", Slug = type.Slug, IsActive = true });
    Assert(type.Name == "P2 edited type", "Case type edit failed.");

    var template = await service.CreateCaseTemplateAsync(new()
    {
        PhoneModelId = modelA.Id, CaseTypeId = type.Id, Name = "Preservation check", Slug = "preservation-check",
        CanvasWidth = 100, CanvasHeight = 200, CornerRadius = 10,
        SafeAreaX = 10, SafeAreaY = 10, SafeAreaWidth = 80, SafeAreaHeight = 180,
        PrintAreaX = 5, PrintAreaY = 5, PrintAreaWidth = 90, PrintAreaHeight = 190
    });

    Assert(await service.DeactivatePhoneModelAsync(modelA.Id), "Model deactivation failed.");
    Assert(await service.DeactivateCaseTypeAsync(type.Id), "Case type deactivation failed.");
    Assert(await service.DeactivatePhoneBrandAsync(brandA.Id), "Brand deactivation failed.");
    Assert((await service.GetPhoneBrandAsync(brandA.Id))?.IsActive == false, "Deactivated brand was not retained.");
    Assert((await service.GetPhoneModelAsync(modelA.Id))?.IsActive == false, "Deactivated model was not retained.");
    Assert((await service.GetCaseTypeAsync(type.Id))?.IsActive == false, "Deactivated type was not retained.");
    Assert(await service.GetCaseTemplateAsync(template.Id) is not null, "Deactivation removed a related template.");
    await Expect<InvalidOperationException>(() => service.CreatePhoneModelAsync(new() { PhoneBrandId = brandA.Id, Name = "Blocked", Slug = "blocked" }), "Inactive brand was accepted for a new model.");

    var protectedPages = new[]
    {
        "PhoneBrands/Index.razor", "PhoneBrands/Form.razor", "PhoneModels/Index.razor",
        "PhoneModels/Form.razor", "CaseTypes/Index.razor", "CaseTypes/Form.razor"
    };
    foreach (var page in protectedPages)
    {
        var source = await File.ReadAllTextAsync(Path.Combine("CaseShop.Web", "Components", "Pages", "Admin", page));
        Assert(source.Contains("[Authorize(Roles = \"Admin\")]"), $"Admin authorization is missing from {page}.");
    }

    Console.WriteLine("PASS: P2.2 CRUD, uniqueness, reference validation, deactivation, relationship preservation and authorization declarations.");
}
finally
{
    await transaction.RollbackAsync();
}

static async Task Expect<T>(Func<Task> action, string message) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new InvalidOperationException(message);
}
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
