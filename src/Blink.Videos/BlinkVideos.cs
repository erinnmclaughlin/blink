using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Blink.Videos;

public sealed class BlinkVideos
{
    public static Assembly Assembly => typeof(BlinkVideos).Assembly;
}

public static class BlinkVideosConfiguration
{
    public static void AddBlinkVideosCore(this IHostApplicationBuilder builder)
    {
        builder.Services.TryAddTransient<IBlinkVideoFactory, BlinkVideo.BlinkVideoFactory>();
    }
}
