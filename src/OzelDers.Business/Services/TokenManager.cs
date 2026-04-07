using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class TokenManager : ITokenService
{
    private readonly IUserRepository _userRepo;
    private readonly IRepository<TokenTransaction> _transactionRepo;
    private readonly IRepository<TokenPackage> _packageRepo;

    public TokenManager(
        IUserRepository userRepo,
        IRepository<TokenTransaction> transactionRepo,
        IRepository<TokenPackage> packageRepo)
    {
        _userRepo = userRepo;
        _transactionRepo = transactionRepo;
        _packageRepo = packageRepo;
    }

    public async Task<int> GetBalanceAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);
        return user.TokenBalance;
    }

    public async Task<List<TokenPackageDto>> GetPackagesAsync()
    {
        var packages = await _packageRepo.GetAllAsync();
        return packages.Select(p => new TokenPackageDto
        {
            Id = p.Id,
            Name = p.Name,
            TokenCount = p.TokenCount,
            Price = p.Price,
            IsPopular = p.IsPopular,
            BadgeText = p.BadgeText
        }).ToList();
    }

    public async Task<List<TokenTransactionDto>> GetTransactionHistoryAsync(Guid userId)
    {
        var transactions = await _transactionRepo.FindAsync(t => t.UserId == userId);
        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TokenTransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList();
    }

    // DİKKAT: Race Condition'ı (çifte harcama) önlemek için Entity Framework'te 
    // Optimistic Concurrency veya Redis üzerinden Distributed Lock uygulanmalıdır.
    public async Task SpendTokenAsync(Guid userId, int amount, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);

        if (user.TokenBalance < amount)
            throw new InsufficientTokenException(user.TokenBalance, amount);

        user.TokenBalance -= amount;
        _userRepo.Update(user);

        await _transactionRepo.AddAsync(new TokenTransaction
        {
            UserId = userId,
            Amount = -amount,
            Type = TransactionType.Spend,
            Description = reason
        });

        await _userRepo.SaveChangesAsync();
    }

    public async Task AddTokenAsync(Guid userId, int amount, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Kullanıcı", userId);

        user.TokenBalance += amount;
        _userRepo.Update(user);

        await _transactionRepo.AddAsync(new TokenTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = TransactionType.Purchase,
            Description = reason
        });

        await _userRepo.SaveChangesAsync();
    }
}
