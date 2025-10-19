using Blink.VideoProcessor;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBlinkStorage();
builder.AddBlinkMessaging(o => o.AddConsumersFromNamespaceContaining<BlinkVideoProcessor>());

builder.Services.AddTransient<IVideoMetadataExtractor, FFprobeMetadataExtractor>();

var host = builder.Build();
host.Run();
