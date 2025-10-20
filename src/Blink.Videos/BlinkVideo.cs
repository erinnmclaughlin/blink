namespace Blink.Videos;

public sealed class BlinkVideo
{
    /// <summary>
    /// The unique identifier of the video.
    /// </summary>
    public Guid Id { get; private init; }
    
    /// <summary>
    /// The video title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// The video description.
    /// </summary>
    public string Description { get; private set; } = "";
    
    /// <summary>
    /// The capture date of the video.
    /// </summary>
    public DateOnly? CaptureDate { get; private set; }
    
    /// <summary>
    /// The video file.
    /// </summary>
    public BlinkFileInfo File { get; private init; }

    /// <summary>
    /// The video metadata.
    /// </summary>
    public BlinkVideoMetaData MetaData { get; private init; } = new();
    
    /// <summary>
    /// The ID of the user who owns the video.
    /// </summary>
    public string OwnerId { get; private init; }
    
    /// <summary>
    /// The date and time that the video was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; private init; }

    private BlinkVideo(Guid id, string title, string fileName, long fileSize, string ownerId, DateTimeOffset uploadedAt)
    {
        Id = id;
        Title = title;
        File = new BlinkFileInfo(id, fileName, fileSize);
        OwnerId = ownerId;
        UploadedAt = uploadedAt;
    }

    public void SetCaptureDate(DateOnly captureDate)
    {
        CaptureDate = captureDate;
    }

    public void ClearCaptureDate()
    {
        CaptureDate = null;
    }
    
    public void SetDescription(string description)
    {
        Description = description;
    }

    public void ClearDescription()
    {
        Description = string.Empty;
    }

    public void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    public sealed class BlinkVideoFactory : IBlinkVideoFactory
    {
        private readonly IDateProvider _dateProvider;
        private readonly IGuidGenerator _guidGenerator;

        public BlinkVideoFactory(IDateProvider dateProvider, IGuidGenerator guidGenerator)
        {
            _dateProvider = dateProvider;
            _guidGenerator = guidGenerator;
        }

        public BlinkVideo CreateNew(string title, string fileName, long fileSize, string ownerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fileSize, 0, nameof(fileSize));
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

            var videoId = _guidGenerator.NewGuid();
            return new BlinkVideo(videoId, title, fileName, fileSize, ownerId, _dateProvider.UtcNow);
        }
    }
}
