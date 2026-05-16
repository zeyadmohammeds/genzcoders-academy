# Advanced Backend Architecture

The backend is now designed as the source of truth for a future Next.js frontend. Keep Next.js focused on UI, routing, and client experience; keep business rules, security, pricing, attendance, grading, progress, and reporting inside ASP.NET Core.

## Main Domains

- Identity and roles: students, parents, engineers, CTAs, school admins, academy admins.
- Profiles: student, parent, staff/CTA, school coordinator.
- Schools and partnerships: partner status, MOU metadata, discounts, leads.
- Course engine: courses, modules, 12-session curriculum, cohorts, scheduled session instances.
- Live learning: live Zoom metadata, session instances, attendance, recordings.
- Tasks: task templates, submissions, grading, rubric scores, CTA queues.
- Quizzes: quizzes, questions, options, attempts, answers, scoring policy.
- Gamification: XP transactions, badges, student badges, weekly challenges.
- Commerce: enrollment orders, enrollment line items, promo codes, payment transactions.
- Growth: referrals, discount credits, public student projects.
- Communication: notification templates, queued messages, audit logs.
- Reporting: admin dashboard and backend schema map.

## Key API Routes

- `GET /api/courses`
- `GET /api/courses/{slug}`
- `POST /api/enrollments`
- `POST /api/applications`
- `POST /api/applications/questions`
- `POST /api/applications/{applicationId}/payment`
- `POST /api/applications/{applicationId}/review`
- `GET /api/course-rounds`
- `POST /api/course-rounds`
- `POST /api/course-rounds/move-student`
- `GET /api/course-rooms/{courseRoundId}`
- `GET /api/course-rooms/leaderboard`
- `POST /api/learning/lessons`
- `POST /api/learning/materials`
- `POST /api/learning/tasks`
- `POST /api/learning/tasks/submissions`
- `POST /api/learning/tasks/submissions/{submissionId}/grade`
- `POST /api/learning/attendance`
- `POST /api/quizzes`
- `POST /api/quizzes/questions`
- `POST /api/quizzes/attempts`
- `PUT /api/notifications/settings`
- `POST /api/notifications/send`
- `GET /api/live-sessions/upcoming`
- `GET /api/live-sessions/{id}/embed-config`
- `POST /api/live-sessions/zoom-signature`
- `POST /api/partnerships/leads`
- `GET /api/admin/dashboard`
- `GET /api/backend-map`

## Next Backend Steps

1. Add JWT bearer auth for the Next.js frontend while keeping Google OAuth.
2. Implement Zoom Meeting SDK signature generation server-side.
3. Add Paymob or Fawry payment callback endpoints.
4. Add WhatsApp/email notification workers.
5. Add full CRUD filtering/pagination for admin tables.
6. Add PDF report and certificate generation.
