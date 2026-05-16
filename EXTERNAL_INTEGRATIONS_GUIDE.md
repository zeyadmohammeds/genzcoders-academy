# External Integrations Setup Guide

This guide provides step-by-step instructions for configuring the third-party services required for the ElSewedy Academy platform.

## 1. Google OAuth (Authentication)
Used for student and staff sign-in.

1.  Go to the [Google Cloud Console](https://console.cloud.google.com/).
2.  Create a new project named **ElSewedyAcademy**.
3.  Navigate to **APIs & Services > OAuth consent screen**.
    *   Select **External**.
    *   Fill in App Name, User support email, and Developer contact info.
4.  Navigate to **Credentials > Create Credentials > OAuth client ID**.
    *   Application type: **Web application**.
    *   Authorized JavaScript origins: `https://localhost:5079` and `http://localhost:3000`.
    *   Authorized redirect URIs: `https://localhost:5079/signin-google` and `https://localhost:5079/api/auth/google-callback`.
5.  Copy the **Client ID** and **Client Secret**.
6.  Update `appsettings.json`:
    ```json
    "Authentication": {
      "Google": {
        "ClientId": "YOUR_CLIENT_ID",
        "ClientSecret": "YOUR_CLIENT_SECRET"
      }
    }
    ```

## 2. Email Service (SendGrid)
Used for OTP verification and acceptance notifications.

1.  Sign up for [SendGrid](https://sendgrid.com/).
2.  Navigate to **Settings > API Keys > Create API Key**.
3.  Give it "Full Access" and copy the key.
4.  Verify a **Sender Identity** in SendGrid.
5.  Update `appsettings.json`:
    ```json
    "Email": {
      "Host": "smtp.sendgrid.net",
      "Port": 587,
      "Username": "apikey",
      "Password": "YOUR_SENDGRID_API_KEY",
      "FromAddress": "verified-email@domain.com"
    }
    ```

## 3. SMS Service (Twilio)
Used for WhatsApp/SMS notifications.

1.  Create a [Twilio Account](https://www.twilio.com/).
2.  Get a **Phone Number** with SMS capabilities.
3.  Copy your **Account SID** and **Auth Token** from the Dashboard.
4.  Update `appsettings.json`:
    ```json
    "Twilio": {
      "AccountSid": "YOUR_SID",
      "AuthToken": "YOUR_TOKEN",
      "FromNumber": "YOUR_TWILIO_NUMBER"
    }
    ```

## 4. Zoom Meeting SDK
Used for live course rooms.

1.  Go to the [Zoom App Marketplace](https://marketplace.zoom.us/).
2.  **Develop > Build App > Meeting SDK**.
3.  Copy the **SDK Key** and **SDK Secret**.
4.  Update `appsettings.json`:
    ```json
    "Zoom": {
      "SdkKey": "YOUR_SDK_KEY",
      "SdkSecret": "YOUR_SDK_SECRET"
    }
    ```

## 5. Deployment Notes
*   **Database**: Ensure SQL Server is running. Run `dotnet ef database update` to apply migrations.
*   **CORS**: Update `AllowedOrigins` in `appsettings.json` with your production frontend URL.
*   **Redirects**: The Google Auth flow redirects to `/onboarding` by default. Ensure this route exists and is protected.

---
**ElSewedy Academy Infrastructure Team**
