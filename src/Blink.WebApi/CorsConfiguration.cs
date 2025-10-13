namespace Blink.WebApi;

public static class CorsConfiguration
{
    public const string KnownClientPolicy = "KnownClients";

    public static void AddAndConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(o =>
        {
            var urls = new List<string>();

            if (builder.Configuration[$"services:{ServiceNames.BlinkWebApp}:https:0"] is { } ssrAppHttps)
                urls.Add(ssrAppHttps);

            if (builder.Configuration[$"services:{ServiceNames.BlinkWebApp}:http:0"] is { } ssrAppHttp)
                urls.Add(ssrAppHttp);

            o.AddPolicy("KnownClients", b =>
            {
                b.WithOrigins([..urls]);
                b.AllowAnyHeader();
                b.AllowAnyMethod();
                b.AllowCredentials();
            });
        });
    }
}
