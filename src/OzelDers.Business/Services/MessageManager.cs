using FluentValidation;
using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class MessageManager : IMessageService
{
    private readonly IRepository<Message> _messageRepo;
    private readonly ITokenService _tokenService;
    private readonly IValidator<MessageSendDto> _sendValidator;

    public MessageManager(
        IRepository<Message> messageRepo,
        ITokenService tokenService,
        IValidator<MessageSendDto> sendValidator)
    {
        _messageRepo = messageRepo;
        _tokenService = tokenService;
        _sendValidator = sendValidator;
    }

    public async Task<List<MessageDto>> GetInboxAsync(Guid userId)
    {
        var messages = await _messageRepo.FindAsync(m => m.ReceiverId == userId);
        return messages
            .OrderByDescending(m => m.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<MessageDto>> GetConversationAsync(Guid userId, Guid otherUserId)
    {
        var messages = await _messageRepo.FindAsync(m =>
            (m.SenderId == userId && m.ReceiverId == otherUserId) ||
            (m.SenderId == otherUserId && m.ReceiverId == userId));
        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// Mesaj gönderme — İki senaryo desteklenir:
    /// Senaryo A (Normal): İlan üzerinden ücretsiz mesaj. İlan sahibi jeton harcayarak açar.
    /// Senaryo B (Direkt Teklif): Gönderen 1 jeton harcar, alıcı mesajı zaten açık olarak görür.
    /// </summary>
    public async Task<MessageDto> SendMessageAsync(MessageSendDto dto, Guid senderId)
    {
        var validationResult = await _sendValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new BusinessException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            ListingId = dto.ListingId,
            Content = dto.Content,
            IsInitiatedWithToken = dto.IsDirectOffer,
        };

        // Senaryo B: Direkt teklif ise gönderen jeton harcar, mesaj zaten açık gelir
        if (dto.IsDirectOffer)
        {
            await _tokenService.SpendTokenAsync(senderId, 1, "Direkt teklif mesajı gönderildi");
            message.Status = MessageStatus.Unlocked;
        }
        else
        {
            // Senaryo A: Normal mesaj, ilan sahibi jeton harcayarak açacak
            message.Status = MessageStatus.Locked;
        }

        await _messageRepo.AddAsync(message);
        await _messageRepo.SaveChangesAsync();

        return MapToDto(message);
    }

    /// <summary>
    /// İlan sahibi kilitli mesajı 1 jeton harcayarak açar (Senaryo A).
    /// </summary>
    public async Task UnlockMessageAsync(Guid messageId, Guid userId)
    {
        var message = (await _messageRepo.FindAsync(m => m.Id == messageId)).FirstOrDefault()
            ?? throw new NotFoundException("Mesaj", messageId);

        if (message.ReceiverId != userId)
            throw new UnauthorizedException("Bu mesajı açma yetkiniz yok.");

        if (message.Status == MessageStatus.Unlocked)
            throw new BusinessException("Bu mesaj zaten açılmış.");

        // 1 jeton harça
        await _tokenService.SpendTokenAsync(userId, 1, "Mesaj kilidi açıldı");

        message.Status = MessageStatus.Unlocked;
        message.ReadAt = DateTime.UtcNow;
        _messageRepo.Update(message);
        await _messageRepo.SaveChangesAsync();
    }

    private static MessageDto MapToDto(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderName = m.Sender?.FullName ?? "",
        SenderImageUrl = m.Sender?.ProfileImageUrl,
        ReceiverId = m.ReceiverId,
        ListingId = m.ListingId,
        ListingTitle = m.Listing?.Title,
        Content = m.Content,
        IsInitiatedWithToken = m.IsInitiatedWithToken,
        IsUnlocked = m.Status == MessageStatus.Unlocked,
        Status = m.Status,
        CreatedAt = m.CreatedAt,
        ReadAt = m.ReadAt
    };
}
