using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class UserManager : IUserService
{
    private readonly IRepository<User> _userRepo;
    private readonly IAuthService _authService;

    public UserManager(IRepository<User> userRepo, IAuthService authService)
    {
        _userRepo = userRepo;
        _authService = authService;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("Kullanıcı", userId);

        return new UserProfileDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FullName = user.FullName,
                Email = user.Email,
                BirthDate = user.BirthDate,
                // Decrypt Phone logic should be here, placeholder for now
                Phone = user.PhoneEncrypted ?? ""
            },
            PaymentInfo = new PaymentInfoDto
            {
                // Decrypt IBAN logic should be here
                IBAN = user.IBANEncrypted ?? "", 
                AccountHolderName = user.FullName
            },
            NotificationSettings = new NotificationSettingsDto
            {
                EmailNotifications = user.EmailNotifications,
                MarketingEmails = user.MarketingEmails,
                SmsNotifications = user.SmsNotifications
            }
        };
    }

    public async Task UpdatePersonalInfoAsync(Guid userId, PersonalInfoDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("Kullanıcı", userId);
        
        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.BirthDate = dto.BirthDate;
        user.PhoneEncrypted = dto.Phone; // Should encrypt in real prod

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task UpdatePaymentInfoAsync(Guid userId, PaymentInfoDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("Kullanıcı", userId);

        user.IBANEncrypted = dto.IBAN; // Should encrypt in real prod
        
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, PasswordChangeDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new BusinessException("Yeni şifreler eşleşmiyor");
        }

        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("Kullanıcı", userId);

        // Verification of current password could rely on AuthService logic matching BCrypt
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessException("Mevcut şifre hatalı");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("Kullanıcı", userId);

        user.EmailNotifications = dto.EmailNotifications;
        user.MarketingEmails = dto.MarketingEmails;
        user.SmsNotifications = dto.SmsNotifications;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }
}
