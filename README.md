# GitHub OAuth Setup for Astraea

Astraea supports GitHub OAuth so learners can connect their real GitHub accounts and sync repository activity into their skill practice signals.

For security, GitHub OAuth secrets are not committed to the repository. Each developer or evaluator should create their own GitHub OAuth App and store the keys locally using .NET User Secrets.

## 1. Create a GitHub OAuth App

1. Go to GitHub.

2. Click your profile picture in the top-right corner.

3. Go to:

   ```text
   Settings -> Developer settings -> OAuth Apps
   ```

4. Click:

   ```text
   New OAuth App
   ```

   If this is your first OAuth app, GitHub may show:

   ```text
   Register a new application
   ```

5. Fill in the form:

   ```text
   Application name:
   Astraea Local Development

   Homepage URL:
   http://localhost:5000

   Application description:
   Celestial skill and retention tracker for self-taught learners.

   Authorization callback URL:
   http://localhost:5000/api/github/oauth/callback
   ```

   Important: the callback URL must match the port where the app is running.

   If the app is running on port `5000`, use:

   ```text
   http://localhost:5000/api/github/oauth/callback
   ```

   If the app is running on another port, replace `5000` with that port.

6. Leave these unchecked unless you specifically need them:

   ```text
   Allow wildcard matching
   Enable Device Flow
   ```

7. Click:

   ```text
   Register application
   ```

## 2. Copy the Client ID and Client Secret

After registering the OAuth app:

1. Copy the generated `Client ID`.

2. Click:

   ```text
   Generate a new client secret
   ```

3. Copy the generated `Client Secret`.

Do not commit the Client Secret to GitHub.

## 3. Store the OAuth keys locally with .NET User Secrets

From the solution folder, run:

```powershell
cd Astraea.Web
dotnet user-secrets init
dotnet user-secrets set "GitHub:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "GitHub:ClientSecret" "YOUR_CLIENT_SECRET"
```

Replace:

```text
YOUR_CLIENT_ID
YOUR_CLIENT_SECRET
```

with the values from your GitHub OAuth App.


## 4. Run the application

Start the app:

```powershell
dotnet run --project Astraea.Web
```

Open the app in the browser:

```text
http://localhost:5000
```

Then log in as a learner and go to the GitHub Integration page.

Click:

```text
Connect with GitHub
```

GitHub should ask you to authorize Astraea. After authorization, you will be redirected back to Astraea and the GitHub connection status should show as connected.

## 5. Troubleshooting

If you see:

```text
GitHub OAuth keys are missing in appsettings.
```

then the Client ID or Client Secret was not found. Re-run:

```powershell
cd Astraea.Web
dotnet user-secrets set "GitHub:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "GitHub:ClientSecret" "YOUR_CLIENT_SECRET"
```

If GitHub shows:

```text
The redirect_uri is not associated with this application.
```

then the callback URL in GitHub does not match the app callback URL.

Make sure the GitHub OAuth App callback is exactly:

```text
http://localhost:5000/api/github/oauth/callback
```

or replace `5000` with the port your app is actually using.
