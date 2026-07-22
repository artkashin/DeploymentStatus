# OpenAPI/Swagger Setup Guide

## 📖 Overview

Your Deployment Status API now includes OpenAPI 3.0 specification with Swagger UI for interactive API documentation.

---

## 🌐 Access Points

### Local Development

**Swagger UI (Interactive Documentation):**
```
http://localhost:7071/api/swagger/ui
```

**OpenAPI Specification (JSON):**
```
http://localhost:7071/api/swagger.json
```

**OpenAPI Specification (YAML):**
```
http://localhost:7071/api/swagger.yaml
```

### Azure Production

**Swagger UI:**
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger/ui
```

**OpenAPI JSON:**
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json
```

**OpenAPI YAML:**
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.yaml
```

---

## 🚀 Quick Start

### 1. Run the API Locally

```powershell
cd DeploymentAPI
func start
```

### 2. Open Swagger UI

Navigate to: http://localhost:7071/api/swagger/ui

### 3. Explore the API

- Browse all available endpoints
- View request/response schemas
- Try out endpoints interactively
- See authentication requirements

---

## 🔑 Authentication in Swagger UI

### For Function-Level Endpoints

1. Click **"Authorize"** button in Swagger UI
2. Enter your Azure Function master/host key in the `code` field
3. Click **Authorize**
4. Now you can test protected endpoints

### For Anonymous Endpoints

No authentication needed - test directly!

---

## 📥 Download OpenAPI Specification

### Using Browser

**JSON Format:**
```
http://localhost:7071/api/swagger.json
```
Right-click → Save As → `openapi.json`

**YAML Format:**
```
http://localhost:7071/api/swagger.yaml
```
Right-click → Save As → `openapi.yaml`

### Using PowerShell

**Download JSON:**
```powershell
Invoke-WebRequest -Uri "http://localhost:7071/api/swagger.json" -OutFile "openapi.json"
```

**Download YAML:**
```powershell
Invoke-WebRequest -Uri "http://localhost:7071/api/swagger.yaml" -OutFile "openapi.yaml"
```

### Using curl

**JSON:**
```bash
curl http://localhost:7071/api/swagger.json > openapi.json
```

**YAML:**
```bash
curl http://localhost:7071/api/swagger.yaml > openapi.yaml
```

---

## 📊 What's Documented

### API Information
- **Title:** Deployment Status API
- **Version:** v1
- **Description:** API for managing deployment status, CI/CD versions, and GitHub workflow integration
- **OpenAPI Version:** 3.0.x

### Endpoints Documented

All your Azure Functions endpoints are automatically documented:

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/clients/status` | GET | Function | Get status of all clients |
| `/api/clients/{clientId}/status` | GET | Function | Get specific client status |
| `/api/applications` | GET | Anonymous | Get all applications |
| `/api/customers` | GET | Anonymous | Get all customers |
| `/api/update-all-customers/latest` | GET | Function | Get latest workflow status |
| `/api/workflow-runs/{runId}/customer-status` | GET | Function | Get workflow customer status |
| ... and more | | | |

### Schema Documentation

- Request models
- Response models
- Data types
- Validation rules
- Example values

---

## 🎨 Customizing Documentation

### Update API Info

Edit `DeploymentAPI/OpenApi/OpenApiConfigurationOptions.cs`:

```csharp
public override OpenApiInfo Info { get; set; } = new OpenApiInfo
{
	Version = "v1",
	Title = "Your API Title",
	Description = "Your API description",
	Contact = new OpenApiContact
	{
		Name = "Your Name",
		Email = "your@email.com"
	}
};
```

### Add OpenAPI Attributes to Functions

**Example:**

```csharp
[OpenApiOperation(operationId: "GetClients", tags: new[] { "Clients" })]
[OpenApiParameter(name: "clientId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ClientStatus))]
public async Task<IActionResult> Run(...)
```

**Available Attributes:**
- `[OpenApiOperation]` - Operation metadata
- `[OpenApiParameter]` - Parameter documentation
- `[OpenApiRequestBody]` - Request body schema
- `[OpenApiResponseWithBody]` - Response with body
- `[OpenApiResponseWithoutBody]` - Response without body
- `[OpenApiSecurity]` - Security requirements

---

## 🔧 Troubleshooting

### Swagger UI Not Loading

**Check:**
1. Is the API running? `func start`
2. Is port 7071 available?
3. Navigate to exact URL: `http://localhost:7071/api/swagger/ui`

**Solution:**
```powershell
# Stop any running Functions
Get-Process func | Stop-Process -Force

# Start fresh
cd DeploymentAPI
func start
```

### 404 on Swagger Endpoints

**Check:**
1. Is OpenAPI package installed?
2. Is OpenAPI service registered in `Program.cs`?
3. Restore packages: `dotnet restore`

### Function Keys Not Working in Swagger

**For Local:**
- Local development doesn't require keys for testing
- Use **AuthorizationLevel.Anonymous** for testing

**For Azure:**
- Get key from Azure Portal
- Enter in Swagger UI Authorize dialog

### OpenAPI JSON Not Generating

**Check:**
1. Rebuild the project: `dotnet build`
2. Check for compilation errors
3. Verify OpenAPI attributes syntax

---

## 📚 Additional Resources

### Official Documentation
- [Azure Functions OpenAPI Extension](https://github.com/Azure/azure-functions-openapi-extension)
- [OpenAPI Specification](https://swagger.io/specification/)
- [Swagger UI](https://swagger.io/tools/swagger-ui/)

### NuGet Package
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.OpenApi" Version="1.5.1" />
```

### Related Files
- **Configuration:** `DeploymentAPI/OpenApi/OpenApiConfigurationOptions.cs`
- **Program Setup:** `DeploymentAPI/Program.cs`
- **Example Function:** `DeploymentAPI/Functions/GetAllClientsStatusFunction.cs`

---

## ✅ Next Steps

1. ✅ Start your API: `func start`
2. ✅ Open Swagger UI: http://localhost:7071/api/swagger/ui
3. ✅ Authorize with function key (if needed)
4. ✅ Test your endpoints interactively
5. ✅ Download OpenAPI spec for client generation
6. ✅ See [OPENAPI-CI-CD-INTEGRATION.md](./OPENAPI-CI-CD-INTEGRATION.md) for CI/CD setup

---

**🎉 Your API is now fully documented with OpenAPI/Swagger!**
