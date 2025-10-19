using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

public static class Helpers
{
    public static string? GetHttpsEndpoint(this IConfiguration config, string serviceName)
    {
        return config[$"services:{serviceName}:https:0"];
    }
    
    public static string? GetHttpEndpoint(this IConfiguration config, string serviceName)
    {
        return config[$"services:{serviceName}:http:0"];
    }
}
