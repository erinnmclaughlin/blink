# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /source

# Copy project files
COPY src/Blink.ServiceNames/*.csproj ./Blink.ServiceNames/
COPY src/Blink.Messaging/*.csproj ./Blink.Messaging/
COPY src/Blink.Storage/*.csproj ./Blink.Storage/
COPY src/Blink.Aspire.ServiceDefaults/*.csproj ./Blink.Aspire.ServiceDefaults/
COPY src/Blink.VideoProcessor/*.csproj ./Blink.VideoProcessor/

# Restore dependencies
RUN dotnet restore ./Blink.VideoProcessor/Blink.VideoProcessor.csproj -a $TARGETARCH

# Copy all source code
COPY src/Blink.ServiceNames/. ./Blink.ServiceNames/
COPY src/Blink.Messaging/. ./Blink.Messaging/
COPY src/Blink.Storage/. ./Blink.Storage/
COPY src/Blink.Aspire.ServiceDefaults/. ./Blink.Aspire.ServiceDefaults/
COPY src/Blink.VideoProcessor/. ./Blink.VideoProcessor/

# Build and publish
WORKDIR /source/Blink.VideoProcessor
RUN dotnet publish -a $TARGETARCH --no-restore -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ffmpeg and ffprobe for video processing
# - ffmpeg: generates video thumbnails
# - ffprobe: extracts video metadata (dimensions, duration)
RUN apt-get update && \
    apt-get install -y --no-install-recommends ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Verify ffmpeg and ffprobe are available
RUN ffmpeg -version && ffprobe -version

# Copy published application
COPY --from=build /app .

# Create a non-root user
RUN useradd -m -s /bin/bash appuser && \
    chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Blink.VideoProcessor.dll"]

