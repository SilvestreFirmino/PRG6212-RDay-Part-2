# RaceDay MVC website

Registration, login, role-specific navigation, event/category management, banners, entries, results and profiles are implemented.

Follow the [setup and video guide](../docs/part3-guide.md). For your existing database, run `docs/part3-upgrade.sql`, then start both API and MVC projects.

Or run `docker compose up --build -d` from the repository root and open http://localhost:8080. Docker uses a blob emulator by default; configure actual Azure storage for the final demonstration.

All database and storage operations go through the Part 2 API.
