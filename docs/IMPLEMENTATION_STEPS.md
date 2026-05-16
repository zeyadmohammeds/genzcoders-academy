# ElSewedy GenZ Coders Implementation Steps

## 1. Run The Project

```powershell
cd D:\ElsewdyAcademy
dotnet restore
dotnet ef database update
dotnet run
```

Open the HTTPS URL shown in the terminal. Swagger is available at `/swagger` in development.

## 2. Configure SQL Server

The project uses SQL Server LocalDB by default:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ElSewedyGenZCoders_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

For production, change `ConnectionStrings:DefaultConnection` to your SQL Server instance and run:

```powershell
dotnet ef database update
```

## 3. Configure Google Sign-In

1. Create OAuth credentials in Google Cloud Console.
2. Add this redirect URI: `https://localhost:PORT/api/auth/google-callback`.
3. Store credentials outside source control:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

Students can start sign-in from `/api/auth/google`.

## 4. Configure Embedded Zoom

The website stores Zoom meeting metadata in `LiveSessions`. The frontend opens an embedded meeting surface at `/live.html`.

Required next step: connect Zoom Meeting SDK server-side signature generation in `LiveSessionsController.ZoomSignature`. Do not generate Zoom signatures in browser JavaScript.

```powershell
dotnet user-secrets set "Zoom:SdkKey" "YOUR_ZOOM_SDK_KEY"
dotnet user-secrets set "Zoom:SdkSecret" "YOUR_ZOOM_SDK_SECRET"
```

## 5. What Is Already Built

- ASP.NET Core API with controllers.
- SQL Server EF Core data layer.
- Identity user/role model for student, parent, engineer, CTA, school admin, academy admin.
- Google OAuth sign-in endpoints.
- Course catalog seeded from the academy documents.
- Enrollment lead endpoint with partner promo support.
- Live session endpoint with Zoom embed metadata.
- School partnership lead endpoint.
- Static polished website pages in `wwwroot`.
- Initial EF migration and SQL script.

## 6. Recommended Next Build Order

1. Finish Zoom Meeting SDK signature generation.
2. Add parent dashboard with child progress and attendance.
3. Add CTA grading workflow for assignments.
4. Add payment gateway integration for Paymob or Fawry.
5. Add notifications through WhatsApp Business API and email.
6. Add project showcase pages and referral tracking.
