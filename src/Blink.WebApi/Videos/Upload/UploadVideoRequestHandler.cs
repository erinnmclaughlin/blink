using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestHandler : IRequestHandler<UploadVideoRequest, UploadedVideoInfo>
{
    private readonly IVideoStorageClient _videoStorageClient;

    public UploadVideoRequestHandler(IVideoStorageClient videoStorageClient)
    {
        _videoStorageClient = videoStorageClient;
    }

    public async Task<UploadedVideoInfo> Handle(UploadVideoRequest request, CancellationToken cancellationToken)
    {
        using var stream = request.File.OpenReadStream();
        var (blobName, fileSize) = await _videoStorageClient.UploadAsync(stream, request.File.FileName, cancellationToken);

        return new UploadedVideoInfo
        {
            BlobName = blobName,
            FileName = request.File.FileName,
            FileSize = fileSize
        };
    }
}