namespace StockService.Services;

public interface IGroqService
{
    Task<string> SuggestDescriptionAsync(string productCode);
    Task<string> GenerateAsync(string prompt);
}
