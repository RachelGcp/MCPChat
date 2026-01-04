using ChatTranslator.McpServer.Contracts;
using ChatTranslator.McpServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ChatTranslator.McpServer.Controllers;

[ApiController]
[Route("tools")]
public class ToolsController : ControllerBase
{
    private readonly OpenAiResponsesClient _openAiClient;

    public ToolsController(OpenAiResponsesClient openAiClient)
    {
        _openAiClient = openAiClient;
    }

    [HttpPost("detect-language")]
    public async Task<DetectLanguageResponse> DetectLanguage([FromBody] DetectLanguageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Text is required");

        var (language, confidence) = await _openAiClient.DetectLanguageAsync(request.Text, ct);
        return new DetectLanguageResponse(language, confidence);
    }

    [HttpPost("translate-text")]
    public async Task<TranslateTextResponse> TranslateText([FromBody] TranslateTextRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Text is required");
        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
            throw new ArgumentException("TargetLanguage is required");

        string sourceLang = request.SourceLanguage ?? "";
        if (string.IsNullOrWhiteSpace(sourceLang) || sourceLang == "auto")
        {
            var (detectedLang, _) = await _openAiClient.DetectLanguageAsync(request.Text, ct);
            sourceLang = detectedLang;
        }

        var translatedText = await _openAiClient.TranslateAsync(request.Text, sourceLang, request.TargetLanguage, ct);
        return new TranslateTextResponse(translatedText, sourceLang, request.TargetLanguage);
    }

    [HttpPost("translate-chat-message")]
    public async Task<TranslateChatMessageResponse> TranslateChatMessage([FromBody] TranslateChatMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Text is required");
        if (string.IsNullOrWhiteSpace(request.ToLanguage))
            throw new ArgumentException("ToLanguage is required");

        string fromLang = request.FromLanguage ?? "";
        if (string.IsNullOrWhiteSpace(fromLang) || fromLang == "auto")
        {
            var (detectedLang, _) = await _openAiClient.DetectLanguageAsync(request.Text, ct);
            fromLang = detectedLang;
        }

        var translatedText = await _openAiClient.TranslateAsync(request.Text, fromLang, request.ToLanguage, ct);
        return new TranslateChatMessageResponse(request.MessageId, translatedText, fromLang, request.ToLanguage);
    }

    [HttpGet("translate-chat-message/stream")]
    public async Task StreamTranslateChatMessage(
        [FromQuery] string messageId,
        [FromQuery] string text,
        [FromQuery] string? fromLang,
        [FromQuery] string toLang,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("text is required");
        if (string.IsNullOrWhiteSpace(toLang))
            throw new ArgumentException("toLang is required");

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        string sourceLang = fromLang ?? "";
        if (string.IsNullOrWhiteSpace(sourceLang) || sourceLang == "auto")
        {
            var (detectedLang, _) = await _openAiClient.DetectLanguageAsync(text, ct);
            sourceLang = detectedLang;
        }

        await foreach (var chunk in _openAiClient.TranslateStreamAsync(text, sourceLang, toLang, ct))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}