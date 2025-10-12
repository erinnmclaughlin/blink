using Blink;
using Blink.Storage;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddBlinkStorage(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobServiceClient(ServiceNames.AzureBlobStorage);
        builder.Services.AddScoped<IVideoStorageClient, VideoStorageClient>();
    }
}
