using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Blink.WebApi;

/// <summary>
/// Configures CORS on Azure Blob Storage at startup (for local Azurite development)
/// </summary>
public class BlobStorageCorsConfigurator : IHostedService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageCorsConfigurator> _logger;

    public BlobStorageCorsConfigurator(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageCorsConfigurator> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Configuring CORS for blob storage...");

            // Get current service properties
            var properties = await _blobServiceClient.GetPropertiesAsync(cancellationToken);

            // Configure CORS rules for direct browser uploads
            var corsRules = new List<BlobCorsRule>
            {
                new BlobCorsRule
                {
                    AllowedOrigins = "*", // In production, restrict to specific origins
                    AllowedMethods = "GET,HEAD,PUT,POST,OPTIONS",
                    AllowedHeaders = "*",
                    ExposedHeaders = "*",
                    MaxAgeInSeconds = 3600
                }
            };

            properties.Value.Cors.Clear();
            foreach (var rule in corsRules)
            {
                properties.Value.Cors.Add(rule);
            }

            // Apply the updated properties
            await _blobServiceClient.SetPropertiesAsync(properties.Value, cancellationToken);

            _logger.LogInformation("CORS configured successfully for blob storage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure CORS for blob storage. This is expected if using managed identity without proper permissions. CORS may need to be configured manually in Azure Portal.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

