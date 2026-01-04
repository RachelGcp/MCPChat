using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace ChatTranslator.McpServer.Infrastructure;

public class OpenAiResponsesClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiResponsesClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<(string language, double confidence)> DetectLanguageAsync(string text, CancellationToken ct = default)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                language = new { type = "string", description = "ISO language code" },
                confidence = new { type = "number", description = "Confidence score between 0 and 1" }
            },
            required = new[] { "language", "confidence" }
        };

        var response = await PostResponsesAsync(
            $"Detect the language of this text and return the ISO language code with confidence: {text}",
            schema, ct);

        using var doc = JsonDocument.Parse(response);
        var language = doc.RootElement.GetProperty("language").GetString()!;
        var confidence = doc.RootElement.GetProperty("confidence").GetDouble();
        
        return (language, confidence);
    }

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct = default)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                translatedText = new { type = "string", description = "The translated text" }
            },
            required = new[] { "translatedText" }
        };

        var prompt = $"Translate this text from {sourceLang} to {targetLang}, preserving tone, slang, and emojis: {text}";
        var response = await PostResponsesAsync(prompt, schema, ct);

        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("translatedText").GetString()!;
    }

    public async IAsyncEnumerable<string> TranslateStreamAsync(string text, string sourceLang, string targetLang, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = $"Translate this text from {sourceLang} to {targetLang}, preserving tone, slang, and emojis. Return ONLY the translated text, no explanations: {text}";
        
        await foreach (var chunk in PostResponsesStreamAsync(prompt, ct))
        {
            yield return chunk;
        }
    }

    private async Task<string> PostResponsesAsync(string prompt, object schema, CancellationToken ct)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "response",
                    schema = schema
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = content;

        var response = await _httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;
    }

    private async IAsyncEnumerable<string> PostResponsesStreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = content;

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !ct.IsCancellationRequested)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentElement))
                        {
                            chunk = contentElement.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON
                }

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }
    }
}