# OpenAPI CI/CD Integration Guide

## 🎯 Overview

This guide shows you how to integrate OpenAPI specification into your CI/CD pipeline for:
- Automated client code generation
- API versioning and change detection
- Automated documentation deployment
- API contract testing

---

## 📋 Table of Contents

1. [Export OpenAPI Spec in CI/CD](#export-openapi-spec-in-cicd)
2. [Automated Client Generation](#automated-client-generation)
3. [API Version Control](#api-version-control)
4. [Breaking Change Detection](#breaking-change-detection)
5. [Documentation Deployment](#documentation-deployment)
6. [GitHub Actions Examples](#github-actions-examples)

---

## 📤 Export OpenAPI Spec in CI/CD

### Option 1: Build-Time Export (Recommended)

Add to your GitHub Actions workflow:

```yaml
name: Export OpenAPI Spec

on:
  push:
	branches: [ develop, main ]
  pull_request:
	branches: [ develop, main ]

jobs:
  export-openapi:
	runs-on: ubuntu-latest

	steps:
	- uses: actions/checkout@v3

	- name: Setup .NET
	  uses: actions/setup-dotnet@v3
	  with:
		dotnet-version: '8.0.x'

	- name: Restore dependencies
	  run: dotnet restore DeploymentAPI/DeploymentAPI.csproj

	- name: Build project
	  run: dotnet build DeploymentAPI/DeploymentAPI.csproj --configuration Release --no-restore

	- name: Start Functions Host
	  run: |
		cd DeploymentAPI
		func start &
		sleep 10

	- name: Download OpenAPI Spec
	  run: |
		curl http://localhost:7071/api/swagger.json > openapi.json
		curl http://localhost:7071/api/swagger.yaml > openapi.yaml

	- name: Upload OpenAPI Artifacts
	  uses: actions/upload-artifact@v3
	  with:
		name: openapi-spec
		path: |
		  openapi.json
		  openapi.yaml
```

### Option 2: Post-Deployment Export

After deploying to Azure:

```yaml
- name: Download OpenAPI from Azure
  run: |
	FUNC_KEY="${{ secrets.AZURE_FUNCTION_KEY }}"
	curl "https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json?code=$FUNC_KEY" > openapi.json
```

---

## 🔄 Automated Client Generation

### Generate TypeScript Client

**Using OpenAPI Generator:**

```yaml
- name: Generate TypeScript Client
  run: |
	npx @openapitools/openapi-generator-cli generate \
	  -i openapi.json \
	  -g typescript-fetch \
	  -o ./generated-clients/typescript \
	  --additional-properties=typescriptThreePlus=true

- name: Publish TypeScript Client
  uses: actions/upload-artifact@v3
  with:
	name: typescript-client
	path: ./generated-clients/typescript
```

### Generate C# Client

```yaml
- name: Generate C# Client
  run: |
	dotnet tool install --global NSwag.ConsoleCore
	nswag openapi2csclient \
	  /input:openapi.json \
	  /classname:DeploymentApiClient \
	  /namespace:DeploymentAPI.Client \
	  /output:DeploymentApiClient.cs
```

### Generate Python Client

```yaml
- name: Generate Python Client
  run: |
	pip install openapi-generator-cli
	openapi-generator-cli generate \
	  -i openapi.json \
	  -g python \
	  -o ./generated-clients/python \
	  --package-name deployment_api_client
```

---

## 📌 API Version Control

### Track API Changes in Git

```yaml
- name: Commit OpenAPI Spec
  run: |
	git config user.name "GitHub Actions"
	git config user.email "actions@github.com"
	git add openapi.json openapi.yaml
	git commit -m "Update OpenAPI specification [skip ci]" || echo "No changes"
	git push
```

### Version Tagging

```yaml
- name: Tag API Version
  if: github.ref == 'refs/heads/main'
  run: |
	API_VERSION=$(jq -r '.info.version' openapi.json)
	git tag "api-v${API_VERSION}"
	git push origin "api-v${API_VERSION}"
```

---

## 🔍 Breaking Change Detection

### Using OpenAPI Diff

```yaml
- name: Check for Breaking Changes
  run: |
	npm install -g openapi-diff

	# Download previous version
	curl https://raw.githubusercontent.com/${{ github.repository }}/main/openapi.json > openapi-previous.json

	# Compare versions
	openapi-diff openapi-previous.json openapi.json --breaking-only > breaking-changes.txt

	if [ -s breaking-changes.txt ]; then
	  echo "::warning::Breaking API changes detected!"
	  cat breaking-changes.txt
	  exit 1
	fi
```

### Using Spectral for Linting

```yaml
- name: Lint OpenAPI Spec
  run: |
	npm install -g @stoplight/spectral-cli
	spectral lint openapi.json --ruleset spectral.yaml
```

**Example `spectral.yaml`:**

```yaml
extends: spectral:oas
rules:
  operation-operationId: error
  operation-description: warn
  operation-tags: error
```

---

## 📖 Documentation Deployment

### Deploy to GitHub Pages

```yaml
- name: Generate Redoc Documentation
  run: |
	npx redoc-cli bundle openapi.json -o documentation.html

- name: Deploy to GitHub Pages
  uses: peaceiris/actions-gh-pages@v3
  with:
	github_token: ${{ secrets.GITHUB_TOKEN }}
	publish_dir: ./
	publish_branch: gh-pages
```

### Deploy to Azure Static Web Apps

```yaml
- name: Deploy API Docs to Azure
  uses: Azure/static-web-apps-deploy@v1
  with:
	azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
	repo_token: ${{ secrets.GITHUB_TOKEN }}
	action: "upload"
	app_location: "/documentation"
```

---

## 🔧 GitHub Actions Examples

### Complete CI/CD Workflow

Create `.github/workflows/api-openapi.yml`:

```yaml
name: OpenAPI CI/CD

on:
  push:
	branches: [ develop, main ]
  pull_request:
	branches: [ develop, main ]

jobs:
  openapi-workflow:
	runs-on: ubuntu-latest

	steps:
	- name: Checkout code
	  uses: actions/checkout@v3
	  with:
		fetch-depth: 0  # Full history for version comparison

	- name: Setup .NET 8
	  uses: actions/setup-dotnet@v3
	  with:
		dotnet-version: '8.0.x'

	- name: Setup Azure Functions Core Tools
	  run: |
		wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
		sudo dpkg -i packages-microsoft-prod.deb
		sudo apt-get update
		sudo apt-get install azure-functions-core-tools-4

	- name: Restore and Build
	  run: |
		dotnet restore DeploymentAPI/DeploymentAPI.csproj
		dotnet build DeploymentAPI/DeploymentAPI.csproj --configuration Release

	- name: Start Functions and Export OpenAPI
	  run: |
		cd DeploymentAPI
		func start &
		FUNC_PID=$!
		sleep 15

		# Download specs
		curl http://localhost:7071/api/swagger.json > ../openapi.json
		curl http://localhost:7071/api/swagger.yaml > ../openapi.yaml

		# Stop function host
		kill $FUNC_PID
		cd ..

	- name: Validate OpenAPI Spec
	  run: |
		npm install -g @stoplight/spectral-cli
		spectral lint openapi.json

	- name: Check for Breaking Changes
	  if: github.event_name == 'pull_request'
	  run: |
		# Download main branch version
		git show origin/main:openapi.json > openapi-main.json || echo "{}" > openapi-main.json

		# Install and run openapi-diff
		npm install -g openapi-diff
		openapi-diff openapi-main.json openapi.json > api-changes.txt || true

		echo "## API Changes" >> $GITHUB_STEP_SUMMARY
		cat api-changes.txt >> $GITHUB_STEP_SUMMARY

	- name: Generate TypeScript Client
	  run: |
		npx @openapitools/openapi-generator-cli generate \
		  -i openapi.json \
		  -g typescript-fetch \
		  -o ./clients/typescript

	- name: Generate C# Client
	  run: |
		dotnet tool install --global NSwag.ConsoleCore
		nswag openapi2csclient \
		  /input:openapi.json \
		  /classname:DeploymentApiClient \
		  /namespace:DeploymentAPI.Client \
		  /output:./clients/csharp/DeploymentApiClient.cs

	- name: Upload Artifacts
	  uses: actions/upload-artifact@v3
	  with:
		name: openapi-and-clients
		path: |
		  openapi.json
		  openapi.yaml
		  clients/

	- name: Commit OpenAPI Spec (main branch only)
	  if: github.ref == 'refs/heads/main'
	  run: |
		git config user.name "GitHub Actions"
		git config user.email "actions@github.com"
		git add openapi.json openapi.yaml
		git commit -m "chore: update OpenAPI specification [skip ci]" || echo "No changes"
		git push

	- name: Create API Docs
	  run: |
		npx redoc-cli bundle openapi.json -o api-documentation.html

	- name: Deploy Documentation
	  if: github.ref == 'refs/heads/main'
	  uses: peaceiris/actions-gh-pages@v3
	  with:
		github_token: ${{ secrets.GITHUB_TOKEN }}
		publish_dir: ./
		destination_dir: api-docs
```

---

## 📦 Package and Publish Clients

### Publish TypeScript Client to npm

```yaml
- name: Publish TypeScript Client to npm
  working-directory: ./clients/typescript
  run: |
	npm version ${{ github.event.release.tag_name }}
	echo "//registry.npmjs.org/:_authToken=${{ secrets.NPM_TOKEN }}" > .npmrc
	npm publish
```

### Publish C# Client to NuGet

```yaml
- name: Package C# Client
  run: |
	dotnet pack ./clients/csharp/DeploymentAPI.Client.csproj \
	  -c Release \
	  -o ./packages

- name: Publish to NuGet
  run: |
	dotnet nuget push ./packages/*.nupkg \
	  --api-key ${{ secrets.NUGET_API_KEY }} \
	  --source https://api.nuget.org/v3/index.json
```

---

## 🧪 API Contract Testing

### Using Dredd

```yaml
- name: API Contract Testing with Dredd
  run: |
	npm install -g dredd

	# Start API
	cd DeploymentAPI
	func start &
	sleep 10

	# Run contract tests
	dredd openapi.yaml http://localhost:7071
```

### Using Postman/Newman

```yaml
- name: Generate Postman Collection
  run: |
	npx openapi-to-postmanv2 \
	  -s openapi.json \
	  -o postman-collection.json

- name: Run Postman Tests
  run: |
	npm install -g newman
	newman run postman-collection.json \
	  --environment postman-environment.json
```

---

## ✅ Best Practices

### 1. Version Your API
- Update version in `OpenApiConfigurationOptions.cs`
- Tag releases with API version
- Maintain changelog for API changes

### 2. Automate Client Generation
- Generate clients on every merge to main
- Publish to package registries
- Version clients alongside API

### 3. Validate Before Merge
- Check for breaking changes in PRs
- Lint OpenAPI spec
- Run contract tests

### 4. Document Changes
- Use commit messages for API changes
- Maintain API changelog
- Update documentation automatically

### 5. Monitor API Usage
- Track which endpoints are used
- Monitor deprecated endpoint usage
- Plan breaking changes carefully

---

## 📚 Tools and Resources

### OpenAPI Tools
- **[OpenAPI Generator](https://openapi-generator.tech/)** - Generate clients in 40+ languages
- **[NSwag](https://github.com/RicoSuter/NSwag)** - .NET OpenAPI toolchain
- **[Redoc](https://github.com/Redocly/redoc)** - Beautiful API documentation
- **[Spectral](https://stoplight.io/open-source/spectral)** - OpenAPI linting
- **[openapi-diff](https://github.com/OpenAPITools/openapi-diff)** - Compare OpenAPI specs

### CI/CD Actions
- **[setup-dotnet](https://github.com/actions/setup-dotnet)** - Setup .NET SDK
- **[upload-artifact](https://github.com/actions/upload-artifact)** - Upload build artifacts
- **[gh-pages](https://github.com/peaceiris/actions-gh-pages)** - Deploy to GitHub Pages

---

## 🎯 Example Use Cases

### Use Case 1: Dashboard Client Generation

```yaml
# Generate TypeScript client for dashboard
- name: Generate Dashboard API Client
  run: |
	npx @openapitools/openapi-generator-cli generate \
	  -i openapi.json \
	  -g typescript-fetch \
	  -o ../DeploymentDashboard/generated-api \
	  --additional-properties=supportsES6=true

# Update dashboard to use generated client
- name: Update Dashboard
  run: |
	cd DeploymentDashboard
	npm install
	# Import generated client in your code
```

### Use Case 2: Multi-Environment Testing

```yaml
# Test against different environments
- name: Test Dev Environment
  run: |
	curl "https://func-dev.azurewebsites.net/api/swagger.json" > openapi-dev.json
	dredd openapi-dev.json https://func-dev.azurewebsites.net

- name: Test Prod Environment
  run: |
	curl "https://func-prod.azurewebsites.net/api/swagger.json" > openapi-prod.json
	dredd openapi-prod.json https://func-prod.azurewebsites.net
```

---

## 🚀 Next Steps

1. ✅ Add OpenAPI workflow to `.github/workflows/`
2. ✅ Configure secrets (NPM_TOKEN, NUGET_API_KEY, etc.)
3. ✅ Test the workflow on a feature branch
4. ✅ Set up automated client generation
5. ✅ Configure breaking change detection
6. ✅ Deploy documentation to GitHub Pages

---

**🎉 Your API is now fully integrated with CI/CD!**
