# Understanding the simpler version

Practise these steps before recording. Explain them in your own words while showing what the program actually does.

## What changed

Each controller now receives individual inputs, like the lecturer's example: email, password, name, categoryId and so on. There are no Consumes attributes or separate request/form classes. The database models remain because Entity Framework Core needs them.

## Read a method from top to bottom

1. The route and HttpPost/HttpGet/HttpPut/HttpDelete say which URL and action the method handles.
2. The parameters are the values the person types. FromForm means read a value from a form field. Route IDs already have their own Swagger input boxes.
3. The if statements check the input or the logged-in role.
4. For a new record, new User or new RaceEvent creates an object and puts the entered values into its properties.
5. _db.Users.Add(user) prepares the new user to be inserted. _db.SaveChanges() writes it to SQL Server.
6. return Ok sends a success message; BadRequest rejects invalid input; Unauthorized rejects a login/access attempt; NotFound means the requested record was not found.

## Small terms you need to understand

- Required: a field cannot be missing or blank.
- MaxLength and MinLength: limits on the length of entered text.
- EmailAddress: checks the shape of an email address.
- string? or DateTime?: a value may be missing. = null makes it optional in the method.
- RegistrationRole: an enum, meaning a list of allowed choices. It gives Swagger the Participant/Organiser dropdown. These two roles are fixed because the assignment defines them; accounts and events still come from SQL Server.
- IActionResult: lets a method return different HTTP responses, such as 200 or 400.
- FirstOrDefault(u => u.Email == email): finds the first user whose email matches the entered email, or returns null if none exists.
- _db: the database context. Visual Studio's startup code supplies it through the controller constructor. The constructor stores it so the methods can use it.
- PasswordService: hashes passwords on registration and checks the entered password on login. Do not replace it with plain-text password storage.
- Session: remembers the UserId and Role after a successful login. Logout clears them.

## Files to show in the video

1. appsettings.json: the SQL Server name and database name.
2. Program.cs: registers EF Core, the database connection, password service, sessions and Swagger.
3. Models/User.cs: the properties of a user record.
4. Data/RaceDayDbContext.cs: the tables and relationships EF Core uses. Part 2 specifies EF Core Code First, so this stays in the project.
5. Controllers/AuthController.cs: explain Register, Login and Logout using the steps above.
6. Controllers/EventsController.cs: show the role check and how an event is saved.
7. RaceDayApi.Tests: show the tests, run them and explain an expected success and rejection.

## Demonstration order

1. Start the API and open Swagger.
2. Register an Organiser using the input fields and role dropdown. Use your own test email and password.
3. Login with that email and password. Explain that the session now remembers the account.
4. Create an event and show its saved record in SSMS.
5. Logout, register a Participant and login with that account. Participant birth date, gender and emergency contact details are required by your existing database design. Phone number, club and medical notes are optional profile details, not registration fields.
6. Attempt to create an event as the Participant and show that access is rejected.
7. Use a published event with an open registration period and available category to demonstrate enrolment.
8. Show the remaining required features, tests and the real successful GitHub Actions run.

## Installing these files

Close the running API first. The easiest option is to open the solution from the new full ZIP. It does not contain your old duplicated controller files.

If updating your existing project, replace each controller with the matching filename; do not paste below the old code. Replace Program.cs and the two test files. Add Models/RegistrationRole.cs. Remove the previous Models/InputForms.cs, Models/RegistrationDetails.cs and Models/RequestModels.cs from this project: the simpler version no longer uses them, and RegistrationDetails.cs would duplicate the enum. Keep your database models, DbContext, migrations, password service, connection settings and SQL database.

No database changes are required for this simplification. The new API uses form requests instead of JSON bodies for input. Use Swagger's Try it out fields. The local checks use an in-memory test database; confirm successful saves against your SQL Server on the VM before recording.
