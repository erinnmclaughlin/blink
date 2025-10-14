using Blink.Storage;
using Blink.VideosApi.Contracts.InitiateUpload;
using MediatR;

namespace Blink.WebApi.Videos.InitiateUpload;

public sealed class InitiateUploadHandler : IRequestHandler<InitiateUploadRequest, InitiateUploadResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly ILogger<InitiateUploadHandler> _logger;

    public InitiateUploadHandler(
        IVideoStorageClient videoStorageClient,
        ILogger<InitiateUploadHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _logger = logger;
    }

    public async Task<InitiateUploadResponse> Handle(InitiateUploadRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating upload URL for file: {FileName}", request.FileName);

        var (blobName, uploadUrl) = await _videoStorageClient.GenerateUploadUrlAsync(request.FileName, cancellationToken);

        _logger.LogInformation("Generated upload URL for blob: {BlobName}", blobName);

        return new InitiateUploadResponse
        {
            BlobName = blobName,
            UploadUrl = uploadUrl
        };
    }
}
