# Assignment & Submission Management System

A full-stack academic management application built with React, TypeScript, ASP.NET Core, and MongoDB. It provides secure, role-aware administrator, teacher, and student workspaces for managing people, academic structure, assignments, submissions, marks, and feedback.

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

### Teacher portal

- Sign in through the shared login page with credentials created by an administrator
- Dashboard totals for assigned subjects, assignments, published work, submissions, and pending reviews
- View only the active courses and subjects assigned to the authenticated teacher
- Create, search, filter, update, publish, draft, and safely delete assignments
- Define assignment title, instructions, course, subject, deadline, and maximum marks
- View only submissions belonging to the teacher's own assignments
- Read student answers and assign marks up to the configured maximum
- Provide feedback and change submission status to Submitted, Late, Reviewed, or Returned
- Responsive assignment and submission review interfaces

### Student portal

- Sign in through the shared login page with credentials created by an administrator
- View published assignments belonging only to the student's assigned course
- Search assignments and filter them by subject
- Read assignment instructions, deadline, maximum marks, and current submission state
- Submit a written answer, a PDF attachment, or both before the deadline
- Update written answers and replace or remove PDF attachments while revision remains open
- View submission history, status, awarded marks, and teacher feedback
- See whether an assignment is open, submitted, returned for revision, or reviewed

### Backend safeguards

- PBKDF2-SHA256 password hashing with per-password salts
- Role authorization on every `/api/admin/*`, `/api/teacher/*`, and `/api/student/*` endpoint
- Ownership checks preventing teachers from accessing another teacher's assignments or submissions
- Course checks preventing students from accessing assignments outside their assigned course
- Deadline and review-state checks preventing invalid or late answer updates
- PDF extension, 10 MB size, and file-signature validation before storage
- Authenticated PDF downloads restricted to the student, assignment teacher, and administrators
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
  Controllers/     Authentication and role-specific REST endpoints
  Data/            MongoDB collections, indexes, and seed logic
  Models/          MongoDB documents and domain enums
  Services/        Password hashing and token creation
Frontend/
  src/components/  Reusable role-aware layout, icons, and modal
  src/lib/         API client
  src/pages/       Admin, teacher, and student workflow screens
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

### Create a teacher login

Sign in as the development administrator, open **People**, and create an active Teacher account with an email and temporary password. Then create a course and subject, assign that teacher to the subject, log out, and sign in through the same page with the teacher credentials.

### Create a student login

Sign in as the development administrator, create or select an active course, then open **People** and create an active Student account. Select the student's course and provide an email and temporary password. Log out and use those credentials on the shared login page. The student will see published assignments for that course only.

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

## Teacher API

All teacher routes require a JWT containing the `Teacher` role. Results are restricted to the authenticated teacher.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/teacher/dashboard` | Get teacher workflow totals |
| `GET` | `/api/teacher/subjects` | List subjects assigned to the teacher |
| `GET/POST` | `/api/teacher/assignments` | List or create the teacher's assignments |
| `GET/PUT/DELETE` | `/api/teacher/assignments/{id}` | Read, update, publish/draft, or delete an assignment |
| `GET` | `/api/teacher/submissions` | List/filter submissions for the teacher's assignments |
| `GET` | `/api/teacher/submissions/{id}` | Read a submission and student answer |
| `PUT` | `/api/teacher/submissions/{id}/review` | Save marks, feedback, and submission status |

## Student API

All student routes require a JWT containing the `Student` role. Results are restricted to the authenticated student's assigned course and own submissions.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/student/dashboard` | Get assignment and submission totals |
| `GET` | `/api/student/assignments` | List/search published assignments for the student's course |
| `GET` | `/api/student/assignments/{id}` | Read assignment details and submission state |
| `POST` | `/api/student/assignments/{id}/submission` | Submit text and/or a PDF before the deadline |
| `GET` | `/api/student/submissions` | List the student's submissions, marks, and feedback |
| `GET` | `/api/student/submissions/{id}` | Read one submission and its review details |
| `PUT` | `/api/student/submissions/{id}` | Update text and replace/remove an eligible PDF |
| `GET` | `/api/submissions/{id}/pdf` | Download an authorized submission PDF |

Submission create and update requests use `multipart/form-data`. The `answer` field is optional when a `pdf` is included. PDF files must have a `.pdf` extension, a valid PDF signature, and be no larger than 10 MB. Send `removePdf=true` during an update to remove the existing attachment.

## MongoDB data model

The database uses separate collections for `users`, `courses`, `subjects`, `assignments`, and `submissions`. PDF attachment bytes are stored in the `submission_pdfs` GridFS bucket. Relationships are stored as MongoDB ObjectId references:

- A student user references the course in which they are enrolled.
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
- Teachers can only create assignments for active subjects assigned to them and cannot delete assignments that already have submissions.
- A numeric mark is required when setting a submission to `Reviewed`; `Returned` supports revision requests without a mark.
- Each student belongs to one course and can only view published assignments assigned to that course.
- Each student can create one submission per assignment containing text, a PDF, or both. Submissions cannot be created after the deadline.
- Submitted, Late, or Returned answers can be updated before the deadline; reviewed answers are locked. Updating a returned answer changes its status back to `Submitted` for another teacher review.

## Security notes

- `.env` and `.env.*` files are ignored, while `.env.example` remains tracked.
- Never commit an Atlas password or production JWT signing secret.
- Replace all development credentials before deployment.
- If a real credential was previously committed, rotate it; deleting it from the current file does not remove it from Git history.
