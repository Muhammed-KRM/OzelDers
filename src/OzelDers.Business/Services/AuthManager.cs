using FluentValidation;
using Microsoft.Extensions.Configuration;
using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Helpers;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class AuthManager : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IValidator<UserRegisterDto> _registerValidator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthManager(
        IUserRepository userRepo,
        IValidator<UserRegisterDto> registerValidator,
        IEmailService emailService,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _registerValidator = registerValidator;
        _emailService = emailService;
        _config = config;
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

        // Hoşgeldin ve doğrulama e-postası (Arka planda çalışması için ateşle-unut yapılabilir)
        _ = _emailService.SendTemplatedEmailAsync(
            user.Email,
            "Hoş Geldiniz!",
            new Dictionary<string, string> { { "FullName", user.FullName } }
        );

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
    public async Task<UserDto> UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);

        user.FullName = dto.FullName;
        user.Bio = dto.Bio;
        
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var aesKey = _config["Encryption:AesKey"] ?? _config["Jwt:Key"] ?? "default_very_secret_aes_key_here";
            user.PhoneEncrypted = AesEncryptionHelper.Encrypt(dto.Phone, aesKey);
        }

        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        var user = (await _userRepo.FindAsync(u => 
            u.RefreshToken == refreshToken && 
            u.RefreshTokenExpiryTime > DateTime.UtcNow)).FirstOrDefault();

        if (user is null || !user.IsActive)
        {
            return new AuthResultDto { Success = false, ErrorMessage = "Geçersiz veya süresi dolmuş refresh token." };
        }

        return new AuthResultDto
        {
            Success = true,
            User = MapToDto(user),
            Token = "", // API katmanında dolacak
            RefreshToken = "" // API katmanında dolacak
        };
    }

    public async Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiryTime)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = expiryTime;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task SetUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);

        user.IsActive = isActive;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
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
        Role = u.Role.ToString(),
        CreatedAt = u.CreatedAt
    };
}
