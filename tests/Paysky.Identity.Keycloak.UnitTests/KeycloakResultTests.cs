using System.Net;
using FluentAssertions;
using Xunit;

namespace Paysky.Identity.Keycloak.UnitTests;

public class KeycloakResultTests
{
    [Fact]
    public void Ok_NonGeneric_IsSuccessWithNoError()
    {
        var result = KeycloakResult.Ok(HttpStatusCode.Created);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public void Fail_NonGeneric_CarriesMessageAndStatus()
    {
        var result = KeycloakResult.Fail("boom", HttpStatusCode.NotFound);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void Ok_Generic_CarriesData()
    {
        var result = KeycloakResult<int>.Ok(42);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(42);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Fail_Generic_HasDefaultDataAndMessage()
    {
        var result = KeycloakResult<string>.Fail("nope", HttpStatusCode.Conflict);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorMessage.Should().Be("nope");
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
