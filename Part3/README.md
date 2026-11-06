# Start the RaceDay website

This first Part 3 milestone lets an existing RaceDay account sign in through the MVC website, see its role-specific landing page, and sign out. Event lists, registration and banner uploads are not implemented in this milestone.

1. On the VM, open `RaceDay.sln` and run `RaceDayApi` with its **https** profile. Keep it running. The API uses your existing SQL Server database.
2. Start `RaceDayMvc` as a second project (Visual Studio: right-click the project → Debug → Start New Instance). The website runs at `https://localhost:7046`. If your API uses a different address, change `ApiBaseUrl` in the MVC project's `appsettings.json`.
3. Sign in with an existing account. With the Part 1 seed data, try `lerato@jozievents.co.za` for Organiser or `nomsa@example.co.za` for Participant, using the local demonstration password `Password123!`.
4. Check the role heading, sign out, and revisit `/Account/Dashboard`. You should be redirected to sign in. Also try a wrong password; the website should explain the problem without displaying a server error.

The login view collects the email and password. `AccountController` passes those fields to the Part 2 login endpoint, then requests `/api/profile` to learn the user's identity and role. The API session cookie is kept in that visitor's server-side MVC session. The website has no database connection or Entity Framework dependency.

For the later banner feature, the MVC site must send files to the API. Only the API will upload files to Azure Blob Storage; event details will display the saved banner.
