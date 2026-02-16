using Exodus.Services.Security;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Services;

public class InputSanitizerServiceTests
{
    private readonly InputSanitizerService _sanitizer = new();

    #region SanitizeHtml

    [Fact]
    public void SanitizeHtml_WhenNull_ShouldReturnNull()
    {
        _sanitizer.SanitizeHtml(null!).Should().BeNull();
    }

    [Fact]
    public void SanitizeHtml_WhenEmpty_ShouldReturnEmpty()
    {
        _sanitizer.SanitizeHtml("").Should().BeEmpty();
    }

    [Fact]
    public void SanitizeHtml_WhenPlainText_ShouldReturnSame()
    {
        var input = "Hello World";
        _sanitizer.SanitizeHtml(input).Should().Be("Hello World");
    }

    [Fact]
    public void SanitizeHtml_WhenContainsScriptTag_ShouldEncode()
    {
        var input = "<script>alert('xss')</script>";
        var result = _sanitizer.SanitizeHtml(input);
        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void SanitizeHtml_WhenContainsHtmlTags_ShouldEncode()
    {
        var input = "<b>Bold</b> <i>Italic</i>";
        var result = _sanitizer.SanitizeHtml(input);
        result.Should().NotContain("<b>");
        result.Should().Contain("&lt;b&gt;");
    }

    [Fact]
    public void SanitizeHtml_WhenContainsAmpersand_ShouldEncode()
    {
        var input = "Tom & Jerry";
        var result = _sanitizer.SanitizeHtml(input);
        result.Should().Contain("&amp;");
    }

    [Fact]
    public void SanitizeHtml_WhenContainsQuotes_ShouldEncode()
    {
        var input = "He said \"hello\"";
        var result = _sanitizer.SanitizeHtml(input);
        result.Should().Contain("&quot;");
    }

    #endregion

    #region SanitizeForSql

    [Fact]
    public void SanitizeForSql_WhenNull_ShouldReturnNull()
    {
        _sanitizer.SanitizeForSql(null!).Should().BeNull();
    }

    [Fact]
    public void SanitizeForSql_WhenEmpty_ShouldReturnEmpty()
    {
        _sanitizer.SanitizeForSql("").Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForSql_WhenContainsSingleQuotes_ShouldEscape()
    {
        var input = "O'Reilly";
        var result = _sanitizer.SanitizeForSql(input);
        result.Should().Be("O''Reilly");
    }

    [Fact]
    public void SanitizeForSql_WhenContainsNullBytes_ShouldRemove()
    {
        var input = "test\0injection";
        var result = _sanitizer.SanitizeForSql(input);
        result.Should().Be("testinjection");
    }

    [Fact]
    public void SanitizeForSql_WhenNormalText_ShouldReturnSame()
    {
        var input = "Normal text without special chars";
        var result = _sanitizer.SanitizeForSql(input);
        result.Should().Be(input);
    }

    #endregion

    #region StripAllTags

    [Fact]
    public void StripAllTags_WhenNull_ShouldReturnNull()
    {
        _sanitizer.StripAllTags(null!).Should().BeNull();
    }

    [Fact]
    public void StripAllTags_WhenEmpty_ShouldReturnEmpty()
    {
        _sanitizer.StripAllTags("").Should().BeEmpty();
    }

    [Fact]
    public void StripAllTags_WhenContainsHtml_ShouldRemoveTags()
    {
        var input = "<p>Hello <b>World</b></p>";
        var result = _sanitizer.StripAllTags(input);
        result.Should().NotContain("<p>");
        result.Should().NotContain("<b>");
    }

    [Fact]
    public void StripAllTags_WhenPlainText_ShouldPreserveContent()
    {
        var input = "Plain text without tags";
        var result = _sanitizer.StripAllTags(input);
        result.Should().Contain("Plain text without tags");
    }

    #endregion

    #region ContainsMaliciousContent

    [Fact]
    public void ContainsMaliciousContent_WhenNull_ShouldReturnFalse()
    {
        _sanitizer.ContainsMaliciousContent(null!).Should().BeFalse();
    }

    [Fact]
    public void ContainsMaliciousContent_WhenEmpty_ShouldReturnFalse()
    {
        _sanitizer.ContainsMaliciousContent("").Should().BeFalse();
    }

    [Fact]
    public void ContainsMaliciousContent_WhenNormalText_ShouldReturnFalse()
    {
        _sanitizer.ContainsMaliciousContent("Hello World").Should().BeFalse();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("onclick=alert(1)")]
    [InlineData("onload=malicious()")]
    [InlineData("onerror=hack()")]
    [InlineData("<iframe src='evil'>")]
    [InlineData("document.cookie")]
    [InlineData("document.write('x')")]
    [InlineData("window.location")]
    [InlineData("eval(code)")]
    [InlineData("<object data='evil'>")]
    [InlineData("<embed src='evil'>")]
    [InlineData("<form action='evil'>")]
    public void ContainsMaliciousContent_WhenXssPattern_ShouldReturnTrue(string input)
    {
        _sanitizer.ContainsMaliciousContent(input).Should().BeTrue();
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("1; DROP TABLE users")]
    [InlineData("UNION SELECT * FROM passwords")]
    [InlineData("'; DELETE FROM users--")]
    [InlineData("INSERT INTO users VALUES")]
    [InlineData("UPDATE users SET admin=1")]
    public void ContainsMaliciousContent_WhenSqlInjection_ShouldReturnTrue(string input)
    {
        _sanitizer.ContainsMaliciousContent(input).Should().BeTrue();
    }

    [Theory]
    [InlineData("SELECT a nice product")]
    [InlineData("My email is user@example.com")]
    [InlineData("Price is $100")]
    public void ContainsMaliciousContent_WhenSafeText_ShouldReturnFalse(string input)
    {
        _sanitizer.ContainsMaliciousContent(input).Should().BeFalse();
    }

    #endregion

    #region SanitizeFileName

    [Fact]
    public void SanitizeFileName_WhenNull_ShouldReturnUnnamed()
    {
        _sanitizer.SanitizeFileName(null!).Should().Be("unnamed");
    }

    [Fact]
    public void SanitizeFileName_WhenEmpty_ShouldReturnUnnamed()
    {
        _sanitizer.SanitizeFileName("").Should().Be("unnamed");
    }

    [Fact]
    public void SanitizeFileName_WhenContainsPathTraversal_ShouldRemove()
    {
        var result = _sanitizer.SanitizeFileName("../../etc/passwd");
        result.Should().NotContain("..");
        result.Should().NotContain("/");
    }

    [Fact]
    public void SanitizeFileName_WhenContainsBackslash_ShouldRemove()
    {
        var result = _sanitizer.SanitizeFileName("..\\windows\\system32");
        result.Should().NotContain("\\");
    }

    [Fact]
    public void SanitizeFileName_WhenTooLong_ShouldTruncate()
    {
        var longName = new string('a', 300) + ".jpg";
        var result = _sanitizer.SanitizeFileName(longName);
        result.Length.Should().BeLessThanOrEqualTo(255);
    }

    [Fact]
    public void SanitizeFileName_WhenValidName_ShouldReturnSame()
    {
        var result = _sanitizer.SanitizeFileName("photo.jpg");
        result.Should().Be("photo.jpg");
    }

    [Fact]
    public void SanitizeFileName_WhenContainsSpaces_ShouldPreserve()
    {
        var result = _sanitizer.SanitizeFileName("my photo.jpg");
        result.Should().Be("my photo.jpg");
    }

    #endregion
}
