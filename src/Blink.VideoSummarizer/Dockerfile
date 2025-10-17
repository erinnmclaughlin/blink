FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Blink.VideoSummarizer/Blink.VideoSummarizer.csproj", "src/Blink.VideoSummarizer/"]
COPY ["src/Blink.Messaging/Blink.Messaging.csproj", "src/Blink.Messaging/"]
COPY ["src/Blink.ServiceNames/Blink.ServiceNames.csproj", "src/Blink.ServiceNames/"]
COPY ["src/Blink.Storage/Blink.Storage.csproj", "src/Blink.Storage/"]
COPY ["src/Blink.Aspire.ServiceDefaults/Blink.Aspire.ServiceDefaults.csproj", "src/Blink.Aspire.ServiceDefaults/"]

RUN dotnet restore "src/Blink.VideoSummarizer/Blink.VideoSummarizer.csproj"

COPY . .
WORKDIR "/src/src/Blink.VideoSummarizer"
RUN dotnet build "Blink.VideoSummarizer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Blink.VideoSummarizer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blink.VideoSummarizer.dll"]

