using Microsoft.Extensions.Configuration;

namespace Blink;

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