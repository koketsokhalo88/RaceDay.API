# RaceDay API (PROG6212 POE Part 2)

ASP.NET Core 8 Web API with Entity Framework Core (code-first, SQL Server), session-based authentication and role-based access.
Part 2 lives on the part2-api branch, in the RaceDay folder.

## Setup and run
1. Install the .NET 8 SDK, Visual Studio 2022 and SQL Server (LocalDB or SQL Express).
2. Clone the repo and switch to the part2-api branch.
3. Open RaceDay/RaceDay.sln in Visual Studio.
4. In RaceDay.Api/appsettings.json, set ConnectionStrings:DefaultConnection to match your SQL Server.
5. Set RaceDay.Api as the startup project and press F5. The database (RaceDayApiDB) is created automatically when the app starts.
6. Swagger opens in the browser at /swagger. Log in with POST /api/auth/login first; Swagger keeps the session cookie.
7. Run the unit tests: in Visual Studio open *Test Explorer* and click *Run All Tests*.

## The two roles
- *Organiser*: creates, updates and deletes their own events and categories, views enrolments for their events, and records finish times and positions.
- *Participant*: views events and categories, enrols in a category, cancels their own enrolment, views and updates their profile, and views their own enrolments and results.
- *Public (no login)*: register, log in, and view events, categories and results leaderboards.

## Authentication
Users register with their details and a role, then log in with email and password. Passwords are hashed with BCrypt and never stored in plain text.
Logging in creates a server-side session holding the user's id and role. Every protected endpoint checks the session:
401 if not logged in, 403 if the role is wrong (or the user does not own the resource).
Note: the Part 1 plan used JWT; Part 2 requires session management, so sessions were used instead.

## CI/CD
GitHub Actions builds the solution and runs all unit tests on every push.

![Green build](docs/ci-green.png)

## Demo video
[Watch on YouTube (unlisted)](PASTE-YOUR-LINK-HERE)