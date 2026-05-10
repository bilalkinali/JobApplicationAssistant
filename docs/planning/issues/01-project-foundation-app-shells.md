# Project Foundation: App Shells

Status: done
Type: AFK

## Parent

docs/planning/v1-prd.md

## What to build

Create the separate V1 application foundation with an ASP.NET Core Minimal API backend and a React + Vite + TypeScript frontend. The result should establish the backend/frontend project shape needed for the desktop web app, with both apps ready for later vertical feature slices.

This issue covers the project foundation only. It should not implement AI behavior, profile fact management, application workflow screens, exports, or provider integrations beyond the minimal project structure needed for later issues.

## Acceptance criteria

- [x] A new ASP.NET Core Minimal API backend project exists for the V1 app.
- [x] A new React + Vite + TypeScript frontend project exists for the V1 app.
- [x] The backend exposes a minimal health or root endpoint that confirms the API project is reachable.
- [x] The frontend has a minimal desktop-first app shell that can later host Home, Profile, Applications, and Settings areas.
- [x] Project naming and folder structure make it clear which code belongs to backend and frontend.
- [x] No OpenAI dependency or configuration is introduced.
- [x] No AI workflow behavior is implemented in this slice.

## Blocked by

None - can start immediately.
