using System.Net.Http.Json;
using System.Text.Json;

var baseUrl = Environment.GetEnvironmentVariable("MCP_BASE_URL") ?? "http://localhost:5000";
using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine("Chat Translator Demo");
Console.WriteLine("1 = Non-streaming");
Console.WriteLine("2 = Streaming");
Console.Write("Choose mode: ");
var mode = Console.ReadLine();

if (mode == "2")
{
    await RunStreamingMode(httpClient);
}
else
{
    await RunNonStreamingMode(httpClient);
}

async Task RunNonStreamingMode(HttpClient client)
{
    Console.WriteLine("\nNon-streaming mode - Chat between Hebrew (A) and English (B) users");
    Console.WriteLine("Type 'quit' to exit\n");

    while (true)
    {
        Console.Write("User A (Hebrew): ");
        var messageA = Console.ReadLine();
        if (messageA == "quit") break;
        if (string.IsNullOrWhiteSpace(messageA)) continue;

        var requestA = new
        {
            MessageId = Guid.NewGuid().ToString(),
            Text = messageA,
            FromUserId = "userA",
            ToUserId = "userB",
            FromLanguage = "he",
            ToLanguage = "en"
        };

        try
        {
            var responseA = await client.PostAsJsonAsync("/tools/translate-chat-message", requestA);
            responseA.EnsureSuccessStatusCode();
            var translationA = await responseA.Content.ReadFromJsonAsync<JsonElement>();
            Console.WriteLine($"B sees: {translationA.GetProperty("translatedText").GetString()}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
            continue;
        }

        Console.Write("User B (English): ");
        var messageB = Console.ReadLine();
        if (messageB == "quit") break;
        if (string.IsNullOrWhiteSpace(messageB)) continue;

        var requestB = new
        {
            MessageId = Guid.NewGuid().ToString(),
            Text = messageB,
            FromUserId = "userB",
            ToUserId = "userA",
            FromLanguage = "en",
            ToLanguage = "he"
        };

        try
        {
            var responseB = await client.PostAsJsonAsync("/tools/translate-chat-message", requestB);
            responseB.EnsureSuccessStatusCode();
            var translationB = await responseB.Content.ReadFromJsonAsync<JsonElement>();
            Console.WriteLine($"A sees: {translationB.GetProperty("translatedText").GetString()}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }
}

async Task RunStreamingMode(HttpClient client)
{
    Console.WriteLine("\nStreaming mode - Enter text to translate");
    Console.WriteLine("Type 'quit' to exit\n");

    while (true)
    {
        Console.Write("Text to translate: ");
        var text = Console.ReadLine();
        if (text == "quit") break;
        if (string.IsNullOrWhiteSpace(text)) continue;

        Console.Write("From language (or auto): ");
        var fromLang = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(fromLang)) fromLang = "auto";

        Console.Write("To language: ");
        var toLang = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(toLang)) continue;

        var messageId = Guid.NewGuid().ToString();
        var url = $"/tools/translate-chat-message/stream?messageId={Uri.EscapeDataString(messageId)}&text={Uri.EscapeDataString(text)}&fromLang={Uri.EscapeDataString(fromLang)}&toLang={Uri.EscapeDataString(toLang)}";

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Console.Write("Translation: ");
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]")
                    {
                        Console.WriteLine();
                        break;
                    }
                    Console.Write(data);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }
}
