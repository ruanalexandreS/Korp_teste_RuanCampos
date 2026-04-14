using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StockService.Services;

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqService> _logger;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["GroqApiKey"] ?? string.Empty;
    }

    public async Task<string> SuggestDescriptionAsync(string productCode)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("GroqApiKey não configurada.");

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $"Você é um assistente de cadastro de produtos. Com base no código do produto \"{productCode}\", sugira uma descrição curta e objetiva em português. Responda apenas com a descrição, sem explicações."
                }
            },
            max_tokens = 100
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API retornou {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Groq API retornou {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? string.Empty;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("GroqApiKey não configurada.");

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 300
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API retornou {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Groq API retornou {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? string.Empty;
    }
}
