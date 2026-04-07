using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(UserRegisterDto dto);
    Task<AuthResultDto> LoginAsync(UserLoginDto dto);
    Task<UserDto?> GetCurrentUserAsync(Guid userId);
    Task<AuthResultDto> RefreshTokenAsync(string refreshToken);
}
