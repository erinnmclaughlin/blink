using Blink.WebApp.Components;
using Blink.WebApp.Components.Account;
using Blink.WebApp.Configuration.Pipeline;
using Blink.WebApp.Data;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped(sp => (IUserEmailStore<BlinkUser>)sp.GetRequiredService<IUserStore<BlinkUser>>());

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.AddOpenBehavior(typeof(RequestValidationPipelineBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.AddNpgsqlDbContext<BlinkDbContext>("blink-pg-db");
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<BlinkUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<BlinkDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<BlinkUser>, IdentityEmailSender>();

var emailEndpoint = builder.Configuration.GetConnectionString("papercut")!;
emailEndpoint = emailEndpoint.Split('=')[1];
var portIndex = emailEndpoint.LastIndexOf(':');

var host = emailEndpoint[..portIndex].Replace("smtp://", "");
var port = emailEndpoint[(portIndex + 1)..];

builder.Services.Configure<EmailOptions>(options =>
{
    options.Host = host;
    options.Port = int.Parse(port);
    options.From = "noreply@blink.test";
});

builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddFeatureManagement();
builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o =>
{
    o.CustomConfigurationMergingEnabled = true;
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    //app.UseMigrationsEndPoint();
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BlinkDbContext>();
    await dbContext.Database.MigrateAsync();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.MapAdditionalIdentityEndpoints();

app.Run();
