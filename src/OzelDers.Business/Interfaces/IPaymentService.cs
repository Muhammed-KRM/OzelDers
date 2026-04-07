namespace OzelDers.Business.Interfaces;

public interface IPaymentService
{
    Task<string> InitiatePaymentAsync(decimal amount, string description, Guid userId);
    Task<bool> VerifyPaymentCallbackAsync(string callbackData);
}
