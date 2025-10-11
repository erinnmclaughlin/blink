using Azure.Storage.Blobs;

namespace Blink.WebApi.Services;

public static class BlobStorageServiceRegistration
{
    public static IHostApplicationBuilder AddBlobStorage(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobServiceClient("blobs");
        builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

        return builder;
    }
}

