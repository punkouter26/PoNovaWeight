# Quickstart: Google OAuth Setup

**Feature**: 002-google-auth  
**Date**: 2025-12-11  
**Purpose**: Step-by-step guide to configure Google OAuth credentials

## Prerequisites

- Google account with access to Google Cloud Console
- `gcloud` CLI installed (optional, for CLI-based steps)
- The app running locally at `https://localhost:5001`

---

## Option A: Google Cloud Console (UI)

### Step 1: Create or Select a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the project dropdown in the top navigation bar
3. Click **New Project**
   - Project name: `PoNovaWeight`
   - Organization: (leave as default or select your org)
4. Click **Create**
5. Wait for the project to be created, then select it

### Step 2: Configure OAuth Consent Screen

1. Navigate to **APIs & Services** → **OAuth consent screen**
2. Select **External** (unless you have a Google Workspace org)
3. Click **Create**
4. Fill in the required fields:
   - App name: `PoNovaWeight`
   - User support email: `your-email@gmail.com`
   - Developer contact email: `your-email@gmail.com`
5. Click **Save and Continue**
6. On the **Scopes** page, click **Add or Remove Scopes**
   - Select `email` and `profile` (under Google Account)
   - Click **Update**
7. Click **Save and Continue**
8. On the **Test users** page (for External/Testing mode):
   - Add your Google email as a test user
   - Click **Save and Continue**
9. Review and click **Back to Dashboard**

### Step 3: Create OAuth 2.0 Client ID

1. Navigate to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. Application type: **Web application**
4. Name: `PoNovaWeight Web App`
5. Authorized JavaScript origins:
   - `https://localhost:5001`
   - `https://ponovaweight-app.azurewebsites.net` (for production)
6. Authorized redirect URIs:
   - `https://localhost:5001/signin-google`
   - `https://ponovaweight-app.azurewebsites.net/signin-google` (for production)
7. Click **Create**
8. **Copy the Client ID and Client Secret** — you'll need these for configuration

---

## Option B: gcloud CLI (Partial)

> **Note**: OAuth consent screen configuration requires the Console UI. CLI can be used for project creation and enabling APIs.

```powershell
# Install gcloud CLI if not already installed
# https://cloud.google.com/sdk/docs/install

# Authenticate with Google Cloud
gcloud auth login

# Create a new project
gcloud projects create ponovaweight --name="PoNovaWeight"

# Set the project as active
gcloud config set project ponovaweight

# Enable the required APIs
gcloud services enable oauth2.googleapis.com
gcloud services enable cloudresourcemanager.googleapis.com

# After configuring OAuth consent screen in Console UI, you can list credentials
gcloud alpha iap oauth-clients list projects/ponovaweight/brands/ponovaweight
```

After running these commands, complete Steps 2-3 in the Console UI.

---

## Configure the Application

### Development (appsettings.Development.json)

Add your Google credentials to `src/PoNovaWeight.Api/appsettings.Development.json`:

```json
{
  "Google": {
    "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

> ⚠️ **Important**: Do NOT commit secrets to source control. The `appsettings.Development.json` file should be in `.gitignore`.

### Production (Azure Key Vault)

For production, store secrets in Azure Key Vault:

```powershell
# Set Azure subscription
az account set --subscription "Your-Subscription-Name"

# Create Key Vault (if not exists)
az keyvault create --name ponovaweight-kv --resource-group PoNovaWeight-rg --location eastus

# Store secrets
az keyvault secret set --vault-name ponovaweight-kv --name "Google--ClientId" --value "YOUR_CLIENT_ID"
az keyvault secret set --vault-name ponovaweight-kv --name "Google--ClientSecret" --value "YOUR_CLIENT_SECRET"
```

The `Program.cs` will be configured to read from Key Vault when `ASPNETCORE_ENVIRONMENT=Production`.

---

## Verify Setup

### 1. Start the Application

```powershell
cd src/PoNovaWeight.Api
dotnet run
```

### 2. Test the OAuth Flow

1. Open `https://localhost:5001` in your browser
2. Click "Sign in with Google"
3. You should be redirected to Google's consent screen
4. After approval, you should be redirected back to the app and see your user info

### 3. Check the Users Table

```powershell
# Using Azure Storage Explorer or Azurite
# Open the "Users" table and verify your email appears as a PartitionKey
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "redirect_uri_mismatch" error | Verify the redirect URI in Google Console exactly matches `https://localhost:5001/signin-google` |
| "Access blocked: app not verified" | Add your email as a test user in OAuth consent screen |
| Cookie not persisting | Ensure HTTPS is used (`https://localhost:5001`, not `http://`) |
| "invalid_client" error | Check that Client ID and Secret are correctly copied (no extra spaces) |

---

## Publishing OAuth Consent Screen (Pre-Production)

Before deploying to production with real users:

1. Go to **OAuth consent screen** in Google Cloud Console
2. Click **Publish App**
3. Complete the verification process (may take days/weeks)
4. Alternatively, keep in "Testing" mode with specific test user emails added

For MVP/personal use, "Testing" mode with added test users is sufficient.
