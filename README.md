# Assignment & Submission Management System

A full-stack academic management application built with React, TypeScript, ASP.NET Core, and MongoDB. The current implementation delivers the administrator workspace from the recruitment brief: secure admin login, teacher/student account management, course and subject management, teacher assignment, and system-wide assignment/submission oversight.

## Implemented features

### Administrator portal

- JWT login with backend role authorization
- Dashboard totals for people, catalog records, assignments, submissions, and pending reviews
- Create, search, update, activate/deactivate, and delete teacher/student accounts
- Create, search, update, activate/deactivate, and safely delete classes/courses
- Create and manage subjects within courses
- Assign or reassign a teacher to a subject
- Filter and view assignments across all courses
- Filter and view all student submissions, answers, marks, and feedback
- Responsive desktop/mobile navigation and forms

### Backend safeguards

- PBKDF2-SHA256 password hashing with per-password salts
- Admin-only authorization on every `/api/admin/*` endpoint
- Request validation and RFC 7807 problem responses
- Unique MongoDB indexes for user emails, course codes, and subject codes within a course
- Referential checks that prevent deleting records still used by academic data
- Automatic initial administrator creation
- Automatic loading of `Backend/.env` for local development

## Technology stack

- Frontend: React 19, TypeScript, Vite, responsive CSS
- Backend: ASP.NET Core 9 Web API, JWT bearer authentication, OpenAPI
- Database: MongoDB Atlas with the official MongoDB .NET driver

## Project structure

```text
Backend/
  Configuration/   Typed application settings
  Contracts/       API request and response models
  Controllers/     Authentication and admin REST endpoints
  Data/            MongoDB collections, indexes, and seed logic
  Models/          MongoDB documents and domain enums
  Services/        Password hashing and token creation
Frontend/
  src/components/  Reusable admin layout, icons, and modal
  src/lib/         API client
  src/pages/       Admin dashboard and management screens
```

## Local setup

### Prerequisites

- .NET 9 SDK
- Node.js 20 or newer
- A MongoDB Atlas deployment (or another compatible MongoDB connection string)

### 1. Configure MongoDB and authentication

Create an Atlas database user, add your current IP address under Atlas Network Access, then create the local environment file:

```bash
cp Backend/.env.example Backend/.env
```

Update `Backend/.env` with your connection details and a random JWT secret of at least 32 characters:

```dotenv
MongoDb__ConnectionString=mongodb+srv://<username>:<password>@<cluster-host>/?retryWrites=true&w=majority
MongoDb__DatabaseName=assignment_submission_db
Jwt__Secret=<random-secret-at-least-32-characters>
Seed__AdminEmail=admin@example.com
Seed__AdminPassword=Admin123!
```

If the MongoDB password contains reserved URL characters such as `@`, `:`, `/`, or `#`, URL-encode the password before placing it in the connection string. The backend connects during startup so invalid credentials fail immediately instead of failing on the first API request.

### 2. Run the backend

```bash
dotnet restore Backend/Backend.csproj
dotnet run --project Backend/Backend.csproj
```

The API runs at `http://localhost:5080`. In development, the OpenAPI document is available at `http://localhost:5080/openapi/v1.json`.

### 3. Run the frontend

In another terminal:

```bash
cd Frontend
npm install
cp .env.example .env.local
npm run dev
```

Open `http://localhost:5173`.

### Development administrator

```text
Email:    admin@example.com
Password: Admin123!
```

These are development defaults. Change them through environment variables before deploying. The seed process only creates the administrator when that email does not already exist; it does not overwrite an existing password.

## Admin API

All admin routes require `Authorization: Bearer <token>`.

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Authenticate and issue a JWT |
| `GET` | `/api/admin/dashboard` | Get admin summary totals |
| `GET/POST` | `/api/admin/users` | List or create teachers/students |
| `GET/PUT/DELETE` | `/api/admin/users/{id}` | Read, update, or delete a user |
| `GET/POST` | `/api/admin/courses` | List or create courses/classes |
| `GET/PUT/DELETE` | `/api/admin/courses/{id}` | Read, update, or delete a course |
| `GET/POST` | `/api/admin/subjects` | List or create subjects |
| `GET/PUT/DELETE` | `/api/admin/subjects/{id}` | Read, update, assign a teacher, or delete a subject |
| `GET` | `/api/admin/assignments` | View/filter all assignments |
| `GET` | `/api/admin/submissions` | View/filter all submissions |

## MongoDB data model

The database uses separate collections for `users`, `courses`, `subjects`, `assignments`, and `submissions`. Relationships are stored as MongoDB ObjectId references:

- A subject references its course and optionally its assigned teacher.
- An assignment references its course, subject, and teacher.
- A submission references its assignment and student.

Documents retain small, stable IDs rather than embedding user or catalog snapshots. Admin read endpoints resolve names for the UI. Deletion is rejected when it would orphan related academic records; administrators can deactivate users, courses, or subjects instead.

## Validation

Run the project checks with:

```bash
dotnet build Backend/Backend.csproj
cd Frontend
npm run lint
npm run build
```

## Assumptions and current scope

- “Class” and “course” are represented by the same `Course` entity.
- Administrators can manage teacher/student accounts but cannot create another administrator through the public admin CRUD API.
- Teachers are assigned at subject level, which also associates them with the subject's course.
- Hard deletion is allowed only for unreferenced records; deactivation preserves academic history.
- This branch implements the requested administrator system. Teacher assignment authoring/review screens and student submission screens are separate role workflows and are not included yet. The admin oversight pages display assignment/submission documents created by those workflows or existing database data.

## Security notes

- `.env` and `.env.*` files are ignored, while `.env.example` remains tracked.
- Never commit an Atlas password or production JWT signing secret.
- Replace all development credentials before deployment.
- If a real credential was previously committed, rotate it; deleting it from the current file does not remove it from Git history.
