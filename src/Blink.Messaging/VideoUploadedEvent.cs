using Blink.Videos;

namespace Blink.Messaging;

/// <summary>
/// Event published when a video has been successfully uploaded
/// </summary>
public sealed record VideoUploadedEvent(BlinkVideo Video);

