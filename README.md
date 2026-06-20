# OC.Automate.LinkedIn

LinkedIn integration for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate). Post content to LinkedIn as part of your automation workflows.

## Installation

```bash
dotnet add package OC.Automate.LinkedIn
```

## Setup

### Step 1: Create a LinkedIn App

1. Go to [https://www.linkedin.com/developers/apps](https://www.linkedin.com/developers/apps) and sign in.
2. Click **Create app**.
3. Fill in:
   - **App name:** e.g. "My Umbraco Automate"
   - **LinkedIn Page:** Select your company page (required — create one first if you don't have one)
   - **Logo:** Upload any image
4. Accept the terms and click **Create app**.

### Step 2: Enable Required Products

On your app page, go to the **Products** tab and request access to:

| Product | Purpose | Approval |
|---------|---------|----------|
| **Share on LinkedIn** | Required for posting as a person | Usually instant |
| **Sign In with LinkedIn using OpenID Connect** | Required for automatic Author URN retrieval | Usually instant |
| **Community Management API** | Only needed if posting as an organization | May take a few days |

### Step 3: Get your Client ID and Client Secret

1. On your app page, go to the **Auth** tab.
2. Copy your **Client ID** and **Client Secret** — you'll need these for `appsettings.json`.

### Step 4: Configure the Redirect URL in LinkedIn

1. On your app's **Auth** tab, scroll down to **Authorized redirect URLs for your app**.
2. Click **Add redirect URL** and enter:
   ```
   https://your-site.com/umbraco/api/linkedin/callback
   ```
3. Click **Update** to save.

> **Important:** This URL must match *exactly* what you set in `appsettings.json` below — including the protocol (`https`), domain, and path.

### Step 5: Configure appsettings.json

Add the following to your Umbraco site's `appsettings.json`:

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

| Setting | Description | Where to find it |
|---------|-------------|-----------------|
| `ClientId` | Your LinkedIn app's Client ID | App → Auth tab → Client ID |
| `ClientSecret` | Your LinkedIn app's Client Secret | App → Auth tab → Client Secret |
| `AuthorizeRedirectUri` | The callback URL for OAuth authorization | Must match the redirect URL you added in Step 4 |

## Usage

### Step 6: Create a LinkedIn Connection

1. In the Umbraco backoffice, go to **Automate** and create a new **LinkedIn** connection.
2. Enter a **Connection Name** (e.g. `LinkedIn`). This is used internally to store your authorization tokens.

### Step 7: Authorize with LinkedIn

1. Click the **"Authorize with LinkedIn"** button in the connection settings.
2. A new window opens and redirects you to LinkedIn — sign in and approve the permissions.
3. After approving, you'll see a **"LinkedIn Connected!"** confirmation page showing your **Author URN** (e.g. `urn:li:person:abc123`).
4. Your Author URN and access tokens are saved automatically. Close the window and return to the backoffice.

### Step 8: Validate and Use

1. Back in the Umbraco backoffice, click **Validate** on your LinkedIn connection to confirm it's working.
2. Create an automation and add the **Send LinkedIn Post** action.
3. Configure the post:
   - **Content** — The text of your post. Supports `${binding}` syntax for dynamic values (e.g. content name, URL).
   - **Post URL** (optional) — A URL to append to the post.
   - **Visibility** — `PUBLIC` (default) or `CONNECTIONS` (visible only to your connections).

## Token Management

- Access tokens are stored in the Umbraco database and managed automatically.
- When an access token expires (~60 days), you will need to re-authorize by clicking the **Authorize with LinkedIn** button again.
- No manual token handling is required.

## Posting as an Organization

To post as a LinkedIn Company Page instead of a personal profile:

1. Ensure **Community Management API** is enabled on your LinkedIn app (Step 2).
2. After authorizing, the package stores your personal Author URN automatically.
3. To post as an organization, you'll need to update the stored URN manually via the `/umbraco/api/linkedin/me` endpoint or by re-configuring the connection.

> **Note:** Organization posting requires additional LinkedIn app approval and the account authorizing must be an admin of the LinkedIn Company Page.
