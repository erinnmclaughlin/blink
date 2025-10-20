using Blink.Web;
using Blink.Web.Authentication;
using Blink.Web.Components;
using Blink.Web.Mentions.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddServiceDefaults();
builder.AddBlinkAuthorization();
builder.AddBlinkDatabase();
builder.AddBlinkFeatureManagement();
builder.AddBlinkMessaging<BlinkWebApp>();
builder.AddBlinkStorage();

builder.Services.AddMediatR(o => o.RegisterServicesFromAssemblies(BlinkWebApp.Assembly));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLoginLogoutEndpoints();

app.MapPeopleEndpoints();

app.MapDefaultEndpoints();

app.Run();
