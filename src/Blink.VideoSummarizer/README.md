# Blink.VideoSummarizer

AI-powered video summarization service that consumes `VideoUploadedEvent` messages and generates content summaries.

## Overview

This service is a MassTransit consumer that listens for video upload events and generates AI summaries of the video content. It follows the same architecture pattern as `Blink.VideoProcessor`.

## Architecture

- **Consumer**: `VideoSummaryGenerator` - Consumes `VideoUploadedEvent` messages
- **Service**: `IVideoSummarizer` / `AiVideoSummarizer` - Generates summaries based on video metadata
- **Message Bus**: RabbitMQ via MassTransit
- **Storage**: Azure Blob Storage for video access

## Current Implementation

The current implementation generates summaries based on video metadata (title, description, content type). This is a placeholder that allows the service to run without requiring AI model integration.

## Future AI Integration

To add real AI capabilities, you can integrate with:

1. **Azure OpenAI / OpenAI**
   - Extract key frames from video using FFmpeg
   - Use GPT-4 Vision to analyze frames
   - Optionally transcribe audio with Whisper

2. **Azure AI Vision**
   - Use Azure Computer Vision for video analysis
   - Extract captions and tags from video content

3. **Ollama (Local AI)**
   - Run local LLaVA or similar vision models
   - Good for development and testing

### Example Integration

```csharp
// In Program.cs, add AI client
builder.Services.AddChatClient(builder => 
    builder.UseOpenAI(apiKey, modelId));

// Update AiVideoSummarizer constructor to accept IChatClient
```

## Configuration

The service requires the following connection strings:

- `Messaging` - RabbitMQ connection string
- `blob` - Azure Blob Storage connection string

## Running Locally

The service is automatically started by the Aspire AppHost when running on Windows:

```bash
dotnet run --project src/Blink.Aspire.AppHost
```

Or run standalone:

```bash
dotnet run --project src/Blink.VideoSummarizer
```

## Docker

The service includes a Dockerfile for containerized deployment:

```bash
docker build -f src/Blink.VideoSummarizer/Dockerfile -t blink-video-summarizer .
```

## Message Flow

1. User uploads a video via `Blink.Web`
2. `VideoUploadedEvent` is published to RabbitMQ
3. `VideoSummaryGenerator` consumes the event
4. Service downloads video from blob storage
5. `AiVideoSummarizer` generates summary
6. Summary is logged (TODO: persist to database or publish event)

## Next Steps

- [ ] Add real AI integration (OpenAI, Azure AI, Ollama)
- [ ] Implement video frame extraction using FFmpeg
- [ ] Add audio transcription support
- [ ] Publish `VideoSummaryGeneratedEvent` 
- [ ] Store summaries in database
- [ ] Add summary to video details page in UI

