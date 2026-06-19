# OC.Automate.LinkedIn

LinkedIn integration for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate). Post content to LinkedIn as part of your automation workflows.

## Installation

```bash
dotnet add package OC.Automate.LinkedIn
```

## Prerequisites

You'll need a LinkedIn App before this package can post on your behalf.

### Step 1: Create a LinkedIn App

1. Go to [https://www.linkedin.com/developers/apps](https://www.linkedin.com/developers/apps) and sign in.
2. Click **Create app**.
3. Fill in:
   - **App name:** e.g. "My Umbraco Automate"
   - **LinkedIn Page:** Select your company page (required — create one first if you don't have one)
   - **Logo:** Upload any image
4. Accept the terms and click **Create app**.

### Step 2: Request API Access

1. On your app page, go to the **Products** tab.
2. Request access to **Share on LinkedIn** (for posting as a person) and/or **Community Management API** (for posting as an organization).
3. Wait for approval — **Share on LinkedIn** is usually instant, **Community Management API** may take a few days.

### Step 3: Get your Client ID and Client Secret

1. On your app page, go to the **Auth** tab.
2. Copy your **Client ID** and **Client Secret**.

### Step 4: Configure the Redirect URL

1. On your app's **Auth** tab, under **Authorized redirect URLs for your app**, add your callback URL:
   ```
   https://your-site.com/umbraco/api/linkedin/callback
   ```
2. This must match exactly what you configure in `appsettings.json` (see below).

### Step 5: Find your Author URN

**To post as yourself (person):**
1. After authorizing (Step 7 below), call the API: `GET https://api.linkedin.com/v2/userinfo` with your access token.
2. The response contains your `sub` field — your Author URN is `urn:li:person:{sub}`.

**To post as an organization:**
1. Go to your LinkedIn Company Page.
2. The URL looks like `https://www.linkedin.com/company/12345678/` — the number is your organization ID.
3. Your Author URN is `urn:li:organization:12345678`.

## Configuration

Add your LinkedIn app credentials to `appsettings.json`:

```json
{
  "OwainCodes": {
    "Automate": {
      "LinkedIn": {
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret",
        "AuthorizeRedirectUri": "https://your-site.com/umbraco/api/linkedin/callback"
      }
    }
  }
}
```

| Setting | Where to find it |
|---------|-----------------|
| `ClientId` | App → Auth tab → Client ID |
| `ClientSecret` | App → Auth tab → Client Secret |
| `AuthorizeRedirectUri` | Must match the redirect URL you added in Step 4 |

## Usage

### Step 6: Create a LinkedIn Connection

1. In the Umbraco backoffice, go to **Automate** and create a new **LinkedIn** connection.
2. Enter your **Author URN** (from Step 5) and a **Connection Name** (e.g. `my-linkedin`).

### Step 7: Authorize with LinkedIn

1. Navigate to: `https://your-site.com/umbraco/api/linkedin/authorize?connectionName=my-linkedin`
   (Replace `my-linkedin` with the connection name you chose in Step 6)
2. You'll be redirected to LinkedIn to authorize the app.
3. After approving, you'll see a **"LinkedIn Connected!"** confirmation page.
4. Tokens are stored automatically — no manual token management needed.

### Step 8: Validate and Use

1. Back in the Umbraco backoffice, click **Validate** on your LinkedIn connection to confirm it's working.
2. Create an automation action using **Send LinkedIn Post**.
3. Configure the post content (supports `${binding}` syntax for dynamic values like content names, URLs, etc.).

The package automatically refreshes access tokens when they expire — no manual intervention required.
