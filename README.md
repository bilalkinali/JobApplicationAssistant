# Job Application Assistant

Personal learning project for experimenting with AI-assisted development and local AI integration.

The app is built around a job application workflow, but the main purpose is learning: using prompts, PRDs, issues, agents, and local LLMs to build and improve a full-stack application.

## Features

- Manage profile information
- Manage job applications
- Upload CV data for profile extraction
- Prepare application material with AI
- Match profile evidence against job postings
- Generate draft application text
- Audit generated claims before use

## Tech Stack

- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- React
- TypeScript
- Vite
- Local AI / OpenAI-compatible endpoint

## Project Structure

```text
src/
  backend/
    JobApplicationAssistant.Api/
    JobApplicationAssistant.Api.Tests/

  frontend/
    job-application-assistant.web/

docs/
  planning/
  agents/
  testing/
```

## Local AI

The AI part is intended to run locally, for example through LM Studio, Ollama, or another OpenAI-compatible local server.

Because of that, this project is mostly tailored to my own local setup. Others may need to change the AI configuration before the AI features work.

AI settings are configured in:

```text
src/backend/JobApplicationAssistant.Api/appsettings.json
```

## Run Backend

```bash
cd src/backend/JobApplicationAssistant.Api
dotnet ef database update
dotnet run
```

## Run Frontend

```bash
cd src/frontend/job-application-assistant.web
npm install
npm run dev
```

## Tests

```bash
cd src/backend
dotnet test
```

```bash
cd src/frontend/job-application-assistant.web
npm run build
```

## Status

Experimental project. Not a finished product.
