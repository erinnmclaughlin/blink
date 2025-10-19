using Blink.ThumbnailGenerator;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBlinkStorage();
builder.AddBlinkMessaging(x => x.AddConsumer<VideoUploadedEventConsumer>());

builder.Services.AddTransient<IThumbnailGenerator, ThumbnailGenerator>();

var host = builder.Build();
host.Run();
