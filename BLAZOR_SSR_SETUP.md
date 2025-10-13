# Blink Blazor SSR Application Setup Guide

## Overview

A new Blazor Server-Side Rendering (SSR) application has been added to the Blink project that authenticates against Keycloak using OpenID Connect, similar to the existing WASM app.

## What Was Added

### New Project: `Blink.BlazorSSR`

A complete Blazor SSR application with:
- OpenID Connect authentication with Keycloak
- JWT bearer token handling for API calls
- All pages from the WASM app (Home, VideoList, VideoUpload, VideoWatch)
- Interactive Server render mode for dynamic components
- Modern, responsive UI using Bootstrap

### Key Files Created

```
src/Blink.BlazorSSR/
├── Blink.BlazorSSR.csproj                      # Project file
├── Program.cs                                   # App configuration with OIDC
├── BlinkApiClient.cs                           # API client with all video methods
├── BlinkApiAuthenticationHandler.cs            # Adds JWT tokens to API requests
├── Components/
│   ├── App.razor                               # Root component
│   ├── Routes.razor                            # Routing with auth
│   ├── _Imports.razor                          # Global imports
│   ├── Layout/
│   │   ├── MainLayout.razor                    # Main layout
│   │   ├── NavMenu.razor                       # Navigation menu
│   │   ├── LoginDisplay.razor                  # Login/logout UI
│   │   └── RedirectToLogin.razor               # Login redirect
│   └── Pages/
│       ├── Home.razor                          # Home page with API tests
│       ├── VideoList.razor                     # Video gallery
│       ├── VideoUpload.razor                   # Video upload form
│       └── VideoWatch.razor                    # Video player
├── wwwroot/                                     # Static files
├── appsettings.json                            # Configuration
└── README.md                                    # Project documentation
```

## Keycloak Client Configuration

### Required: Create New Keycloak Client

You **must** create a new client in Keycloak for this SSR application:

1. Open Keycloak Admin Console (http://localhost:8080 when running via Aspire)
2. Select the "blink" realm
3. Navigate to **Clients** → **Create client**
4. Configure the new client:

   **General Settings:**
   - Client ID: `blink-blazor-ssr`
   - Name: `Blink Blazor SSR`
   - Description: `Blazor Server-Side Rendering Application`
   - Client Protocol: `openid-connect`

   **Capability Config:**
   - Client authentication: ON (this makes it confidential)
   - Authorization: OFF
   - Authentication flow:
     - Standard flow: ON (authorization code flow)
     - Direct access grants: OFF
     - Implicit flow: OFF

   **Login Settings:**
   - Root URL: `https://localhost:7002`
   - Valid redirect URIs: `https://localhost:7002/*`
   - Valid post logout redirect URIs: `https://localhost:7002/*`
   - Web origins: `https://localhost:7002`

5. After creating, go to the **Credentials** tab
6. Copy the **Client Secret** (you'll need this for production deployments)

### Client Scopes

The application requests the following scopes:
- `openid` - Required for OIDC
- `profile` - User profile information
- `email` - User email address

These should be automatically available in the "blink" realm.

## How Authentication Works

### Flow Diagram

```
User → SSR App → /login → Keycloak → Authorization Code → SSR App
                                             ↓
                                        Access Token
                                             ↓
                        API Calls ← [Bearer Token] ← SSR App
```

### Detailed Flow

1. **Unauthenticated Request**: User navigates to a protected page
2. **Redirect to Login**: App redirects to `/login` endpoint
3. **OIDC Challenge**: Server initiates OpenID Connect flow, redirects to Keycloak
4. **Keycloak Authentication**: User authenticates with Keycloak
5. **Authorization Code**: Keycloak redirects back with authorization code
6. **Token Exchange**: App exchanges code for access token and refresh token
7. **Cookie Storage**: Tokens stored in encrypted authentication cookie
8. **API Calls**: `BlinkApiAuthenticationHandler` adds access token to API requests
9. **Authorized Access**: User can access protected pages and API endpoints

## Running the Application

### Via Aspire (Recommended)

The SSR app is integrated into the Aspire AppHost:

```bash
cd src/Blink.Aspire.AppHost
dotnet run
```

Then open the Aspire dashboard (usually http://localhost:15174) and:
1. Wait for all services to start (Keycloak, PostgreSQL, RabbitMQ, Azure Storage)
2. Configure Keycloak client (see above)
3. Navigate to the Blink BlazorSSR endpoint (https://localhost:7002)

### URLs

When running via Aspire:
- **Blink SSR App**: https://localhost:7002
- **Blink WASM App**: Check Aspire dashboard
- **Blink API**: Check Aspire dashboard
- **Keycloak**: http://localhost:8080

## Architecture Comparison

### SSR App (New)

**Advantages:**
- ✅ Faster initial page load (server-rendered HTML)
- ✅ Better SEO (search engines see full HTML)
- ✅ Smaller initial download size
- ✅ Server-side token management (more secure)
- ✅ No CORS issues (server-to-server API calls)

**Considerations:**
- Requires SignalR connection for interactivity
- Server resources needed for each connected user
- Slight delay for interactive updates (network round-trip)

### WASM App (Existing)

**Advantages:**
- ✅ No server resources for UI after initial load
- ✅ Instant UI updates (runs in browser)
- ✅ Can work offline (with service worker)

**Considerations:**
- Larger initial download (.NET runtime + assemblies)
- Slower initial page load
- Token stored in browser
- CORS configuration required

## Testing the Application

### 1. Test Authentication

1. Navigate to https://localhost:7002
2. Click "Log in"
3. You should be redirected to Keycloak
4. Log in with a Keycloak user
5. You should be redirected back to the app
6. You should see "Hello, [username]" in the top bar

### 2. Test API Integration

The Home page shows three tests:
- **Public API Test**: Tests unauthenticated endpoint
- **Authenticated API Test**: Tests authenticated endpoint
- **Claims from API**: Shows JWT token claims

All three should show success ✓

### 3. Test Video Features

1. Click "Videos" in the sidebar
2. Click "Upload Video"
3. Upload an MP4 file
4. View uploaded video
5. Test video playback

## Configuration

### App Settings

The app uses Aspire for service discovery. Key configurations:

**BlinkApi:BaseAddress**
- Default: `https://localhost:7001`
- Auto-configured by Aspire

**Keycloak Settings**
- Auto-configured by Aspire via service reference
- Realm: `blink`
- Client ID: `blink-blazor-ssr`

### Development Secrets

For production, you'll need to configure:

```bash
dotnet user-secrets set "Keycloak:ClientSecret" "<your-client-secret>"
```

## Troubleshooting

### Error: "Unable to obtain configuration from keycloak"

**Cause**: Keycloak is not running or not accessible

**Solution**:
1. Ensure Aspire AppHost is running
2. Check Keycloak container is started in Aspire dashboard
3. Wait 30 seconds for Keycloak to fully initialize

### Error: "invalid_redirect_uri"

**Cause**: Redirect URI not configured in Keycloak client

**Solution**:
1. Open Keycloak Admin Console
2. Go to Clients → blink-blazor-ssr
3. Add `https://localhost:7002/*` to Valid redirect URIs
4. Add `https://localhost:7002` to Web origins
5. Save

### Error: "Unauthorized" when calling API

**Cause**: Access token not being sent or not valid

**Solution**:
1. Check `BlinkApiAuthenticationHandler` is registered
2. Verify `HttpContextAccessor` is registered
3. Check token is stored: Inspect browser cookies for `.AspNetCore.Cookies`
4. Check API is accepting tokens from Keycloak realm

### Authentication Loop

**Cause**: Cookie configuration or redirect URI mismatch

**Solution**:
1. Clear browser cookies
2. Verify redirect URIs in Keycloak match exactly (including protocol)
3. Check `RequireHttpsMetadata` setting in Program.cs
4. Ensure URLs don't have trailing slashes where not expected

## Next Steps

1. **Configure Keycloak client** as described above
2. **Test the application** following the testing guide
3. **Compare with WASM app** to see the differences
4. **Choose the right app** for your use case:
   - Need SEO? → Use SSR
   - Need offline support? → Use WASM
   - Maximum security? → Use SSR
   - Minimal server resources? → Use WASM

## Additional Resources

- [Blazor Server Documentation](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-server)
- [OpenID Connect with ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)

## Support

For issues specific to this implementation:
1. Check build errors: `dotnet build src/Blink.BlazorSSR/Blink.BlazorSSR.csproj`
2. Review logs in Aspire dashboard
3. Check Keycloak admin console for client configuration
4. Verify API is accessible and accepting tokens

