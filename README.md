# Job Application Platform

A monorepo containing AI-powered tools for managing your job search.

## Architecture

```
job-application-platform/
  JobMatchService/        -- Analyzes job postings against your profile (ASP.NET)
  ApplicationTracker/     -- Tracks applications, interviews, notes (ASP.NET + SQLite)
  EmailSync/              -- Syncs Gmail for application status updates (console + cron)
  frontend/               -- Landing page linking to service UIs (React + Vite)
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for containerized development)

## Local Development

### Run with Docker Compose

```bash
# Set required environment variables
export ANTHROPIC_API_KEY=your-key-here

# Start all services
docker compose up --build
```

| Service             | URL                    |
|---------------------|------------------------|
| Job Match Service   | http://localhost:5136  |
| Application Tracker | http://localhost:5002  |
| Frontend            | http://localhost:3000  |

### Run individually (without Docker)

```bash
# Job Match Service
dotnet run --project JobMatchService/src/Api

# Application Tracker
dotnet run --project ApplicationTracker/src/Api

# Frontend
cd frontend && npm install && npm run dev
```

## CI/CD

Each service has its own GitHub Actions workflow with path filters. Pushing changes to `main` that affect a specific service triggers only that service's build-and-push pipeline. Docker images are published to `ghcr.io/ozshpigel/`.

## Solution

Open `job-application-platform.sln` in Visual Studio or Rider to work with all .NET projects at once.
