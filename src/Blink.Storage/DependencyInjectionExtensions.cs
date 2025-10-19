using Blink.Storage;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddBlinkStorage(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobServiceClient("blobs");
        builder.Services.AddScoped<IVideoStorageClient, VideoStorageClient>();
    }
}
