using Blink.VideoSummarizer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBlinkStorage();
builder.AddAndConfigureMessaging();

builder.Services.AddTransient<IVideoSummarizer, AiVideoSummarizer>();

var host = builder.Build();
host.Run();

