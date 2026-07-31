# CodeKids

Interactive programming learning platform for children ages 8–12.

## Stack

- ASP.NET Core 10 + CQRS handlers
- Angular standalone frontend
- PostgreSQL via Docker Compose
- JWT auth with Student / Parent / Teacher roles

## Features

- Auth: register/login with role-based access
- Courses with themed lessons and step challenges
- Quizzes with XP rewards
- Badges awarded from XP + completed steps
- Unlockable avatars
- Parent dashboard for linked children
- Teacher dashboard for classroom overview
- Super Admin: create teachers/students/courses/classrooms and assign teachers + students
- Teacher-authored quizzes and assignments with answer review
- Zoom live sessions assigned to classrooms
- WhatsApp notify phones + share link when creating Zoom meetings

## Demo accounts

- Super Admin: `admin@codekids.local` / `Admin123!`
- Student: `student@codekids.local` / `Student123!`
- Parent: `parent@codekids.local` / `Parent123!`
- Teacher: `teacher@codekids.local` / `Teacher123!`

## Zoom setup

### App-level (Server-to-Server) fallback

```json
"Zoom": {
  "AccountId": "your-account-id",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "HostUserId": "me",
  "UserOAuthClientId": "oauth-app-client-id",
  "UserOAuthClientSecret": "oauth-app-client-secret",
  "UserOAuthRedirectUri": "http://localhost:5078/api/zoom/callback",
  "FrontendRedirectUri": "http://localhost:4200/teacher/zoom"
}
```

Teachers can **Connect personal Zoom** on `/teacher/zoom`. Meetings then use their account (`users/me/meetings`). If not connected, Server-to-Server credentials are used; if those are empty, mock join/start URLs are created for local UI testing.

OAuth redirect URI must match the Zoom OAuth app settings. Required user scopes typically include `meeting:write` and `user:read`.

## WhatsApp setup

Meta Cloud API credentials in `appsettings.json`:

```json
"WhatsApp": {
  "AccessToken": "your-token",
  "PhoneNumberId": "your-phone-number-id",
  "ApiVersion": "v21.0"
}
```

When creating a Zoom meeting with notify enabled, WhatsApp is sent to:
- each enrolled student's `MobilePhone` (set under Admin → Students)
- classroom `WhatsAppNotifyPhones` (comma-separated E.164)

Classroom `WhatsAppGroupInviteUrl` is included in the message text when set.

When WhatsApp API credentials are empty, a `wa.me` share link is still returned.

## Run locally

1. Start PostgreSQL:

```bash
docker compose up -d
```

2. Run API:

```bash
cd src/backend/CodeKids.Api
dotnet restore
dotnet run
```

API: `http://localhost:5078` (Swagger included)

3. Run Angular:

```bash
cd src/frontend
npm install
npm start
```

App: `http://localhost:4200`

## API map

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/courses`
- `GET /api/lessons`
- `POST /api/progress/complete-step` (Student)
- `GET /api/quizzes` + `POST /api/quizzes/submit` (Student)
- `GET /api/badges/me` + `GET /api/avatars` (Student)
- `GET /api/dashboard/parent` (Parent)
- `GET /api/dashboard/teacher` (Teacher)
- `GET /api/meetings` (authenticated)
- `POST /api/meetings` (Teacher) — creates Zoom meeting for a classroom + optional WhatsApp notify
- `GET/POST /api/admin/users` (SuperAdmin)
- `POST /api/admin/courses` (SuperAdmin)
- `GET/POST /api/classrooms` (+ assign teacher/course, enroll students, WhatsApp settings)
- `POST /api/quizzes` (Teacher)
- `GET/POST /api/assignments` + submit/grade (Teacher/Student)
