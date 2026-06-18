# OC.Automate.LinkedIn

LinkedIn integration for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate). Post content to LinkedIn as part of your automation workflows.

## Installation

```bash
dotnet add package OC.Automate.LinkedIn
```

## Prerequisites

You'll need a LinkedIn App with the right permissions before this package can post on your behalf. Follow these steps:

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
2. Copy your **Client ID** and **Client Secret** — you'll need these for your `appsettings.json`.

### Step 4: Generate an Access Token

The simplest way to get a token for testing:

1. On your app's **Auth** tab, scroll down to **OAuth 2.0 tools**.
2. Click **Create token** (or use the LinkedIn Token Generator).
3. Select the scopes: **w_member_social** (post as yourself) or **w_organization_social** (post as an organization).
4. Complete the authorization flow — LinkedIn will show you an access token.
5. Copy the token.

> **Warning:** Access tokens expire (typically after 60 days). For production use, you'll need to implement a token refresh flow outside of this package, or manually rotate tokens when they expire.

### Step 5: Find your Author URN

**To post as yourself (person):**
1. Use the LinkedIn Token Generator or call the API: `GET https://api.linkedin.com/v2/userinfo` with your access token.
2. The response contains your `sub` field — your Author URN is `urn:li:person:{sub}`.

**To post as an organization:**
1. Go to your LinkedIn Company Page.
2. The URL looks like `https://www.linkedin.com/company/12345678/` — the number is your organization ID.
3. Your Author URN is `urn:li:organization:12345678`.

## Configuration

Add your LinkedIn credentials to `appsettings.json`:

```json
{
  "OwainCodes": {
    "Automate": {
      "LinkedIn": {
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret",
        "AccessTokens": {
          "my-linkedin": "your-access-token"
        }
      }
    }
  }
}
```

| Setting | Where to find it |
|---------|-----------------|
| `ClientId` | App → Auth tab → Client ID |
| `ClientSecret` | App → Auth tab → Client Secret |
| `AccessTokens` key | A name you choose (e.g. `"my-linkedin"`) — you'll enter this same name in the Umbraco connection setup |
| `AccessTokens` value | The OAuth2 access token from Step 4 |

## Usage

1. In the Umbraco backoffice, go to **Automate** and create a new **LinkedIn** connection.
2. Enter your **Author URN** (from Step 5 above) and **Connection Name** (must match the key you used in `AccessTokens`).
3. Click **Validate** to confirm the token is working.
4. Create an automation action using **Send LinkedIn Post**.
5. Configure the post content (supports `${binding}` syntax for dynamic values like content names, URLs, etc.).
