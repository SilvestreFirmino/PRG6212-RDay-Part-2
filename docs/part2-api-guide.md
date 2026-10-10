# RaceDay Part 2 API guide

This API is written as a beginner-friendly Entity Framework Core project. It uses the same ideas as the earlier `SqlConnection` examples, but Entity Framework Core creates and reads database records without writing SQL strings inside every controller.

## Important folders

| Folder | What it does |
|---|---|
| `Models` | Classes that become database tables, such as `User`, `RaceEvent`, and `Category`. The same simple classes are also used as the input when creating or updating records. |
| `Data` | Contains `RaceDayDbContext`, which is the connection between C# classes and SQL Server. |
| `Controllers` | Contains the API methods. Each method handles a route such as login, creating an event, or entering an event. |
| `RequestModels.cs` | Contains only `LoginDetails` and `StatusChange`, because login and a status change are not complete database records. |
| `Services` | Contains `PasswordService`, which hashes passwords before they are saved. |
| `RaceDayApi.Tests` | Unit tests for registration, login, organiser access, and enrolment. |

## Session login in simple terms

When a user logs in, `AuthController` stores two values in the session:

```csharp
HttpContext.Session.SetInt32("UserId", user.UserId);
HttpContext.Session.SetString("Role", user.Role);
```

Other controllers use these values to decide what the user may do. For example, an event can be created only when `IsOrganiser()` returns `true`.

## Why the code is simple

Most controller methods follow the same short pattern:

1. Check that the logged-in role is allowed to do the action.
2. Find the record with Entity Framework Core.
3. Check that the record belongs to the user where needed.
4. Add, change, or remove the record.
5. Save the change and return a clear message.

Entity Framework Core is required for this part of the assignment. It replaces handwritten SQL such as `INSERT` and `SELECT` with readable C# commands such as `_db.Events.Add(raceEvent)` and `await _db.SaveChangesAsync()`.

## Typical API flow

1. Register an Organiser or Participant account using `POST /api/auth/register`.
2. Log in using `POST /api/auth/login`.
3. An Organiser creates an event and categories.
4. A Participant enters an event by selecting a category.
5. The Organiser records results after the event is completed.
