using Blink.VideoProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBlinkStorage();
builder.AddAndConfigureMessaging();

builder.Services.AddTransient<IThumbnailGenerator, ThumbnailGenerator>();

var host = builder.Build();
host.Run();
