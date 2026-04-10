using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using OzelDers.Business.DTOs;
using OzelDers.IntegrationTests.Setup;
using Xunit;

namespace OzelDers.IntegrationTests.API.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly System.Net.Http.HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_And_Login_Flow_Works()
    {
        // 1. KAYIT İLK DEFA
        var registerDto = new UserRegisterDto
        {
            Email = "integration@test.com",
            Password = "Password123!",
            FullName = "Integration User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        registerResponse.EnsureSuccessStatusCode(); // 200 OK Bekleniyor
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        registerResult.Should().NotBeNull();
        registerResult!.Success.Should().BeTrue();
        registerResult.Token.Should().NotBeNullOrEmpty();
        registerResult.RefreshToken.Should().NotBeNullOrEmpty();
        registerResult.User.Should().NotBeNull();
        registerResult.User!.TokenBalance.Should().Be(3); // Hoş geldin jetonu

        // 2. GİRİŞ (Aynı bilgilerle)
        var loginDto = new UserLoginDto
        {
            Email = "integration@test.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new UserRegisterDto
        {
            Email = "duplicate@test.com",
            Password = "Password123!",
            FullName = "First User"
        };

        // Act 1: Başarılı kayıt
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Act 2: Aynı maille tekrar kayıt
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        duplicateResponse.IsSuccessStatusCode.Should().BeFalse(); // Genellikle 400 Döner
        var errorString = await duplicateResponse.Content.ReadAsStringAsync();
        errorString.Should().Contain("Bu e-posta adresi zaten kayıtlı");
    }
}
