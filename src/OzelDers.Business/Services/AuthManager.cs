using FluentValidation;
using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Helpers;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class AuthManager : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IValidator<UserRegisterDto> _registerValidator;

    public AuthManager(
        IUserRepository userRepo,
        IValidator<UserRegisterDto> registerValidator)
    {
        _userRepo = userRepo;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResultDto> RegisterAsync(UserRegisterDto dto)
    {
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
            };

        if (!await _userRepo.IsEmailUniqueAsync(dto.Email))
            return new AuthResultDto { Success = false, ErrorMessage = "Bu e-posta adresi zaten kayıtlı." };

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            FullName = dto.FullName,
            TokenBalance = 3 // Hoş geldin hediyesi: 3 jeton
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        // JWT token üretimi API katmanında JwtHelper ile yapılacak
        return new AuthResultDto
        {
            Success = true,
            User = MapToDto(user),
            Token = "", // API katmanında doldurulacak
            RefreshToken = "" // API katmanında doldurulacak
        };
    }

    public async Task<AuthResultDto> LoginAsync(UserLoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email);

        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            return new AuthResultDto { Success = false, ErrorMessage = "E-posta veya şifre hatalı." };

        if (!user.IsActive)
            return new AuthResultDto { Success = false, ErrorMessage = "Hesabınız askıya alınmıştır." };

        return new AuthResultDto
        {
            Success = true,
            User = MapToDto(user),
            Token = "", // API katmanında doldurulacak
            RefreshToken = "" // API katmanında doldurulacak
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        // Refresh token yenileme mantığı API katmanında JwtHelper ile implement edilecek
        throw new NotImplementedException("RefreshToken API katmanında implement edilecek.");
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        IsTeacherProfileComplete = u.IsTeacherProfileComplete,
        ProfileImageUrl = u.ProfileImageUrl,
        Bio = u.Bio,
        TokenBalance = u.TokenBalance,
        CreatedAt = u.CreatedAt
    };
}
