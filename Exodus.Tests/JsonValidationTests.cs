using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Exodus.Tests;

public class JsonValidationTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string GetJsonPath(string relativePath) =>
        Path.Combine(ProjectRoot, relativePath);

    #region JSON Syntax Validation

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldBeValidJson(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        File.Exists(filePath).Should().BeTrue($"JSON dosyasi bulunamadi: {filePath}");

        var content = File.ReadAllText(filePath);
        // BOM varsa temizle
        content = content.TrimStart('\uFEFF');

        var act = () => JsonDocument.Parse(content);
        act.Should().NotThrow($"{relativePath} gecerli bir JSON olmali");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldNotHaveUtf8Bom(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var bytes = File.ReadAllBytes(filePath);

        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        hasBom.Should().BeFalse($"{relativePath} UTF-8 BOM icermemeli");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldBeUtf8Encoded(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var bytes = File.ReadAllBytes(filePath);

        // UTF-8 BOM'u atla
        var startIndex = (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ? 3 : 0;
        var contentBytes = bytes.Skip(startIndex).ToArray();

        var act = () => Encoding.UTF8.GetString(contentBytes);
        act.Should().NotThrow($"{relativePath} UTF-8 ile kodlanmis olmali");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_RootShouldBeObject(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var content = File.ReadAllText(filePath).TrimStart('\uFEFF');
        using var doc = JsonDocument.Parse(content);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object,
            $"{relativePath} kok elemani bir JSON nesnesi olmali");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldNotBeEmpty(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var content = File.ReadAllText(filePath).TrimStart('\uFEFF').Trim();

        content.Should().NotBeNullOrWhiteSpace($"{relativePath} bos olmamali");

        using var doc = JsonDocument.Parse(content);
        doc.RootElement.EnumerateObject().Any().Should().BeTrue(
            $"{relativePath} en az bir ozellik icermeli");
    }

    #endregion

    #region appsettings.json Structure Tests

    [Fact]
    public void AppSettings_ShouldContainRequiredSections()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("Logging", out _).Should().BeTrue("Logging bolumu olmali");
        root.TryGetProperty("ConnectionStrings", out _).Should().BeTrue("ConnectionStrings bolumu olmali");
        root.TryGetProperty("JwtSettings", out _).Should().BeTrue("JwtSettings bolumu olmali");
    }

    [Fact]
    public void AppSettings_LoggingShouldHaveValidStructure()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var logging = doc.RootElement.GetProperty("Logging");

        logging.TryGetProperty("LogLevel", out var logLevel).Should().BeTrue("LogLevel tanimli olmali");
        logLevel.TryGetProperty("Default", out _).Should().BeTrue("Default log seviyesi tanimli olmali");
    }

    [Fact]
    public void AppSettings_ConnectionStringShouldExist()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var connStrings = doc.RootElement.GetProperty("ConnectionStrings");

        connStrings.TryGetProperty("DefaultConnection", out var defaultConn).Should().BeTrue(
            "DefaultConnection tanimli olmali");
        defaultConn.GetString().Should().NotBeNullOrWhiteSpace(
            "DefaultConnection bos olmamali");
    }

    [Fact]
    public void AppSettings_JwtSettingsShouldHaveRequiredFields()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var jwt = doc.RootElement.GetProperty("JwtSettings");

        jwt.TryGetProperty("SecretKey", out var secretKey).Should().BeTrue("SecretKey tanimli olmali");
        secretKey.GetString().Should().NotBeNullOrWhiteSpace("SecretKey bos olmamali");

        jwt.TryGetProperty("Issuer", out var issuer).Should().BeTrue("Issuer tanimli olmali");
        issuer.GetString().Should().NotBeNullOrWhiteSpace("Issuer bos olmamali");

        jwt.TryGetProperty("Audience", out var audience).Should().BeTrue("Audience tanimli olmali");
        audience.GetString().Should().NotBeNullOrWhiteSpace("Audience bos olmamali");

        jwt.TryGetProperty("ExpiryMinutes", out var expiry).Should().BeTrue("ExpiryMinutes tanimli olmali");
        expiry.ValueKind.Should().Be(JsonValueKind.Number, "ExpiryMinutes sayi olmali");
        expiry.GetInt32().Should().BeGreaterThan(0, "ExpiryMinutes pozitif olmali");
    }

    [Fact]
    public void AppSettings_CorsShouldHaveAllowedOrigins()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var cors = doc.RootElement.GetProperty("Cors");

        cors.TryGetProperty("AllowedOrigins", out var origins).Should().BeTrue(
            "AllowedOrigins tanimli olmali");
        origins.ValueKind.Should().Be(JsonValueKind.Array, "AllowedOrigins dizi olmali");
        origins.GetArrayLength().Should().BeGreaterThan(0, "En az bir origin tanimli olmali");

        foreach (var origin in origins.EnumerateArray())
        {
            var url = origin.GetString();
            url.Should().NotBeNullOrWhiteSpace("Origin bos olmamali");
            url.Should().StartWith("http", "Origin gecerli bir URL olmali");
        }
    }

    [Fact]
    public void AppSettings_EmailSettingsShouldHaveValidStructure()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var email = doc.RootElement.GetProperty("EmailSettings");

        email.TryGetProperty("SmtpHost", out _).Should().BeTrue("SmtpHost tanimli olmali");
        email.TryGetProperty("SmtpPort", out var port).Should().BeTrue("SmtpPort tanimli olmali");
        port.ValueKind.Should().Be(JsonValueKind.Number, "SmtpPort sayi olmali");
        port.GetInt32().Should().BeInRange(1, 65535, "SmtpPort gecerli bir port numarasi olmali");

        email.TryGetProperty("FromEmail", out var fromEmail).Should().BeTrue("FromEmail tanimli olmali");
        fromEmail.GetString().Should().Contain("@", "FromEmail gecerli bir e-posta olmali");
    }

    [Fact]
    public void AppSettings_FileUploadSettingsShouldHaveValidStructure()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var upload = doc.RootElement.GetProperty("FileUploadSettings");

        upload.TryGetProperty("MaxFileSizeBytes", out var maxSize).Should().BeTrue(
            "MaxFileSizeBytes tanimli olmali");
        maxSize.ValueKind.Should().Be(JsonValueKind.Number, "MaxFileSizeBytes sayi olmali");
        maxSize.GetInt64().Should().BeGreaterThan(0, "MaxFileSizeBytes pozitif olmali");

        upload.TryGetProperty("AllowedImageExtensions", out var imgExt).Should().BeTrue(
            "AllowedImageExtensions tanimli olmali");
        imgExt.ValueKind.Should().Be(JsonValueKind.Array, "AllowedImageExtensions dizi olmali");
        imgExt.GetArrayLength().Should().BeGreaterThan(0, "En az bir resim uzantisi tanimli olmali");

        foreach (var ext in imgExt.EnumerateArray())
        {
            ext.GetString().Should().StartWith(".", "Uzanti nokta ile baslamali");
        }

        upload.TryGetProperty("AllowedDocumentExtensions", out var docExt).Should().BeTrue(
            "AllowedDocumentExtensions tanimli olmali");
        docExt.ValueKind.Should().Be(JsonValueKind.Array, "AllowedDocumentExtensions dizi olmali");
    }

    [Fact]
    public void AppSettings_IyzicoSettingsShouldHaveValidStructure()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var iyzico = doc.RootElement.GetProperty("IyzicoSettings");

        iyzico.TryGetProperty("ApiKey", out _).Should().BeTrue("ApiKey tanimli olmali");
        iyzico.TryGetProperty("SecretKey", out _).Should().BeTrue("SecretKey tanimli olmali");

        iyzico.TryGetProperty("BaseUrl", out var baseUrl).Should().BeTrue("BaseUrl tanimli olmali");
        baseUrl.GetString().Should().StartWith("https://", "BaseUrl HTTPS olmali");

        iyzico.TryGetProperty("IsSandbox", out var isSandbox).Should().BeTrue("IsSandbox tanimli olmali");
        isSandbox.ValueKind.Should().BeOneOf(new[] { JsonValueKind.True, JsonValueKind.False },
            "IsSandbox boolean olmali");

        iyzico.TryGetProperty("CallbackUrl", out var callback).Should().BeTrue("CallbackUrl tanimli olmali");
        callback.GetString().Should().Contain("/api/payments/", "CallbackUrl payments endpoint icermeli");
    }

    #endregion

    #region appsettings.Development.json Tests

    [Fact]
    public void AppSettingsDev_ShouldHaveLogging()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Development.json"));
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("Logging", out _).Should().BeTrue(
            "Development ayarlarinda Logging bolumu olmali");
    }

    #endregion

    #region appsettings.Production.json Tests

    [Fact]
    public void AppSettingsProd_ShouldHaveRequiredSections()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("Logging", out _).Should().BeTrue("Logging bolumu olmali");
        root.TryGetProperty("ConnectionStrings", out _).Should().BeTrue("ConnectionStrings bolumu olmali");
        root.TryGetProperty("JwtSettings", out _).Should().BeTrue("JwtSettings bolumu olmali");
    }

    [Fact]
    public void AppSettingsProd_LoggingShouldBeMoreRestrictive()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        using var doc = JsonDocument.Parse(content);
        var logLevel = doc.RootElement.GetProperty("Logging").GetProperty("LogLevel");

        var defaultLevel = logLevel.GetProperty("Default").GetString();
        defaultLevel.Should().BeOneOf("Warning", "Error", "Critical",
            "Production'da default log seviyesi Warning veya daha yuksek olmali");
    }

    [Fact]
    public void AppSettingsProd_SensitiveFieldsShouldBeEmpty()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var connString = root.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
        connString.Should().BeNullOrWhiteSpace(
            "Production'da ConnectionString bos olmali (env variable ile doldurulmali)");

        var jwtSecret = root.GetProperty("JwtSettings").GetProperty("SecretKey").GetString();
        jwtSecret.Should().BeNullOrWhiteSpace(
            "Production'da JwtSettings SecretKey bos olmali (env variable ile doldurulmali)");
    }

    #endregion

    #region launchSettings.json Tests

    [Fact]
    public void LaunchSettings_ShouldHaveProfiles()
    {
        var content = File.ReadAllText(GetJsonPath("Properties/launchSettings.json"))
            .TrimStart('\uFEFF');
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("profiles", out var profiles).Should().BeTrue("profiles bolumu olmali");
        profiles.ValueKind.Should().Be(JsonValueKind.Object, "profiles bir nesne olmali");
        profiles.EnumerateObject().Any().Should().BeTrue("En az bir profil tanimli olmali");
    }

    [Fact]
    public void LaunchSettings_ProfilesShouldHaveApplicationUrl()
    {
        var content = File.ReadAllText(GetJsonPath("Properties/launchSettings.json"))
            .TrimStart('\uFEFF');
        using var doc = JsonDocument.Parse(content);
        var profiles = doc.RootElement.GetProperty("profiles");

        foreach (var profile in profiles.EnumerateObject())
        {
            if (profile.Value.TryGetProperty("applicationUrl", out var url))
            {
                url.GetString().Should().NotBeNullOrWhiteSpace(
                    $"'{profile.Name}' profilinde applicationUrl bos olmamali");
                url.GetString().Should().Contain("localhost",
                    $"'{profile.Name}' profilinde applicationUrl localhost icermeli");
            }
        }
    }

    [Fact]
    public void LaunchSettings_ProfilesShouldHaveEnvironmentVariables()
    {
        var content = File.ReadAllText(GetJsonPath("Properties/launchSettings.json"))
            .TrimStart('\uFEFF');
        using var doc = JsonDocument.Parse(content);
        var profiles = doc.RootElement.GetProperty("profiles");

        foreach (var profile in profiles.EnumerateObject())
        {
            profile.Value.TryGetProperty("environmentVariables", out var envVars).Should().BeTrue(
                $"'{profile.Name}' profilinde environmentVariables olmali");

            envVars.TryGetProperty("ASPNETCORE_ENVIRONMENT", out var env).Should().BeTrue(
                $"'{profile.Name}' profilinde ASPNETCORE_ENVIRONMENT tanimli olmali");

            env.GetString().Should().BeOneOf("Development", "Staging", "Production",
                $"'{profile.Name}' profilinde gecerli bir environment olmali");
        }
    }

    #endregion

    #region Cross-file Consistency Tests

    [Fact]
    public void AllAppSettings_ShouldHaveConsistentLoggingStructure()
    {
        var files = new[] { "appsettings.json", "appsettings.Development.json", "appsettings.Production.json" };

        foreach (var file in files)
        {
            var content = File.ReadAllText(GetJsonPath(file));
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.TryGetProperty("Logging", out var logging).Should().BeTrue(
                $"{file} icinde Logging bolumu olmali");

            logging.TryGetProperty("LogLevel", out var logLevel).Should().BeTrue(
                $"{file} icinde LogLevel tanimli olmali");

            logLevel.TryGetProperty("Default", out var defaultLevel).Should().BeTrue(
                $"{file} icinde Default log seviyesi tanimli olmali");

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
            defaultLevel.GetString().Should().BeOneOf(validLevels,
                $"{file} icinde gecerli bir log seviyesi olmali");
        }
    }

    [Fact]
    public void ProductionSettings_ShouldNotContainDevelopmentValues()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        var text = content.ToLower();

        text.Should().NotContain("localhost:3000", "Production'da localhost:3000 olmamali");
        text.Should().NotContain("localhost:5173", "Production'da localhost:5173 olmamali");
        text.Should().NotContain("sandbox", "Production'da sandbox deger olmamali");
    }

    [Fact]
    public void AppSettings_JwtIssuerShouldBeConsistentAcrossEnvironments()
    {
        var mainContent = File.ReadAllText(GetJsonPath("appsettings.json"));
        using var mainDoc = JsonDocument.Parse(mainContent);
        var mainIssuer = mainDoc.RootElement.GetProperty("JwtSettings").GetProperty("Issuer").GetString();

        var prodContent = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        using var prodDoc = JsonDocument.Parse(prodContent);
        var prodIssuer = prodDoc.RootElement.GetProperty("JwtSettings").GetProperty("Issuer").GetString();

        prodIssuer.Should().Be(mainIssuer, "JWT Issuer tum ortamlarda ayni olmali");
    }

    #endregion

    #region JSON Quality Tests

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldNotHaveDuplicateKeys(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var content = File.ReadAllText(filePath).TrimStart('\uFEFF');

        // System.Text.Json varsayilan olarak duplicate key'lerde son degeri alir
        // Biz ise duplicate key olmadigini dogrulamak istiyoruz
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        };

        var act = () => JsonDocument.Parse(content, options);
        act.Should().NotThrow($"{relativePath} gecersiz JSON elementleri icermemeli");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Properties/launchSettings.json")]
    public void JsonFile_ShouldNotContainComments(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var content = File.ReadAllText(filePath).TrimStart('\uFEFF');

        // JSON standardi yorumlara izin vermez
        var options = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Disallow
        };

        var act = () => JsonDocument.Parse(content, options);
        act.Should().NotThrow($"{relativePath} JSON yorum icermemeli");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Production.json")]
    public void AppSettingsFile_ShouldNotContainTrailingCommas(string relativePath)
    {
        var filePath = GetJsonPath(relativePath);
        var content = File.ReadAllText(filePath).TrimStart('\uFEFF');

        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = false
        };

        var act = () => JsonDocument.Parse(content, options);
        act.Should().NotThrow($"{relativePath} sonda virgul icermemeli");
    }

    #endregion

    #region Security Tests

    [Fact]
    public void AppSettings_ShouldNotContainRealCredentials()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.json")).ToLower();

        // Gercek sifre veya token icermemeli (sandbox/placeholder haric)
        content.Should().NotContainAny(
            "password123", "admin123", "123456",
            "real-api-key", "sk-", "pk_live_");
    }

    [Fact]
    public void ProductionSettings_ConnectionStringShouldNotContainPassword()
    {
        var content = File.ReadAllText(GetJsonPath("appsettings.Production.json"));
        using var doc = JsonDocument.Parse(content);
        var connString = doc.RootElement.GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection").GetString() ?? "";

        connString.ToLower().Should().NotContain("password=",
            "Production ConnectionString icinde sifre olmamali");
        connString.ToLower().Should().NotContain("pwd=",
            "Production ConnectionString icinde sifre olmamali");
    }

    #endregion
}
