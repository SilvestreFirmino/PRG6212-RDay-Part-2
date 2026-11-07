# RaceDay Part 3: setup and demonstration

## Run on your VM

1. Save your work, pull the latest changes and open RaceDay.sln.
2. In SSMS connect to the instance used by your API, usually `(localdb)\api_both`.
3. For your existing RaceDayDb, execute `docs/part3-upgrade.sql`. This adds BannerUrl without deleting records. Do not rerun the full create-database script.
4. For a new database only, run `docs/raceday-database.sql` instead. Do not apply the initial EF migration on top of that database.
5. Check RaceDayApi/appsettings.json's connection string. MVC does not need a database connection.
6. In Visual Studio's solution startup settings, set both RaceDayApi and RaceDayMvc to Start using HTTPS.
7. Open https://localhost:7046. Keep the API running at https://localhost:7036. If the API address changes, update ApiBaseUrl in MVC's appsettings.json.
8. Sign in with lerato@jozievents.co.za (Organiser) or nomsa@example.co.za (Participant), password `Password123!`, or register your own test account.

The home page lists upcoming published events. If empty, create a future event, add a category and publish it. Old sample events may no longer be upcoming.

## Docker

Start Docker Desktop with Linux containers. From the repository root:

```powershell
docker compose up --build -d
```

Open http://localhost:8080. This starts MVC, API, SQL Server and Azurite (a local blob emulator). It uses its own database, not your VM's LocalDB. First startup creates sample records; subsequent starts preserve records and images in named volumes.

Stop with `docker compose down`. Do not add `-v` unless you deliberately want to delete those volumes. The included sample accounts and SQL password are for local demonstrations only.

## Real Azure storage

The emulator is for testing. Your assignment demonstration needs actual Azure Blob Storage.

1. Sign in to Azure and check whether your student account has an active subscription. Check costs before creating resources.
2. Create a Storage account supporting Blob Storage. Keep anonymous blob access disabled. The API creates a private `event-banners` container at first upload.
3. Copy a connection string from Access keys. Keep this secret out of GitHub, screenshots, your video and chat.
4. In Visual Studio, right-click RaceDayApi → Manage User Secrets. Save this structure, replacing the placeholder locally:

```json
{
  "AzureStorage": {
    "ConnectionString": "YOUR PRIVATE CONNECTION STRING",
    "Container": "event-banners"
  }
}
```

5. Restart the API. As Organiser, create/edit an event and upload a PNG or JPEG of at most 5 MB.
6. Open the event details. Confirm the blob exists in Azure and BannerUrl is stored in SQL's Events table.
7. For Docker, put `AZURE_STORAGE_CONNECTION_STRING=your-private-connection-string` in a local `.env` beside compose.yaml, then run `docker compose up -d --force-recreate api`. This file is ignored by Git.

Without storage configured, local event management still works; uploading a banner displays a friendly error. Files travel MVC → API → Azure. Only the API has storage credentials and database access. Private banners are streamed back through the API and MVC.

## Test before recording

1. Register both roles in separate browsers/private windows.
2. As Organiser create a future event, add a category, open registration and publish it.
3. Upload a banner and view the details.
4. As Participant browse/filter events, enter a category and check My entries.
5. Repeat the entry: it should be rejected. Open /Organiser as Participant: access should be refused.
6. As Organiser view Entries, mark the event Completed when appropriate and record a result.
7. As Participant check My results and update your profile.
8. Sign out and revisit a protected page: it should ask you to sign in.
9. Run `dotnet test RaceDay.sln` with the .NET 6 runtime installed.
10. Check GitHub's build, clean SQL and Part 3 container workflows. The container test uses real SQL Server and Azurite, not your Azure account.

## Video: code to open in order

Begin by demonstrating the working website, then explain:

1. Part3/RaceDayMvc/Program.cs: registers MVC, sessions and HTTP communication.
2. Controllers/AccountController.cs: sends login fields to the API and remembers the user's role.
3. Services/RaceDayClient.cs: sends API requests and keeps each visitor's session separate.
4. Controllers/OrganiserController.cs and Views/Organiser/Edit.cshtml: controller sends/loads data; view displays the form.
5. Controllers/ParticipantController.cs: entries and personal results.
6. RaceDayApi/Controllers/BannersController.cs and Services/BannerStorage.cs: checks ownership/files and uploads to Azure.
7. compose.yaml and the Dockerfiles: separate services running together.
8. RaceDayApi.Tests/Part3Tests.cs: registration, session separation, access checks and banners.

Async means waiting for an API response without holding up the web server. Models hold form/display values, views are pages, and controllers connect requests to views. Database rules stay in the API.

Show actual Azure uploads, Docker running, passing tests and a green GitHub build. Record in your own voice and add the new unlisted Part 3 video to README. Existing Part 1/2 videos do not replace it.

The project retains .NET 6 for compatibility with the class project. It is out of support; upgrade before public production deployment.
