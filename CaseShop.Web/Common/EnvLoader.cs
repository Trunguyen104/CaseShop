using System;
using System.IO;
using System.Linq;

namespace CaseShop.Web.Common;

/// <summary>
/// Lightweight, zero-dependency environment loader that reads key-value pairs from .env
/// and injects them into Environment and ASP.NET Core configuration keys.
/// </summary>
public static class EnvLoader
{
    public static void Load(string? customPath = null)
    {
        string? envFilePath = null;

        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            envFilePath = customPath;
        }
        else
        {
            var baseDir = Directory.GetCurrentDirectory();
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            var candidates = new[]
            {
                Path.Combine(baseDir, ".env"),
                Path.Combine(baseDir, "CaseShop.Web", ".env"),
                Path.Combine(baseDir, "..", ".env"),
                Path.Combine(appDir, ".env"),
                Path.Combine(appDir, "..", "..", "..", ".env"),
                Path.Combine(appDir, "..", "..", "..", "..", ".env")
            };

            envFilePath = candidates.FirstOrDefault(File.Exists);
        }

        if (envFilePath == null)
        {
            return;
        }

        try
        {
            var lines = File.ReadAllLines(envFilePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex <= 0)
                {
                    continue;
                }

                var key = trimmed.Substring(0, equalIndex).Trim();
                var val = trimmed.Substring(equalIndex + 1).Trim();

                // Strip matching quotes if wrapped
                if (val.Length >= 2 &&
                    ((val.StartsWith('"') && val.EndsWith('"')) ||
                     (val.StartsWith('\'') && val.EndsWith('\''))))
                {
                    val = val.Substring(1, val.Length - 2);
                }

                // Set raw environment variable
                Environment.SetEnvironmentVariable(key, val);

                // Map standard friendly aliases to ASP.NET Core hierarchical keys
                MapHierarchicalAlias(key, val);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnvLoader] Warning: Failed to load .env file from '{envFilePath}': {ex.Message}");
        }
    }

    private static void MapHierarchicalAlias(string key, string val)
    {
        switch (key.ToUpperInvariant())
        {
            case "CLOUDINARY_CLOUD_NAME":
            case "CLOUDINARY_CLOUDNAME":
                Environment.SetEnvironmentVariable("Cloudinary__CloudName", val);
                break;

            case "CLOUDINARY_API_KEY":
            case "CLOUDINARY_APIKEY":
                Environment.SetEnvironmentVariable("Cloudinary__ApiKey", val);
                break;

            case "CLOUDINARY_API_SECRET":
            case "CLOUDINARY_APISECRET":
                Environment.SetEnvironmentVariable("Cloudinary__ApiSecret", val);
                break;

            case "DEFAULT_CONNECTION":
            case "CONNECTION_STRING":
            case "DATABASE_URL":
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", val);
                break;

            case "ADMIN_USERNAME":
                Environment.SetEnvironmentVariable("AdminCredentials__Username", val);
                break;

            case "ADMIN_PASSWORD":
                Environment.SetEnvironmentVariable("AdminCredentials__Password", val);
                break;
        }
    }
}
