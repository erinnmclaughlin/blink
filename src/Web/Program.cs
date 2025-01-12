using Blink.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.AddServiceDefaults();

//builder.Services.AddRazorComponents().AddInteractiveServerComponents();

//builder.Services.AddOutputCache();

builder.Services
    .AddHttpClient<UsersApiClient>(client =>
    {
        client.BaseAddress = new Uri("https+http://blink-api");
    })
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
        handler.ConfigureHandler(["https+http://blink-api"], scopes: ["https://blinkapp.onmicrosoft.com/blinkapi/access_as_user"]);
        return handler;
    });

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("offline_access");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://blinkapp.onmicrosoft.com/blinkapi/access_as_user");
    
    // Optional: Use redirect instead of popup for login UI.
    options.ProviderOptions.LoginMode = "redirect";
});

var app = builder.Build();

/*
if (!builder.HostEnvironment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true); 
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
*/

await app.RunAsync();
