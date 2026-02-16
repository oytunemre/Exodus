using Exodus.Services.Common;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Models;

public class ApiExceptionTests
{
    #region NotFoundException

    [Fact]
    public void NotFoundException_ShouldHave404StatusCode()
    {
        var ex = new NotFoundException("Not found");
        ex.StatusCode.Should().Be(404);
    }

    [Fact]
    public void NotFoundException_ShouldContainMessage()
    {
        var message = "User not found";
        var ex = new NotFoundException(message);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void NotFoundException_ShouldBeApiException()
    {
        var ex = new NotFoundException("test");
        ex.Should().BeAssignableTo<ApiException>();
    }

    [Fact]
    public void NotFoundException_ShouldBeException()
    {
        var ex = new NotFoundException("test");
        ex.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region ConflictException

    [Fact]
    public void ConflictException_ShouldHave409StatusCode()
    {
        var ex = new ConflictException("Conflict");
        ex.StatusCode.Should().Be(409);
    }

    [Fact]
    public void ConflictException_ShouldContainMessage()
    {
        var message = "Email already exists";
        var ex = new ConflictException(message);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ConflictException_ShouldBeApiException()
    {
        var ex = new ConflictException("test");
        ex.Should().BeAssignableTo<ApiException>();
    }

    #endregion

    #region BadRequestException

    [Fact]
    public void BadRequestException_ShouldHave400StatusCode()
    {
        var ex = new BadRequestException("Bad request");
        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void BadRequestException_ShouldContainMessage()
    {
        var message = "Invalid input";
        var ex = new BadRequestException(message);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void BadRequestException_ShouldBeApiException()
    {
        var ex = new BadRequestException("test");
        ex.Should().BeAssignableTo<ApiException>();
    }

    #endregion

    #region UnauthorizedException

    [Fact]
    public void UnauthorizedException_ShouldHave401StatusCode()
    {
        var ex = new UnauthorizedException("Unauthorized");
        ex.StatusCode.Should().Be(401);
    }

    [Fact]
    public void UnauthorizedException_ShouldContainMessage()
    {
        var message = "Access denied";
        var ex = new UnauthorizedException(message);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void UnauthorizedException_ShouldBeApiException()
    {
        var ex = new UnauthorizedException("test");
        ex.Should().BeAssignableTo<ApiException>();
    }

    #endregion

    #region ApiException Base

    [Fact]
    public void ApiException_ShouldBeAbstract()
    {
        typeof(ApiException).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void AllExceptions_ShouldHaveDifferentStatusCodes()
    {
        var notFound = new NotFoundException("a");
        var conflict = new ConflictException("b");
        var badRequest = new BadRequestException("c");
        var unauthorized = new UnauthorizedException("d");

        var codes = new[] { notFound.StatusCode, conflict.StatusCode, badRequest.StatusCode, unauthorized.StatusCode };
        codes.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
