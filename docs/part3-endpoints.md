# Part 3 additions to the API plan

The original endpoint plan remains the Part 1 specification. These additions support the MVC pages and required Azure banners. Existing event creation now returns `{ eventId, message }` so the website can upload a banner after creation. GET /api/events accepts optional city, eventType and upcoming filters.

| Method | Route | Purpose | Role | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/banner | Upload event banner to private Blob Storage | Organiser owning event | Multipart `file`, PNG/JPEG, max 5 MB | 200 saved; 400 invalid image; 401/403 denied; 404 event missing; 503 storage unavailable |
| GET | /api/events/{eventId}/banner | Stream saved banner | Public for Published/Completed; otherwise owner | None | 200 image; 404 missing/private/unavailable |
| GET | /api/portal/organiser | Own events with enrolment counts | Organiser | None | 200 list; 401/403 denied |
| GET | /api/portal/enrolments | Own entries with event/category names | Participant | None | 200 list; 401/403 denied |
| GET | /api/portal/results | Own results with event/category names | Participant | None | 200 list; 401/403 denied |
| GET | /api/portal/events/{eventId}/enrolments | Event roster with participant names and results | Organiser owning event | None | 200 list; 401/403 denied; 404 event missing/not owned |

The portal routes return display data from the API, avoiding database joins or business rules inside MVC. Session cookies remain isolated per website visitor. Unsafe MVC requests require antiforgery tokens.
