using FluentAssertions;
using Xunit;

namespace Paysky.Identity.Keycloak.UnitTests;

public class KeycloakErrorParserTests
{
    [Fact]
    public void Extract_ReadsErrorMessage_FromAdminErrorBody()
        => KeycloakErrorParser.Extract("{\"errorMessage\":\"User exists with same username\"}", "fallback")
            .Should().Be("User exists with same username");

    [Fact]
    public void Extract_ReadsErrorDescription_FromOidcErrorBody()
        => KeycloakErrorParser.Extract("{\"error\":\"invalid_grant\",\"error_description\":\"Invalid user credentials\"}", "fallback")
            .Should().Be("Invalid user credentials");

    [Fact]
    public void Extract_ReturnsRawText_WhenNotJson()
        => KeycloakErrorParser.Extract("Service Unavailable", "fallback")
            .Should().Be("Service Unavailable");

    [Fact]
    public void Extract_ReturnsFallback_WhenNullOrEmpty()
    {
        KeycloakErrorParser.Extract(null, "fallback").Should().Be("fallback");
        KeycloakErrorParser.Extract("", "fallback").Should().Be("fallback");
    }
}
