namespace Blink.WebApi;

public static class CorsConfiguration
{
    public const string KnownClientPolicy = "KnownClients";

    public static void AddAndConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(o =>
        {
            var urls = new List<string>();

            // WASM App
            if (builder.Configuration["services:blink-webapp:https:0"] is { } webAppHttps)
                urls.Add(webAppHttps);

            if (builder.Configuration["services:blink-webapp:http:0"] is { } webAppHttp)
                urls.Add(webAppHttp);

            // SSR App
            if (builder.Configuration["services:blink-blazor-ssr:https:0"] is { } ssrAppHttps)
                urls.Add(ssrAppHttps);

            if (builder.Configuration["services:blink-blazor-ssr:http:0"] is { } ssrAppHttp)
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
