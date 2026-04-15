namespace OzelDers.Business.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}
