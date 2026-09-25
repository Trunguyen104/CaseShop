using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CaseShop.Web.DTOs;
using Npgsql;

var valid = Template();
Assert(Validate(valid).Count == 0, "Valid geometry should pass validation.");

var invalidCanvas = Template();
invalidCanvas.CanvasWidth = 0;
Assert(Validate(invalidCanvas).Any(x => x.MemberNames.Contains(nameof(invalidCanvas.CanvasWidth))), "Zero canvas width must be rejected.");

var invalidSafeArea = Template();
invalidSafeArea.SafeAreaX = 90;
invalidSafeArea.SafeAreaWidth = 20;
Assert(Validate(invalidSafeArea).Any(x => x.ErrorMessage!.Contains("SafeArea")), "Safe area outside canvas must be rejected.");

var invalidPrintArea = Template();
invalidPrintArea.PrintAreaHeight = -1;
Assert(Validate(invalidPrintArea).Any(x => x.ErrorMessage!.Contains("PrintArea")), "Invalid print rectangle must be rejected.");

var invalidBleed = Template();
invalidBleed.BleedLeft = -0.1m;
Assert(Validate(invalidBleed).Any(x => x.ErrorMessage!.Contains("bleed")), "Negative bleed must be rejected.");

var settings = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine("CaseShop.Web", "appsettings.Development.json")));
var connectionString = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var expectedTables = new[] { "PhoneBrands", "PhoneModels", "CaseTypes", "CaseTemplates" };
foreach (var table in expectedTables)
{
    await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
    command.Parameters.AddWithValue("name", $"public.\"{table}\"");
    Assert((bool)(await command.ExecuteScalarAsync())!, $"Table {table} is missing.");
}

const string catalogSql = """
SELECT
  (SELECT count(*) FROM pg_constraint WHERE conname IN ('FK_PhoneModels_PhoneBrands_PhoneBrandId','FK_CaseTemplates_PhoneModels_PhoneModelId','FK_CaseTemplates_CaseTypes_CaseTypeId')),
  (SELECT count(*) FROM pg_indexes WHERE indexname IN ('IX_PhoneBrands_Slug','IX_PhoneModels_PhoneBrandId_Slug','IX_CaseTypes_Slug','IX_CaseTemplates_PhoneModelId_CaseTypeId_Slug')),
  (SELECT count(*) FROM pg_constraint WHERE conname LIKE 'CK_CaseTemplates_%');
""";
await using (var command = new NpgsqlCommand(catalogSql, connection))
await using (var reader = await command.ExecuteReaderAsync())
{
    await reader.ReadAsync();
    Assert(reader.GetInt64(0) == 3, "Expected three foundation foreign keys.");
    Assert(reader.GetInt64(1) == 4, "Expected four unique indexes.");
    Assert(reader.GetInt64(2) == 4, "Expected four geometry check constraints.");
}

Console.WriteLine("PASS: CaseTemplate DTO validation and PostgreSQL schema verification.");

static List<ValidationResult> Validate(object value)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(value, new ValidationContext(value), results, true);
    return results;
}

static CaseTemplateCreateUpdateDto Template() => new()
{
    PhoneModelId = Guid.NewGuid(), CaseTypeId = Guid.NewGuid(), Name = "Validation template", Slug = "validation-template",
    CanvasWidth = 100, CanvasHeight = 200, CornerRadius = 10,
    SafeAreaX = 10, SafeAreaY = 10, SafeAreaWidth = 80, SafeAreaHeight = 180,
    PrintAreaX = 5, PrintAreaY = 5, PrintAreaWidth = 90, PrintAreaHeight = 190,
    BleedTop = 2, BleedRight = 2, BleedBottom = 2, BleedLeft = 2
};

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
