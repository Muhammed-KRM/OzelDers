using Microsoft.Extensions.Logging;
using OzelDers.Business.Interfaces;

namespace OzelDers.Business.Infrastructure.Payment;

/// <summary>
/// PayTR Stub: Gerçek PayTR API'si entegre edilene kadar loglama yapan geçici sağlayıcı.
/// PayTR hesabınız hazır olduğunda bu sınıfın içini gerçek API çağrılarıyla dolduracaksınız.
/// </summary>
public class PayTRPaymentService : IPaymentService
{
    private readonly ILogger<PayTRPaymentService> _logger;

    public PayTRPaymentService(ILogger<PayTRPaymentService> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "PayTR";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        _logger.LogInformation("[PayTR] Ödeme başlatılıyor: {Amount} {Currency} - Kullanıcı: {UserId}",
            request.Amount, request.Currency, request.UserId);

        // TODO: Gerçek PayTR API entegrasyonu
        // 1. PayTR Merchant Key, Salt gibi bilgiler appsettings.json'dan alınacak
        // 2. PayTR iframe token API'sine istek atılacak
        // 3. Dönen token ile iframe URL oluşturulacak

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"PAYTR-STUB-{Guid.NewGuid():N}",
            RedirectUrl = null,
            ErrorMessage = null
        });
    }

    public Task<bool> VerifyCallbackAsync(Dictionary<string, string> callbackData)
    {
        _logger.LogInformation("[PayTR] Callback doğrulanıyor...");
        // TODO: PayTR callback hash doğrulaması
        return Task.FromResult(true);
    }

    public Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        _logger.LogInformation("[PayTR] İade işlemi: {TransactionId}, Tutar: {Amount}", transactionId, amount);
        // TODO: PayTR iade API'si
        return Task.FromResult(true);
    }
}

/// <summary>
/// Stripe Stub: Yurt dışı ödemeler için Stripe sağlayıcısı.
/// Stripe hesabınız hazır olduğunda bu sınıfın içini gerçek API çağrılarıyla dolduracaksınız.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(ILogger<StripePaymentService> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "Stripe";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        _logger.LogInformation("[Stripe] Ödeme başlatılıyor: {Amount} {Currency} - Kullanıcı: {UserId}",
            request.Amount, request.Currency, request.UserId);

        // TODO: Stripe.NET SDK entegrasyonu
        // StripeConfiguration.ApiKey = ...
        // var session = await new SessionService().CreateAsync(...)

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"STRIPE-STUB-{Guid.NewGuid():N}",
            RedirectUrl = null,
            ErrorMessage = null
        });
    }

    public Task<bool> VerifyCallbackAsync(Dictionary<string, string> callbackData)
    {
        _logger.LogInformation("[Stripe] Webhook doğrulanıyor...");
        // TODO: Stripe webhook signature doğrulaması
        return Task.FromResult(true);
    }

    public Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        _logger.LogInformation("[Stripe] İade işlemi: {TransactionId}, Tutar: {Amount}", transactionId, amount);
        // TODO: Stripe Refund API
        return Task.FromResult(true);
    }
}

/// <summary>
/// Factory Pattern: Ülke koduna göre ödeme sağlayıcısını seçer.
/// TR → PayTR, diğer ülkeler → Stripe
/// </summary>
public class PaymentServiceFactory : IPaymentServiceFactory
{
    private readonly IEnumerable<IPaymentService> _services;
    private readonly ILogger<PaymentServiceFactory> _logger;

    public PaymentServiceFactory(IEnumerable<IPaymentService> services, ILogger<PaymentServiceFactory> logger)
    {
        _services = services;
        _logger = logger;
    }

    public IPaymentService GetPaymentService(string countryCode = "TR")
    {
        var providerName = countryCode == "TR" ? "PayTR" : "Stripe";
        var service = _services.FirstOrDefault(s => s.ProviderName == providerName);

        if (service == null)
        {
            _logger.LogWarning("Ödeme sağlayıcısı bulunamadı: {Provider}, varsayılan (PayTR) kullanılıyor.", providerName);
            service = _services.First();
        }

        _logger.LogInformation("Ödeme sağlayıcısı seçildi: {Provider} (Ülke: {Country})", service.ProviderName, countryCode);
        return service;
    }
}
