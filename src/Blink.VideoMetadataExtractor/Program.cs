using Blink.VideoMetadataExtractor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBlinkStorage();
builder.AddBlinkMessaging(x => x.AddConsumer<VideoUploadedEventConsumer>());

builder.Services.AddTransient<IVideoMetadataExtractor, FFprobeMetadataExtractor>();

var host = builder.Build();
host.Run();
