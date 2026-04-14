namespace BillingService.Services;

public interface IGroqService
{
    Task<string> GenerateAsync(string prompt);
}
