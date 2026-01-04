namespace ChatTranslator.McpServer.Contracts;

public record DetectLanguageRequest(string Text);
public record DetectLanguageResponse(string Language, double Confidence);

public record TranslateTextRequest(string Text, string? SourceLanguage, string TargetLanguage);
public record TranslateTextResponse(string TranslatedText, string UsedSourceLanguage, string TargetLanguage);

public record TranslateChatMessageRequest(string MessageId, string Text, string FromUserId, string ToUserId, string? FromLanguage, string ToLanguage);
public record TranslateChatMessageResponse(string MessageId, string TranslatedText, string DetectedFromLanguage, string ToLanguage);