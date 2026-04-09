using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task UpdatePersonalInfoAsync(Guid userId, PersonalInfoDto dto);
    Task UpdatePaymentInfoAsync(Guid userId, PaymentInfoDto dto);
    Task ChangePasswordAsync(Guid userId, PasswordChangeDto dto);
    Task UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDto dto);
}
