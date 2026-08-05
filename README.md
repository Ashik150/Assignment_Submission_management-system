# Assignment & Submission Management System

Initial full-stack scaffold for the OnnoRokom Assistant Software Engineer recruitment project.

## Technology stack

- Frontend: React 19, TypeScript, and Vite
- Backend: ASP.NET Core 9 Web API with controllers and OpenAPI
- Database: MongoDB Atlas through the official MongoDB .NET driver

This repository currently contains project initialization only. The role-based assignment and submission features described in the project brief have not been implemented yet.

## Project structure

```text
Frontend/  React and TypeScript client
Backend/   ASP.NET Core Web API and MongoDB configuration
```

## Run the frontend

```bash
cd Frontend
npm install
cp .env.example .env.local
npm run dev
```

The frontend runs at `http://localhost:5173`.

## Configure and run the backend

Create a MongoDB Atlas deployment, allow your current IP address in its network access settings, and create a database user. Then expose the configuration values to ASP.NET Core:

```bash
export MongoDb__ConnectionString='mongodb+srv://<username>:<password>@<cluster-host>/?retryWrites=true&w=majority'
export MongoDb__DatabaseName='assignment_submission_db'

cd Backend
dotnet restore
dotnet run
```

The API runs at `http://localhost:5080`. In development, its OpenAPI document is available at `http://localhost:5080/openapi/v1.json`.

Do not commit a real Atlas connection string. The files named `.env.example` document the required settings and contain placeholders only.
