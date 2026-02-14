# Interactive API Documentation Setup

## Overview

This document describes how to set up and use interactive API documentation for the PoNovaWeight application using Scalar (OpenAPI/Swagger).

---

## Current Setup (Already Configured)

The application already has Scalar API UI configured in `Program.cs`:

```csharp
// Add OpenAPI
builder.Services.AddOpenApi();

// Map Scalar API reference
app.MapScalarApiReference();
```

**Access Points:**
- OpenAPI Spec: `http://localhost:5000/openapi/v1.json`
- Scalar UI: `http://localhost:5000/scalar/v1`

---

## Using the Interactive API Docs

### 1. Access the Documentation

Navigate to: `http://localhost:5000/scalar/v1`

### 2. Available Endpoints

The API exposes the following endpoint groups:

#### Authentication Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/auth/login` | GET | Initiate Google OAuth |
| `/auth/callback` | GET | OAuth callback |
| `/auth/logout` | POST | Logout |
| `/auth/status` | GET | Check auth status |

#### Daily Logs Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/dailylogs/{date}` | GET | Get daily log |
| `/api/dailylogs` | PUT | Upsert daily log |
| `/api/dailylogs/increment` | POST | Increment unit |
| `/api/dailylogs/water` | PATCH | Update water |
| `/api/dailylogs/{year}/{month}` | GET | Get month |
| `/api/dailylogs/streak` | GET | Get streaks |
| `/api/dailylogs/trends` | GET | Get trends |
| `/api/dailylogs/alcohol-correlation` | GET | Alcohol correlation |

#### Weekly Summary
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/weeklysummary/{date}` | GET | Get weekly summary |

#### Meal Scan
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/mealscan` | POST | Scan meal photo |

#### Diagnostics
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/alive` | GET | Liveness check |

---

## Testing API Endpoints

### Using Scalar UI

1. Open `http://localhost:5000/scalar/v1`
2. Click on an endpoint (e.g., `GET /api/dailylogs/{date}`)
3. Click **Try it out**
4. Enter required parameters (e.g., date: "2026-02-14")
5. Click **Send Request**
6. View the response

### Using curl

```bash
# Get daily log
curl -X GET "http://localhost:5000/api/dailylogs/2026-02-14" \
  -H "Authorization: Bearer <session_cookie>"

# Get weekly summary
curl -X GET "http://localhost:5000/api/weeklysummary/2026-02-14" \
  -H "Authorization: Bearer <session_cookie>"

# Health check
curl -X GET "http://localhost:5000/health"
```

---

## OpenAPI Configuration

### Adding Custom Documentation

To add descriptions to your endpoints, use XML comments:

```csharp
/// <summary>
/// Gets a daily food log for a specific date.
/// </summary>
/// <param name="date">Date in yyyy-MM-dd format</param>
/// <returns>DailyLogDto or 404 if not found</returns>
[HttpGet("api/dailylogs/{date}")]
public async Task<ActionResult<DailyLogDto>> GetDailyLog(string date)
{
    // ...
}
```

### OpenAPI Generator

Generate clients from OpenAPI spec:

```bash
# Install OpenAPI generator
npm install -g @openapitools/openapi-generator-cli

# Generate C# client
openapi-generator generate \
  -i http://localhost:5000/openapi/v1.json \
  -g csharp \
  -o ./generated-client
```

---

## Integration with API Management

For production, consider:

### Azure API Management
```yaml
# Add to Bicep for APIM
resource apim 'Microsoft.ApiManagement/service@2023-05-01' = {
  name: '${baseName}-apim'
  location: location
  sku: {
    name: 'Consumption'
  }
  properties: {
    publisherEmail: 'admin@example.com'
    publisherName: 'PoNovaWeight'
  }
}
```

### PowerApps/Logic Apps Integration
The OpenAPI spec can be imported into:
- Power Automate
- Power Apps
- Azure Logic Apps
- Azure Functions

---

## Security Considerations

### Authentication Required Endpoints
Most endpoints require authentication. When testing via Scalar UI:

1. First, authenticate via browser:
   - Navigate to `http://localhost:5000/auth/login`
   - Complete Google OAuth flow
   
2. Copy session cookie:
   - Open DevTools (F12)
   - Application tab → Cookies
   - Copy `.AspNetCore.Cookies` value

3. Add to Scalar UI:
   - Click **Add authentication**
   - Paste cookie value

---

## Customizing Scalar UI

### Custom Theme

```csharp
app.MapScalarApiReference(options => {
    options
        .WithOpenApiRoutePattern("/openapi/v1.json")
        .WithTheme(ScalarTheme.Kepler)
        .WithTitle("PoNovaWeight API")
        .WithDescription("Food Journaling PWA API");
});
```

### Available Themes
- Default
- Kepler
- Saturn
- Neptune
- Purple
- Material

---

## Troubleshooting

### OpenAPI Spec Not Loading
- Check that the app is running: `dotnet run --project src/PoNovaWeight.Api`
- Verify endpoint: `curl http://localhost:5000/openapi/v1.json`

### 401 Errors When Testing
- Authentication is required for most endpoints
- Use browser to authenticate first, then copy cookie to Scalar UI
- Or use DevAuth for local testing

### Missing Endpoints in Spec
- Ensure endpoints are mapped before `app.MapFallbackToFile()`
- Check that MediatR handlers have proper HTTP attributes
