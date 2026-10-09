# RaceDay Part 2 API guide

This API is written as a beginner-friendly Entity Framework Core project. It uses the same ideas as the earlier `SqlConnection` examples, but Entity Framework Core creates and reads database records without writing SQL strings inside every controller.

## Important folders

| Folder | What it does |
|---|---|
| `Models` | Classes that become database tables, such as `User`, `RaceEvent`, and `Category`. |
| `Data` | Contains `RaceDayDbContext`, which is the connection between C# classes and SQL Server. |
| `Controllers` | Contains the API methods. Each method handles a route such as login, creating an event, or entering an event. |
| `DTOs` | Small classes that describe the data sent to an API endpoint. |
| `Services` | Contains `PasswordService`, which hashes passwords before they are saved. |
| `RaceDayApi.Tests` | Unit tests for registration, login, organiser access, and enrolment. |

## Session login in simple terms

When a user logs in, `AuthController` stores two values in the session:

```csharp
HttpContext.Session.SetInt32("UserId", user.UserId);
HttpContext.Session.SetString("Role", user.Role);
```

Other controllers use these values to decide what the user may do. For example, an event can be created only when `IsOrganiser()` returns `true`.

## Typical API flow

1. Register an Organiser or Participant account using `POST /api/auth/register`.
2. Log in using `POST /api/auth/login`.
3. An Organiser creates an event and categories.
4. A Participant enters an event by selecting a category.
5. The Organiser records results after the event is completed.
