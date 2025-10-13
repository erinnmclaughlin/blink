# Blink Blazor SSR

A Blazor Server-Side Rendering (SSR) application for the Blink video platform with Keycloak authentication.

## Features

- **Server-Side Rendering**: Built with Blazor SSR for improved initial load times
- **Keycloak Authentication**: Secure authentication using OpenID Connect with Keycloak
- **Interactive Components**: Uses Interactive Server render mode for dynamic components
- **Video Management**: Upload, view, and manage videos
- **Responsive Design**: Bootstrap-based responsive UI

## Architecture

This application is a Blazor Web App that:
- Uses Server-Side Rendering (SSR) as the default render mode
- Enables Interactive Server render mode for pages requiring interactivity
- Authenticates against Keycloak using OpenID Connect
- Communicates with the Blink WebAPI backend with JWT bearer tokens

## Authentication Flow

1. User navigates to the SSR app
2. If not authenticated, redirects to `/login` endpoint
3. App initiates OpenID Connect flow with Keycloak
4. Keycloak authenticates the user and returns authorization code
5. App exchanges code for access token
6. Access token is stored in authentication cookie
7. API calls include access token in Authorization header via `BlinkApiAuthenticationHandler`

## Configuration

### Keycloak Client Setup

You need to create a new client in Keycloak for this SSR application:

1. Log into Keycloak Admin Console
2. Navigate to your realm (e.g., "blink")
3. Go to Clients → Create Client
4. Set the following:
   - Client ID: `blink-blazor-ssr`
   - Client Protocol: `openid-connect`
   - Access Type: `confidential`
   - Valid Redirect URIs: `https://localhost:7002/*` (adjust for your environment)
   - Web Origins: `https://localhost:7002`

### Application Settings

The app uses Aspire for configuration. Key settings:

- **Keycloak**: Connected via Aspire service reference
- **BlinkApi**: API base address configured in Program.cs
- **Authentication**: Uses cookie authentication with OIDC

## Running the Application

### Via Aspire AppHost

The recommended way to run the application is through the Aspire AppHost:

```bash
cd src/Blink.Aspire.AppHost
dotnet run
```

This will start all services including:
- Keycloak
- PostgreSQL
- RabbitMQ
- Azure Storage Emulator
- Blink WebAPI
- Blink WebApp (WASM)
- Blink BlazorSSR (this app)

### Standalone

To run standalone (requires Keycloak and API to be running):

```bash
cd src/Blink.BlazorSSR
dotnet run
```

Then navigate to `https://localhost:7002`

## Project Structure

```
Blink.BlazorSSR/
├── Components/
│   ├── Layout/           # Layout components
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   └── LoginDisplay.razor
│   ├── Pages/            # Page components
│   │   ├── Home.razor
│   │   ├── VideoList.razor
│   │   ├── VideoUpload.razor
│   │   └── VideoWatch.razor
│   ├── App.razor         # Root component
│   ├── Routes.razor      # Routing configuration
│   └── _Imports.razor    # Global imports
├── wwwroot/              # Static files
├── BlinkApiClient.cs     # API client
├── BlinkApiAuthenticationHandler.cs  # Auth handler
└── Program.cs            # App configuration
```

## Key Components

### BlinkApiAuthenticationHandler

A `DelegatingHandler` that automatically adds the access token from the authentication cookie to outgoing API requests.

### BlinkApiClient

A strongly-typed client for communicating with the Blink WebAPI. Includes methods for:
- Video upload
- Video listing
- Video metadata management
- Video URL generation

## Comparison with WASM App

| Feature | WASM App | SSR App |
|---------|----------|---------|
| Initial Load | Slower (downloads runtime) | Faster (server-rendered) |
| Interactivity | Client-side | Server-side (SignalR) |
| SEO | Limited | Better |
| Authentication | OIDC in browser | OIDC on server |
| Token Storage | Browser storage | Server session |
| File Upload | JavaScript fetch API | Blazor InputFile |

## Development Notes

- Uses .NET 9.0
- Interactive Server render mode for pages requiring interactivity
- Authentication state cascaded to all components
- Supports video files up to 2GB

## Troubleshooting

### "The name 'InteractiveServer' does not exist"

This error occurs if using the wrong render mode syntax. Use:
```razor
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
```

### Authentication Loop

If you experience an authentication loop:
1. Check Keycloak redirect URIs match your app URL
2. Verify cookie settings in Program.cs
3. Ensure HTTPS is properly configured

### API Connection Issues

If API calls fail:
1. Verify `BlinkApi:BaseAddress` configuration
2. Check that `BlinkApiAuthenticationHandler` is registered
3. Ensure access token is being included in requests

