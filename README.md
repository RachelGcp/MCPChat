# Chat Translator Demo

A .NET 8 solution demonstrating a translator chat using an MCP server implemented as an ASP.NET Core Web API with OpenAI integration.

## Prerequisites

- .NET 8 SDK
- OpenAI API key

## Setup

1. Set environment variables:
   ```bash
   set OPENAI_API_KEY=your_openai_api_key_here
   set MCP_BASE_URL=http://localhost:5000  # optional, defaults to this
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

## Running

### Start the MCP Server
```bash
dotnet run --project src/ChatTranslator.McpServer
```

The server will start on `http://localhost:5000` with Swagger UI available at `/swagger`.

### Run the Client Console
```bash
dotnet run --project src/ChatTranslator.ClientConsole
```

Choose between:
- **Mode 1 (Non-streaming)**: Simulates chat between Hebrew and English users
- **Mode 2 (Streaming)**: Interactive translation with real-time streaming

## API Endpoints

### Tools Endpoints
- `POST /tools/detect-language` - Detect language of text
- `POST /tools/translate-text` - Translate text with language detection
- `POST /tools/translate-chat-message` - Translate chat messages
- `GET /tools/translate-chat-message/stream` - Streaming translation (SSE)

### Example Usage

**Detect Language:**
```json
POST /tools/detect-language
{
  "text": "שלום עולם"
}
```

**Translate Text:**
```json
POST /tools/translate-text
{
  "text": "Hello world",
  "sourceLanguage": "en",
  "targetLanguage": "he"
}
```

**Streaming Translation:**
```
GET /tools/translate-chat-message/stream?text=Hello&fromLang=en&toLang=he
```

## Features

- Language detection using OpenAI
- Non-streaming translation with structured outputs
- Server-Sent Events (SSE) streaming translation
- Minimal dependencies (no OpenAI SDK)
- Environment-based configuration
- Swagger documentation