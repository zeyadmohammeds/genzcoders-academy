# ElSewedy GenZ Coders — Full Product Specification
### Version 1.0 · May 2026 · Internal Reference Document

---

## TABLE OF CONTENTS

1. Executive Summary
2. Product Vision & Strategic Goals
3. User Roles & Personas
4. Platform Architecture Overview
5. Full Database Schema
6. Complete API Specification
7. Feature Deep-Dives
   - 7.1 Authentication & Onboarding
   - 7.2 Course & Curriculum Engine
   - 7.3 Live Session Management
   - 7.4 Tasks & Assignments
   - 7.5 Quizzes & Exams Engine
   - 7.6 Progress & Badge System
   - 7.7 Gamification & Leaderboards
   - 7.8 Referral System
   - 7.9 Promo Codes & Discounts
   - 7.10 CTA (Co-Teaching Assistant) Portal
   - 7.11 School Admin Portal
   - 7.12 Parent Dashboard
   - 7.13 Notification & Communication System
8. Session Engagement Design
9. Marketing & Virality Strategy
10. Content Calendar & Plan
11. Launch Roadmap

---

# 1. EXECUTIVE SUMMARY

ElSewedy GenZ Coders is a live, project-based technology education platform affiliated with El Sewedy Electrometer. It serves students aged 10–18 across Egypt through 5 structured courses, delivered by qualified engineers, supported by Co-Teaching Assistants (CTAs), and reinforced with online tasks, quizzes, and workshops.

This document defines the complete digital platform required to run the academy at scale — covering every user role, data model, API endpoint, engagement mechanic, growth loop, and marketing strategy needed to launch Phase 1 on 8 June 2026 and grow sustainably into Phase 2 and beyond.

**The platform must do three things exceptionally well:**
- Make learning feel like a game, not school
- Make parents trust it completely
- Make students talk about it to their friends

---

# 2. PRODUCT VISION & STRATEGIC GOALS

## Vision Statement
> "Egypt's most engaging youth tech platform — where students don't just learn to code, they build things they're proud to show the world."

## Phase 1 Goals (June – August 2026)
| Goal | Target |
|------|--------|
| Enrolled students | 50 students |
| Active course completions | 40+ |
| Net Promoter Score | 8.5 / 10 |
| Referral-driven enrollments | 30% of total |
| Social media followers | 500+ |
| School partnerships | 3 MOUs signed |

## Phase 2 Goals (Q4 2026)
| Goal | Target |
|------|--------|
| Enrolled students | 200+ |
| Courses offered | 8 (add Web Advanced, Mobile, Embedded) |
| School partnerships | 10+ |
| Platform DAU | 150+ |

## Core Design Principles
- **Project-first**: Every session produces something tangible
- **Peer-powered**: CTAs and leaderboards make learning social
- **Transparent**: Parents see everything their child does
- **Viral by design**: Referral codes, shareable project links, public showcase pages
- **Low-friction enrollment**: 3 steps max from discovery to enrolled

---

# 3. USER ROLES & PERSONAS

## 3.1 Roles Overview
| Role | Description | Key Needs |
|------|-------------|-----------|
| `student` | Ages 10–18, enrolled in 1+ courses | Progress clarity, fun, peer competition |
| `parent` | Guardian of enrolled student | Visibility, trust, payment control |
| `engineer` | Alaa / Fady / Aya — delivers core sessions | Session tools, attendance, grade submission |
| `cta` | Co-Teaching Assistant from ElSewedy school | Task grading, support session hosting |
| `school_admin` | School coordinator at partner school | Student lists, progress reports |
| `academy_admin` | Platform superadmin (Alaa as lead) | Full control — pricing, users, content |

## 3.2 Persona Profiles

### Persona A — Youssef, 14, STEM school student
- Wants to build real things, not watch lectures
- Competitive, will respond to leaderboards
- Needs: project showcase, XP system, badge sharing

### Persona B — Dr. Mona, parent of a 12-year-old
- Skeptical of online learning after bad experiences
- Needs: weekly progress report, WhatsApp updates, clear payment receipts

### Persona C — Nour, 17, CTA from ElSewedy school
- Proud to be a mentor, wants recognition
- Needs: clean task grading interface, CTA leaderboard, certificate

### Persona D — Mr. Khaled, coordinator at a secondary school
- Busy, needs low-friction tools
- Needs: one dashboard to see all enrolled students from his school, auto-reports

---

# 4. PLATFORM ARCHITECTURE OVERVIEW

## 4.1 Tech Stack Recommendation

**Frontend:** React (Next.js 14) + Tailwind CSS + shadcn/ui
**Backend:** Node.js (Express) or Python (FastAPI)
**Database:** PostgreSQL (primary) + Redis (sessions/cache)
**Auth:** JWT + Refresh Tokens (bcrypt passwords)
**File Storage:** AWS S3 or Cloudflare R2 (project uploads, profile photos)
**Real-time:** Socket.io (live session chat, live quiz)
**Email:** Resend or SendGrid
**SMS/WhatsApp:** Twilio / WhatsApp Business API
**Payments:** Paymob (Egypt) or Fawry
**Video Sessions:** Zoom SDK embed or Google Meet links per session
**Deployment:** Railway / Render / DigitalOcean App Platform

## 4.2 Service Modules
```
Platform
├── Auth Service          → login, register, JWT, roles
├── User Service          → profiles, roles, preferences
├── Course Service        → curriculum, sessions, materials
├── Enrollment Service    → orders, pricing, discounts, promo
├── Session Service       → scheduling, attendance, recordings
├── Task Service          → assignments, submissions, grading
├── Quiz Service          → question bank, attempts, scoring
├── Progress Service      → XP, badges, streaks, leaderboards
├── Referral Service      → codes, tracking, rewards
├── Notification Service  → email, SMS, WhatsApp, in-app
├── School Service        → partnerships, coordinators, reports
├── Payment Service       → invoices, Paymob gateway, receipts
└── Admin Service         → platform config, analytics, reports
```

---

# 5. FULL DATABASE SCHEMA

## 5.1 Users & Auth

```sql
-- USERS (base identity table)
CREATE TABLE users (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email           VARCHAR(255) UNIQUE,
  phone           VARCHAR(20) UNIQUE,
  password_hash   VARCHAR(255) NOT NULL,
  role            ENUM('student','parent','engineer','cta','school_admin','academy_admin'),
  first_name      VARCHAR(100) NOT NULL,
  last_name       VARCHAR(100) NOT NULL,
  avatar_url      TEXT,
  is_verified     BOOLEAN DEFAULT FALSE,
  is_active       BOOLEAN DEFAULT TRUE,
  created_at      TIMESTAMP DEFAULT NOW(),
  updated_at      TIMESTAMP DEFAULT NOW()
);

-- REFRESH TOKENS
CREATE TABLE refresh_tokens (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID REFERENCES users(id) ON DELETE CASCADE,
  token       VARCHAR(512) UNIQUE NOT NULL,
  expires_at  TIMESTAMP NOT NULL,
  revoked     BOOLEAN DEFAULT FALSE,
  created_at  TIMESTAMP DEFAULT NOW()
);

-- STUDENT PROFILES (extends users where role = student)
CREATE TABLE student_profiles (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  age             INTEGER,
  school_id       UUID REFERENCES schools(id),
  parent_id       UUID REFERENCES users(id),   -- linked parent account
  grade_level     VARCHAR(50),
  xp_total        INTEGER DEFAULT 0,
  streak_days     INTEGER DEFAULT 0,
  last_active     TIMESTAMP,
  referral_code   VARCHAR(20) UNIQUE,          -- student's own referral code
  referred_by     UUID REFERENCES users(id),   -- who referred this student
  created_at      TIMESTAMP DEFAULT NOW()
);

-- PARENT PROFILES
CREATE TABLE parent_profiles (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  whatsapp_number VARCHAR(20),
  notification_prefs JSONB DEFAULT '{"whatsapp":true,"email":true,"sms":false}',
  created_at      TIMESTAMP DEFAULT NOW()
);

-- ENGINEER / CTA PROFILES
CREATE TABLE staff_profiles (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  bio             TEXT,
  specialization  VARCHAR(255),
  linkedin_url    TEXT,
  is_cta          BOOLEAN DEFAULT FALSE,
  cta_school_id   UUID REFERENCES schools(id),  -- CTAs come from ElSewedy school
  created_at      TIMESTAMP DEFAULT NOW()
);
```

## 5.2 Schools & Partnerships

```sql
-- SCHOOLS
CREATE TABLE schools (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name            VARCHAR(255) NOT NULL,
  type            ENUM('secondary','stem','iats','other'),
  city            VARCHAR(100),
  address         TEXT,
  phone           VARCHAR(20),
  email           VARCHAR(255),
  logo_url        TEXT,
  partner_since   DATE,
  mou_signed      BOOLEAN DEFAULT FALSE,
  mou_signed_at   TIMESTAMP,
  mou_document_url TEXT,
  discount_rate   DECIMAL(5,2) DEFAULT 15.00,   -- % off for partner students
  bundle_discount DECIMAL(5,2) DEFAULT 25.00,
  is_active       BOOLEAN DEFAULT TRUE,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- SCHOOL COORDINATORS (school_admin users linked to a school)
CREATE TABLE school_coordinators (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID REFERENCES users(id) ON DELETE CASCADE,
  school_id   UUID REFERENCES schools(id) ON DELETE CASCADE,
  is_primary  BOOLEAN DEFAULT TRUE,
  created_at  TIMESTAMP DEFAULT NOW(),
  UNIQUE(user_id, school_id)
);
```

## 5.3 Courses & Curriculum

```sql
-- COURSES
CREATE TABLE courses (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug                VARCHAR(100) UNIQUE NOT NULL,  -- 'scratch', 'intro-cpp', etc.
  title               VARCHAR(255) NOT NULL,
  subtitle            TEXT,
  description         TEXT,
  target_age_min      INTEGER,
  target_age_max      INTEGER,
  standard_price      DECIMAL(10,2) NOT NULL,
  cover_image_url     TEXT,
  icon_emoji          VARCHAR(10),
  color_hex           VARCHAR(7),
  total_core_sessions INTEGER DEFAULT 8,
  total_ts_sessions   INTEGER DEFAULT 4,
  deliverable         TEXT,    -- "A fully designed original game"
  skills_taught       TEXT[],  -- ['loops','conditions','OOP']
  phase               INTEGER DEFAULT 1,
  is_active           BOOLEAN DEFAULT TRUE,
  sort_order          INTEGER DEFAULT 0,
  created_at          TIMESTAMP DEFAULT NOW()
);

-- COURSE SESSIONS (the curriculum — each row = one session)
CREATE TABLE course_sessions (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  course_id       UUID REFERENCES courses(id) ON DELETE CASCADE,
  session_number  INTEGER NOT NULL,   -- 1-12 overall
  session_type    ENUM('core','technical_support'),
  title           VARCHAR(255) NOT NULL,
  description     TEXT,
  outcome         TEXT,               -- "Score system + touch detection logic"
  principle       TEXT,               -- "Decision making and data tracking"
  duration_minutes INTEGER,           -- 90 for core, 240 for TS
  materials_url   TEXT[],             -- array of resource links
  sort_order      INTEGER,
  UNIQUE(course_id, session_number)
);
```

## 5.4 Enrollments & Pricing

```sql
-- ENROLLMENT ORDERS
CREATE TABLE enrollment_orders (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id      UUID REFERENCES users(id),
  order_type      ENUM('single','bundle'),
  subtotal        DECIMAL(10,2) NOT NULL,
  discount_amount DECIMAL(10,2) DEFAULT 0,
  discount_type   VARCHAR(100),        -- 'early_bird', 'partner_school', 'referral', 'sibling'
  promo_code_id   UUID REFERENCES promo_codes(id),
  referral_code   VARCHAR(20),
  total_amount    DECIMAL(10,2) NOT NULL,
  payment_status  ENUM('pending','paid','failed','refunded') DEFAULT 'pending',
  payment_method  VARCHAR(50),
  payment_ref     VARCHAR(255),        -- Paymob/Fawry transaction ID
  paid_at         TIMESTAMP,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- ENROLLMENT LINE ITEMS (one per course in an order)
CREATE TABLE enrollment_items (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id        UUID REFERENCES enrollment_orders(id) ON DELETE CASCADE,
  course_id       UUID REFERENCES courses(id),
  unit_price      DECIMAL(10,2),
  discount_amount DECIMAL(10,2) DEFAULT 0,
  final_price     DECIMAL(10,2),
  status          ENUM('active','completed','dropped','refunded') DEFAULT 'active',
  enrolled_at     TIMESTAMP DEFAULT NOW()
);

-- COHORTS (scheduled run of a course — ties sessions to dates)
CREATE TABLE cohorts (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  course_id       UUID REFERENCES courses(id),
  cohort_name     VARCHAR(100),          -- "Phase 1 — June 2026"
  engineer_id     UUID REFERENCES users(id),
  cta_id          UUID REFERENCES users(id),
  start_date      DATE NOT NULL,
  end_date        DATE,
  max_students    INTEGER DEFAULT 20,
  session_link    TEXT,                  -- Zoom/Meet link
  status          ENUM('upcoming','active','completed') DEFAULT 'upcoming',
  created_at      TIMESTAMP DEFAULT NOW()
);

-- COHORT ENROLLMENTS (maps enrolled students → cohort)
CREATE TABLE cohort_enrollments (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  cohort_id       UUID REFERENCES cohorts(id),
  student_id      UUID REFERENCES users(id),
  enrollment_item_id UUID REFERENCES enrollment_items(id),
  enrolled_at     TIMESTAMP DEFAULT NOW(),
  UNIQUE(cohort_id, student_id)
);
```

## 5.5 Session Scheduling & Attendance

```sql
-- SCHEDULED SESSION INSTANCES (each session has a date/time)
CREATE TABLE session_instances (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  cohort_id       UUID REFERENCES cohorts(id),
  course_session_id UUID REFERENCES course_sessions(id),
  scheduled_at    TIMESTAMP NOT NULL,
  duration_minutes INTEGER,
  session_link    TEXT,
  recording_url   TEXT,
  status          ENUM('scheduled','live','completed','cancelled') DEFAULT 'scheduled',
  notes           TEXT,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- ATTENDANCE
CREATE TABLE attendance (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_instance_id UUID REFERENCES session_instances(id) ON DELETE CASCADE,
  student_id          UUID REFERENCES users(id),
  status              ENUM('present','absent','late','excused') DEFAULT 'present',
  joined_at           TIMESTAMP,
  left_at             TIMESTAMP,
  xp_earned          INTEGER DEFAULT 0,
  marked_by           UUID REFERENCES users(id),
  created_at          TIMESTAMP DEFAULT NOW(),
  UNIQUE(session_instance_id, student_id)
);
```

## 5.6 Tasks & Assignments

```sql
-- TASK TEMPLATES (created by engineers, assigned to a course session)
CREATE TABLE tasks (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  course_session_id   UUID REFERENCES course_sessions(id),
  cohort_id           UUID REFERENCES cohorts(id),   -- null = applies to all cohorts
  title               VARCHAR(255) NOT NULL,
  description         TEXT NOT NULL,
  instructions        TEXT,
  task_type           ENUM('code','design','project','reflection','research'),
  submission_type     ENUM('file','link','text','image'),
  max_score           INTEGER DEFAULT 100,
  xp_reward           INTEGER DEFAULT 50,
  due_hours_after_session INTEGER DEFAULT 48,   -- due 48h after session
  is_required         BOOLEAN DEFAULT TRUE,
  rubric              JSONB,   -- [{"criterion":"Logic","max":30},{"criterion":"Output","max":70}]
  sample_solution_url TEXT,   -- shown only after submission or deadline
  created_by          UUID REFERENCES users(id),
  created_at          TIMESTAMP DEFAULT NOW()
);

-- TASK SUBMISSIONS
CREATE TABLE task_submissions (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id         UUID REFERENCES tasks(id),
  student_id      UUID REFERENCES users(id),
  submission_url  TEXT,       -- S3 link to file or external URL
  submission_text TEXT,
  submitted_at    TIMESTAMP DEFAULT NOW(),
  is_late         BOOLEAN DEFAULT FALSE,
  score           INTEGER,
  feedback        TEXT,
  graded_by       UUID REFERENCES users(id),  -- CTA or engineer
  graded_at       TIMESTAMP,
  xp_awarded      INTEGER DEFAULT 0,
  status          ENUM('pending','graded','returned_for_revision') DEFAULT 'pending'
);
```

## 5.7 Quizzes & Exams

```sql
-- QUIZ TEMPLATES
CREATE TABLE quizzes (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  course_session_id UUID REFERENCES course_sessions(id),
  cohort_id       UUID REFERENCES cohorts(id),
  title           VARCHAR(255) NOT NULL,
  quiz_type       ENUM('formative','mid_course','final_exam','bonus'),
  time_limit_minutes INTEGER,
  max_attempts    INTEGER DEFAULT 1,
  pass_score      INTEGER DEFAULT 60,   -- % to pass
  xp_reward       INTEGER DEFAULT 100,
  shuffle_questions BOOLEAN DEFAULT TRUE,
  show_answers_after ENUM('immediately','after_deadline','never') DEFAULT 'after_deadline',
  available_from  TIMESTAMP,
  available_until TIMESTAMP,
  is_published    BOOLEAN DEFAULT FALSE,
  created_by      UUID REFERENCES users(id),
  created_at      TIMESTAMP DEFAULT NOW()
);

-- QUESTION BANK
CREATE TABLE questions (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  quiz_id         UUID REFERENCES quizzes(id) ON DELETE CASCADE,
  question_text   TEXT NOT NULL,
  question_type   ENUM('mcq','true_false','short_answer','code_output','match'),
  image_url       TEXT,
  code_snippet    TEXT,        -- show code, ask what it outputs
  points          INTEGER DEFAULT 10,
  explanation     TEXT,        -- shown after answering
  sort_order      INTEGER DEFAULT 0,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- MCQ OPTIONS
CREATE TABLE question_options (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  question_id     UUID REFERENCES questions(id) ON DELETE CASCADE,
  option_text     TEXT NOT NULL,
  is_correct      BOOLEAN DEFAULT FALSE,
  sort_order      INTEGER DEFAULT 0
);

-- QUIZ ATTEMPTS
CREATE TABLE quiz_attempts (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  quiz_id         UUID REFERENCES quizzes(id),
  student_id      UUID REFERENCES users(id),
  attempt_number  INTEGER DEFAULT 1,
  started_at      TIMESTAMP DEFAULT NOW(),
  submitted_at    TIMESTAMP,
  score           INTEGER,
  percentage      DECIMAL(5,2),
  passed          BOOLEAN,
  xp_awarded      INTEGER DEFAULT 0,
  time_taken_seconds INTEGER
);

-- QUESTION ANSWERS (per attempt)
CREATE TABLE question_answers (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  attempt_id      UUID REFERENCES quiz_attempts(id) ON DELETE CASCADE,
  question_id     UUID REFERENCES questions(id),
  selected_option_id UUID REFERENCES question_options(id),
  text_answer     TEXT,
  is_correct      BOOLEAN,
  points_earned   INTEGER DEFAULT 0,
  answered_at     TIMESTAMP DEFAULT NOW()
);
```

## 5.8 Progress, XP & Badges

```sql
-- XP TRANSACTIONS (audit trail of every XP event)
CREATE TABLE xp_transactions (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id      UUID REFERENCES users(id),
  amount          INTEGER NOT NULL,       -- can be negative for deductions
  source_type     ENUM('attendance','task','quiz','streak','referral','bonus','badge'),
  source_id       UUID,                   -- ID of the triggering record
  description     TEXT,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- BADGES
CREATE TABLE badges (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug            VARCHAR(100) UNIQUE,
  name            VARCHAR(100) NOT NULL,
  description     TEXT,
  icon_url        TEXT,
  color_hex       VARCHAR(7),
  xp_reward       INTEGER DEFAULT 0,
  badge_type      ENUM('milestone','streak','performance','social','special'),
  criteria_type   ENUM('xp_threshold','streak_days','task_count','quiz_score','referral_count','attendance_rate','course_complete','manual'),
  criteria_value  INTEGER,      -- e.g., 500 XP, 7 days streak, 5 tasks
  is_hidden       BOOLEAN DEFAULT FALSE,  -- surprise badges
  created_at      TIMESTAMP DEFAULT NOW()
);

-- STUDENT BADGES (earned badges)
CREATE TABLE student_badges (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id      UUID REFERENCES users(id),
  badge_id        UUID REFERENCES badges(id),
  earned_at       TIMESTAMP DEFAULT NOW(),
  UNIQUE(student_id, badge_id)
);

-- LEADERBOARDS (materialized snapshots, recalculated hourly)
CREATE TABLE leaderboard_snapshots (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  scope           ENUM('global','course','cohort','school'),
  scope_id        UUID,          -- course_id, cohort_id, or school_id
  student_id      UUID REFERENCES users(id),
  rank            INTEGER,
  xp_total        INTEGER,
  tasks_completed INTEGER,
  quiz_avg_score  DECIMAL(5,2),
  attendance_rate DECIMAL(5,2),
  snapshot_at     TIMESTAMP DEFAULT NOW()
);

-- STREAKS
CREATE TABLE student_streaks (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id      UUID UNIQUE REFERENCES users(id),
  current_streak  INTEGER DEFAULT 0,
  longest_streak  INTEGER DEFAULT 0,
  last_activity_date DATE,
  streak_frozen_until DATE   -- streak freeze power-up
);
```

## 5.9 Referral & Promo System

```sql
-- REFERRAL TRACKING
CREATE TABLE referral_events (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  referrer_id     UUID REFERENCES users(id),    -- who shared the code
  referred_id     UUID REFERENCES users(id),    -- who used the code
  referral_code   VARCHAR(20) NOT NULL,
  status          ENUM('registered','enrolled','rewarded') DEFAULT 'registered',
  reward_type     VARCHAR(50),       -- 'discount','xp','cash_credit'
  reward_value    DECIMAL(10,2),
  rewarded_at     TIMESTAMP,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- PROMO CODES
CREATE TABLE promo_codes (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code            VARCHAR(50) UNIQUE NOT NULL,  -- 'EARLYBIRD10', 'STEM15'
  description     TEXT,
  discount_type   ENUM('percentage','fixed'),
  discount_value  DECIMAL(10,2) NOT NULL,
  max_uses        INTEGER,                       -- null = unlimited
  used_count      INTEGER DEFAULT 0,
  min_order_value DECIMAL(10,2) DEFAULT 0,
  valid_from      TIMESTAMP DEFAULT NOW(),
  valid_until     TIMESTAMP,
  applies_to      ENUM('any','course','bundle','specific_course'),
  course_id       UUID REFERENCES courses(id),  -- if specific_course
  school_id       UUID REFERENCES schools(id),  -- school-specific codes
  created_by      UUID REFERENCES users(id),
  is_active       BOOLEAN DEFAULT TRUE,
  created_at      TIMESTAMP DEFAULT NOW()
);

-- PROMO CODE USAGE LOG
CREATE TABLE promo_code_uses (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  promo_code_id   UUID REFERENCES promo_codes(id),
  user_id         UUID REFERENCES users(id),
  order_id        UUID REFERENCES enrollment_orders(id),
  discount_applied DECIMAL(10,2),
  used_at         TIMESTAMP DEFAULT NOW()
);
```

## 5.10 Projects & Showcase

```sql
-- STUDENT PROJECTS (final deliverables — public showcase)
CREATE TABLE student_projects (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id      UUID REFERENCES users(id),
  course_id       UUID REFERENCES courses(id),
  cohort_id       UUID REFERENCES cohorts(id),
  title           VARCHAR(255) NOT NULL,
  description     TEXT,
  project_url     TEXT,         -- live URL (web app) or demo link
  thumbnail_url   TEXT,
  demo_video_url  TEXT,
  files_url       TEXT[],
  is_public       BOOLEAN DEFAULT FALSE,    -- opt-in public showcase
  likes_count     INTEGER DEFAULT 0,
  views_count     INTEGER DEFAULT 0,
  featured        BOOLEAN DEFAULT FALSE,    -- admin can feature top projects
  submitted_at    TIMESTAMP DEFAULT NOW()
);

-- PROJECT LIKES
CREATE TABLE project_likes (
  student_id  UUID REFERENCES users(id),
  project_id  UUID REFERENCES student_projects(id),
  liked_at    TIMESTAMP DEFAULT NOW(),
  PRIMARY KEY(student_id, project_id)
);
```

## 5.11 Notifications

```sql
-- NOTIFICATION LOG
CREATE TABLE notifications (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID REFERENCES users(id),
  type            VARCHAR(100),    -- 'session_reminder','task_due','badge_earned', etc.
  title           VARCHAR(255),
  body            TEXT,
  data            JSONB,           -- extra payload
  channel         ENUM('in_app','email','sms','whatsapp','push'),
  is_read         BOOLEAN DEFAULT FALSE,
  sent_at         TIMESTAMP,
  read_at         TIMESTAMP,
  created_at      TIMESTAMP DEFAULT NOW()
);
```

---

# 6. COMPLETE API SPECIFICATION

All endpoints use base URL: `https://api.genzcoders.academy/v1`
All protected routes require: `Authorization: Bearer <jwt_token>`
All responses follow: `{ success: bool, data: {}, error: string, meta: {} }`

---

## 6.1 AUTH ENDPOINTS

```
POST   /auth/register
       Body: { first_name, last_name, email, phone, password, role, referral_code? }
       Returns: { user, access_token, refresh_token }
       Notes: Sends verification email/SMS. referral_code logged if valid.

POST   /auth/login
       Body: { identifier: email|phone, password }
       Returns: { user, access_token, refresh_token }

POST   /auth/refresh
       Body: { refresh_token }
       Returns: { access_token, refresh_token }

POST   /auth/logout
       Body: { refresh_token }
       Returns: { success: true }

POST   /auth/verify-email
       Body: { token }
       Returns: { success: true }

POST   /auth/forgot-password
       Body: { email }
       Returns: { message: "Reset link sent" }

POST   /auth/reset-password
       Body: { token, new_password }
       Returns: { success: true }

POST   /auth/change-password
       Body: { current_password, new_password }
       [Protected]
```

## 6.2 USER & PROFILE ENDPOINTS

```
GET    /users/me
       Returns: full profile based on role (student_profile, staff_profile, etc.)

PATCH  /users/me
       Body: { first_name?, last_name?, avatar?, whatsapp_number?, notification_prefs? }
       Returns: updated user

GET    /users/:id
       [academy_admin only]
       Returns: full user detail

GET    /users
       [academy_admin only]
       Query: ?role=student&school_id=&page=&limit=
       Returns: paginated user list

DELETE /users/:id
       [academy_admin only]
       Returns: { success: true }
```

## 6.3 COURSE ENDPOINTS

```
GET    /courses
       Query: ?phase=1&age=13&active=true
       Returns: [{ id, slug, title, description, price, standard_price, 
                   partner_price, age_min, age_max, deliverable, 
                   total_sessions, cover_image, color_hex, 
                   enrolled_count (public), skills_taught }]

GET    /courses/:slug
       Returns: full course detail + session_sequence + sample_quiz

GET    /courses/:slug/sessions
       Returns: [{ session_number, type, title, outcome, principle, duration }]

POST   /courses
       [academy_admin only]
       Body: full course object
       Returns: created course

PATCH  /courses/:id
       [academy_admin only]
       Returns: updated course

GET    /courses/:id/cohorts
       [engineer, academy_admin]
       Returns: cohorts for this course
```

## 6.4 ENROLLMENT ENDPOINTS

```
POST   /enrollments/calculate
       Body: { course_ids[], promo_code?, referral_code?, school_id? }
       Returns: { 
         line_items: [{ course_id, title, standard_price, discounts[], final_price }],
         subtotal, discount_total, total,
         discount_breakdown: { type, description, amount }[]
       }
       Notes: Applies all discount logic. Only best discount per item (or bundle).

POST   /enrollments/checkout
       Body: { course_ids[], promo_code?, referral_code?, school_id?, payment_method }
       Returns: { order_id, payment_url, amount }
       Notes: Creates order, initiates Paymob payment.

POST   /enrollments/payment-webhook
       [Paymob webhook — internal]
       Notes: Confirms payment, activates enrollment, triggers notifications.

GET    /enrollments/my
       [student]
       Returns: [{ course, status, cohort, progress_summary, enrolled_at }]

GET    /enrollments/:order_id
       Returns: order detail + payment status

GET    /enrollments
       [academy_admin]
       Query: ?status=paid&course_id=&school_id=&date_from=&date_to=
       Returns: paginated enrollment list + revenue summary
```

## 6.5 COHORT & SESSION ENDPOINTS

```
GET    /cohorts
       Query: ?course_id=&status=active&engineer_id=
       Returns: cohort list

POST   /cohorts
       [academy_admin, engineer]
       Body: { course_id, cohort_name, engineer_id, cta_id, start_date, max_students }
       Returns: created cohort

GET    /cohorts/:id
       Returns: cohort detail + enrolled students + session schedule

GET    /cohorts/:id/schedule
       Returns: [{ session_instance_id, scheduled_at, type, title, status, recording_url }]

POST   /cohorts/:id/sessions
       [engineer, academy_admin]
       Body: { course_session_id, scheduled_at, session_link }
       Returns: created session_instance

PATCH  /sessions/:id
       [engineer]
       Body: { status?, recording_url?, notes?, session_link? }
       Returns: updated session

POST   /sessions/:id/attendance
       [engineer, cta]
       Body: { attendance: [{ student_id, status, joined_at?, left_at? }] }
       Returns: { saved_count, xp_awarded_total }

GET    /sessions/:id/attendance
       [engineer, cta, school_admin]
       Returns: attendance list with student details

GET    /students/:id/attendance
       [student (own only), parent, engineer, school_admin]
       Returns: attendance summary + per-session breakdown + attendance_rate
```

## 6.6 TASK ENDPOINTS

```
GET    /tasks
       Query: ?cohort_id=&student_id=&status=pending&overdue=true
       Returns: task list with submission status for requesting student

POST   /tasks
       [engineer, academy_admin]
       Body: { course_session_id, cohort_id?, title, description, task_type,
               submission_type, max_score, xp_reward, due_hours_after_session,
               rubric?, is_required }
       Returns: created task
       Notes: Auto-calculates due_at based on session date.

GET    /tasks/:id
       Returns: task detail + student's own submission (if student role)

POST   /tasks/:id/submit
       [student]
       Body: { submission_url?, submission_text? }
       Returns: { submission_id, submitted_at, is_late }
       Notes: Awards partial XP for on-time submission even before grading.

PATCH  /tasks/:id/submissions/:submission_id/grade
       [cta, engineer]
       Body: { score, feedback, rubric_scores?: {} }
       Returns: updated submission + xp_awarded
       Notes: Fires XP transaction, triggers student notification.

GET    /tasks/:id/submissions
       [engineer, cta]
       Returns: all submissions for this task + grading status

GET    /students/:id/tasks
       Returns: task completion summary + per-task breakdown
```

## 6.7 QUIZ ENDPOINTS

```
GET    /quizzes
       Query: ?cohort_id=&course_id=&available_now=true
       Returns: available quizzes (questions hidden, only metadata)

POST   /quizzes
       [engineer, academy_admin]
       Body: { course_session_id, title, quiz_type, time_limit_minutes,
               max_attempts, pass_score, xp_reward, shuffle_questions,
               show_answers_after, available_from, available_until }
       Returns: created quiz

POST   /quizzes/:id/questions
       [engineer]
       Body: [{ question_text, question_type, points, explanation?,
                image_url?, code_snippet?,
                options: [{ option_text, is_correct }] }]
       Returns: created questions

PATCH  /quizzes/:id/publish
       [engineer, academy_admin]
       Returns: { published: true }
       Notes: Triggers notification to all cohort students.

POST   /quizzes/:id/attempts/start
       [student]
       Returns: { attempt_id, questions: [shuffled, no correct answers shown],
                  time_limit_minutes, started_at }

POST   /quizzes/:id/attempts/:attempt_id/submit
       [student]
       Body: { answers: [{ question_id, selected_option_id?, text_answer? }] }
       Returns: { score, percentage, passed, xp_awarded, 
                  correct_count, total_questions,
                  review: [{ question_id, is_correct, points_earned }] }

GET    /quizzes/:id/attempts
       [student] → own attempts only
       [engineer, cta] → all attempts with student breakdown

GET    /quizzes/:id/analytics
       [engineer, academy_admin]
       Returns: { avg_score, pass_rate, question_difficulty[],
                  score_distribution, completion_rate }
```

## 6.8 PROGRESS & GAMIFICATION ENDPOINTS

```
GET    /students/:id/progress
       Returns: {
         xp_total, xp_this_week, rank_global, rank_course,
         streak_current, streak_longest,
         courses: [{ course_id, title, sessions_attended, tasks_completed,
                     tasks_total, avg_quiz_score, completion_pct }],
         badges: [earned badges],
         badges_next: [next achievable badges + progress toward them],
         project: { submitted, public_url }
       }

GET    /leaderboard
       Query: ?scope=global&scope_id=&limit=10&student_id=
       Returns: { 
         leaderboard: [{ rank, student: {name, avatar, school}, xp, badges_count }],
         my_rank: { rank, xp, percentile }  -- if student token
       }

GET    /badges
       Returns: all badges (public) with earned=true/false for authenticated student

GET    /students/:id/badges
       Returns: earned badges with earned_at

GET    /students/:id/xp-history
       Query: ?page=&limit=20
       Returns: XP transaction log

POST   /admin/xp/award
       [academy_admin]
       Body: { student_id, amount, description }
       Returns: transaction record
```

## 6.9 REFERRAL & PROMO ENDPOINTS

```
GET    /referral/my-code
       [student]
       Returns: { code, referral_link, stats: { used_count, rewarded_count, xp_earned } }

POST   /referral/validate
       Body: { code }
       Returns: { valid: bool, referrer_name?, discount_preview? }

GET    /referral/leaderboard
       Returns: top referrers (name, count) — public social proof

POST   /promo-codes
       [academy_admin]
       Body: { code, description, discount_type, discount_value, max_uses?,
               valid_from, valid_until, applies_to, course_id?, school_id? }
       Returns: created promo code

GET    /promo-codes
       [academy_admin]
       Returns: all codes with usage stats

PATCH  /promo-codes/:id
       [academy_admin]
       Body: { is_active?, valid_until?, max_uses? }

POST   /promo-codes/validate
       Body: { code, course_ids[], school_id? }
       Returns: { valid, discount_type, discount_value, 
                  applies_to, description, expires_at }
```

## 6.10 PROJECT SHOWCASE ENDPOINTS

```
POST   /projects
       [student]
       Body: { title, description, course_id, project_url?, demo_video_url?,
               thumbnail_url?, is_public }
       Returns: created project

GET    /projects/showcase
       Query: ?course_id=&featured=true&page=&limit=
       Returns: public projects (only is_public=true) with student info

GET    /projects/:id
       Returns: project detail

POST   /projects/:id/like
       [any authenticated user]
       Returns: { likes_count }

PATCH  /projects/:id/feature
       [academy_admin]
       Body: { featured: true }
       Returns: updated project
```

## 6.11 SCHOOL ADMIN ENDPOINTS

```
GET    /schools
       [academy_admin]
       Returns: all partner schools with stats

POST   /schools
       [academy_admin]
       Body: { name, type, city, email, phone, discount_rate, bundle_discount }
       Returns: created school

GET    /schools/:id/students
       [school_admin (own school only), academy_admin]
       Returns: enrolled students from this school + progress summary

GET    /schools/:id/report
       [school_admin, academy_admin]
       Returns: {
         enrolled_count, active_count, completion_rate,
         avg_attendance, avg_quiz_score, top_students[],
         course_breakdown: [{ course, enrolled, completed }]
       }
       Notes: PDF export endpoint: GET /schools/:id/report?format=pdf

GET    /schools/my
       [school_admin]
       Returns: own school detail + above report
```

## 6.12 NOTIFICATION ENDPOINTS

```
GET    /notifications
       Query: ?unread=true&page=&limit=20
       Returns: notification list

PATCH  /notifications/:id/read
       Returns: { read: true }

PATCH  /notifications/read-all
       Returns: { updated_count }

POST   /admin/notifications/broadcast
       [academy_admin]
       Body: { audience: 'all'|'course'|'cohort'|'school', 
               audience_id?, title, body, channels: ['email','whatsapp'] }
       Returns: { queued_count }
```

---

# 7. FEATURE DEEP-DIVES

## 7.1 Authentication & Onboarding

### Registration Flow
1. Student enters: name, age, school, phone number, parent phone
2. System checks if school is a partner → applies partner discount automatically
3. OTP sent to phone (WhatsApp preferred, SMS fallback)
4. Unique referral code auto-generated (e.g., `YOUSSEF-K7X2`)
5. Welcome notification sent to parent's number automatically

### Role-Based Dashboards
Each role sees a completely different home screen:
- **Student**: XP bar, streak, upcoming session countdown, pending tasks, leaderboard position
- **Parent**: Child's attendance rate, last session summary, upcoming session time, payment status
- **Engineer**: Today's session, cohort attendance, pending task grades, quiz analytics
- **CTA**: Upcoming support session, tasks to grade, student questions
- **School Admin**: School enrollment count, attendance heatmap, top performers, progress report download
- **Academy Admin**: Platform KPIs, revenue dashboard, enrollment funnel, promo code manager

---

## 7.2 Course & Curriculum Engine

### Course Card (public-facing)
Each course card must show:
- Icon, color, title, age range, price
- Skills chips (e.g., "Loops · Functions · OOP")
- Final project deliverable ("You'll build: A playable SFML game")
- Session count (12 sessions: 8 core + 4 support)
- Partner badge if school discount applies
- Enroll CTA with price calculator

### Session Progress Tracker
Visual 12-step roadmap per course (like a game level map):
- Completed sessions: filled circle with checkmark
- Current session: pulsing highlight
- Future sessions: locked icon
- TS sessions: distinct color/icon
- Hover/click → session title and outcome

---

## 7.3 Live Session Management

### Session Lobby (30 min before session)
- Countdown timer
- "What we'll build today" preview
- Last session recap card
- Quick warm-up question (optional 1-question poll)

### During Session (Engineer View)
- Attendance quick-mark (present/late/absent per student)
- Live poll tool (multiple choice, word cloud)
- Raise-hand queue for student questions
- Timer for session segments
- Notes field (synced to session record)

### Post-Session Auto-Actions
After engineer marks session as completed:
1. Task for this session automatically activated with due date
2. Quiz (if any) unlocked for students
3. Recording URL field prompted
4. Session recap notification sent to students and parents
5. XP awarded for attendance (50 XP for present, 25 for late)

---

## 7.4 Tasks & Assignments

### Task Difficulty Indicators
Every task tagged: Warm-up (⭐) / Standard (⭐⭐) / Challenge (⭐⭐⭐)

### Submission UX
- Paste a link (Scratch project link, GitHub, Replit, deployed app)
- Upload a file (image of circuit, zip of code)
- Type a text response (reflection questions)

### CTA Grading Interface
Grid view of all submissions for a task:
- Red = not submitted / overdue
- Yellow = submitted, ungraded
- Green = graded
Click a student → submission + rubric form side by side
Remark field auto-populates with praise + improvement note template

### Late Submission Policy
- On time: 100% XP
- 1-48h late: 75% XP
- 48h+ late: 50% XP, still accepted (never block learning)
- Auto-flag to engineer if student has 2+ consecutive lates

---

## 7.5 Quizzes & Exams Engine

### Question Types Supported
| Type | Example Use |
|------|-------------|
| MCQ (4 options) | "What does a for loop do?" |
| True/False | "Variables must be declared before use: T/F" |
| Code Output | Show code snippet → "What does this print?" |
| Short Answer | "In your own words, what is a class?" (manual grade) |
| Matching | Match concept to definition |

### Live Quiz Mode (optional)
Engineer can launch a "live quiz" during session:
- All students answer simultaneously
- Real-time results shown on screen
- Leaderboard flashes after each question
- Similar to Kahoot — boosts engagement dramatically

### Quiz Feedback Design
After submission, student sees:
- Score badge (A / B / C / Retry)
- "Well done! You got 8/10" with a personalized message
- Per-question breakdown: ✓ or ✗ with explanation
- XP earned animation
- "What to review" suggestion → links to session recording

---

## 7.6 Progress & Badge System

### XP Earning Events
| Action | XP |
|--------|-----|
| Attend session (on time) | 50 XP |
| Attend session (late) | 25 XP |
| Submit task (on time) | 30 XP |
| Task graded: full score | +70 XP bonus |
| Task graded: 50-99% | +35 XP bonus |
| Pass quiz | 100 XP |
| Quiz perfect score | +50 XP bonus |
| 7-day streak | 200 XP |
| Refer a friend who enrolls | 300 XP |
| Project submitted | 150 XP |
| Project featured | 500 XP |
| Complete full course | 1000 XP |

### Badge Catalogue (Phase 1)

| Badge | Trigger |
|-------|---------|
| 🚀 First Launch | Complete first session |
| 🔥 On Fire | 7-day streak |
| 💎 Perfect Score | 100% on any quiz |
| ⚡ Speed Coder | Submit task within 2h of session |
| 🤝 Ambassador | Refer 1 enrolled student |
| 👑 Top Recruiter | Refer 5 enrolled students |
| 🛠️ Builder | Submit first project |
| 🌟 Featured | Have a project featured by admin |
| 📚 Full Course | Complete all 12 sessions of a course |
| 🤖 Robot Maker | Complete Electronics & Robot course |
| 🎮 Game Dev | Complete Advanced C++ course |
| 🌐 Deployed | Deploy web app (Web App AI course) |
| 💯 100 Club | Reach 1,000 XP |
| 🏆 Legend | Reach 5,000 XP |
| 👀 Never Miss | 100% attendance in a course |
| 🧑‍🏫 Mentor | CTA who grades 50+ tasks |

---

## 7.7 Gamification & Leaderboards

### Leaderboard Scopes
1. **Global** — all students on the platform
2. **Per Course** — all students in same course
3. **Per Cohort** — your exact classmates
4. **Per School** — students from your school

Design principle: Always show "your rank" prominently even if you're #47. Show how many XP to reach the next rank.

### Streak System
- Streak increments when student does ANY of: attends session, submits task, completes quiz
- Streak freezes available: 1 freeze earned per 14-day streak (used when student is absent)
- Streak lost → empathetic message ("Don't give up! Start again today") + 50 XP re-engagement bonus

### Weekly Challenges
Every Monday, a new platform-wide challenge appears:
- "Submit all tasks this week → 500 XP bonus"
- "Be the first in your cohort to complete the quiz → 200 XP"
- "Get a perfect attendance this week → 300 XP"

### Level System
| Level | XP Range | Title |
|-------|----------|-------|
| 1 | 0–199 | Beginner Coder |
| 2 | 200–499 | Apprentice Builder |
| 3 | 500–999 | Junior Developer |
| 4 | 1,000–2,499 | Coder |
| 5 | 2,500–4,999 | Senior Builder |
| 6 | 5,000–9,999 | Engineer |
| 7 | 10,000+ | GenZ Legend |

---

## 7.8 Referral System (Advanced)

### How It Works
1. Every student gets a unique code (e.g., `YOUSSEF-K7X2`) immediately on registration
2. Code is shown prominently on their dashboard with a share button
3. Referred friend registers with code → referrer gets a badge + 100 XP
4. Referred friend **enrolls and pays** → referrer gets 300 XP + a discount credit (100 EGP) for their next purchase
5. Referred friend gets 10% off their enrollment

### Referral Dashboard (student view)
- Total referrals made
- Pending (registered but not yet enrolled)
- Rewarded (enrolled and paid)
- Total XP earned from referrals
- Total discount credits earned
- Share link → auto-copies `https://genzcoders.academy/join?ref=YOUSSEF-K7X2`

### Referral Leaderboard (public)
Top 10 referrers shown publicly → huge social motivation. Top referrer each month gets a "Free Course" reward.

### Virality Trigger
When a referral completes enrollment, send the referrer:
- In-app notification: "🎉 [Friend's name] just joined! You earned 300 XP + 100 EGP credit!"
- WhatsApp message to the referrer

---

## 7.9 Promo Codes & Discounts

### Discount Priority Rules (only one applies per item)
```
Priority 1: Partnership Bundle (school partner, all 5 courses) → 25% off
Priority 2: Partner School (per course) → 15% off
Priority 3: Promo Code (if higher than partner) → code value
Priority 4: Early Bird → 10% off (if still active)
Priority 5: Referral → 10% off
Priority 6: Sibling → 10% off
```
Partnership bundle supersedes everything. System calculates best deal automatically.

### Admin Promo Code Builder
Fields: Code, Description, Type (% or fixed EGP), Value, Max uses, Expiry, Applies to (any / specific course / bundle), School restriction, Active toggle.

Use cases:
- `IATS2026` → 20% off for IATS school students
- `ROBOT50` → 50 EGP off Robot course only, 30 uses max
- `SUMMER25` → 25% off bundle, no expiry
- `RAMADAN10` → 10% off, valid for 3 days

---

## 7.10 CTA Portal

CTAs (Co-Teaching Assistants) are selected students from ElSewedy school. Their portal includes:

### CTA Dashboard
- Today's support session (link, student list, topics to cover)
- Task grading queue (sorted by most urgent)
- Student questions inbox (from WhatsApp/platform)
- Own CTA XP and rank on the CTA leaderboard

### CTA Grading Workflow
1. Open task → see all submissions in grid
2. Click student submission → view file/link in-window
3. Fill rubric sliders → total auto-calculated
4. Write feedback (templates available: "Great job on X, improve Y")
5. Submit grade → student notified immediately

### CTA Recognition
- CTAs have a separate leaderboard
- Top CTA each month featured on social media
- CTAs earn "Mentor Badges" for milestones
- Phase 2: Top CTAs promoted to "Junior Instructor" with pay

---

## 7.11 School Admin Portal

School coordinator gets a clean read-only (+ some actions) view of their school's students.

### School Dashboard Shows:
- Total enrolled students (by course)
- Attendance rate this week
- Tasks submitted rate
- Top 5 students (XP leaderboard, school-scoped)
- Students who haven't attended in 2+ sessions (at-risk alert)

### Reports
- Weekly PDF report (auto-generated every Sunday)
- Per-student: attendance, tasks, quiz scores, XP, badges
- Downloadable for sharing with school principal

---

## 7.12 Parent Dashboard

Parent links their account to their child's account during enrollment.

### Parent Dashboard Shows:
- Child's photo + name + current course(s)
- Attendance this week (session by session: ✓ / ✗)
- Next session: date + time + join link
- Last task: submitted on time? Score?
- Current XP and rank
- Upcoming payment (if multi-course or installments)
- Message the team button (routes to WhatsApp)

### Parent Notifications (WhatsApp by default)
- Session reminder 2h before
- Session attended confirmation
- Task submitted / not submitted (24h warning)
- Grade received
- Badge earned ("🏅 Your child just earned the 'First Launch' badge!")
- Monthly progress summary

---

## 7.13 Notification System

### Trigger-Based Notifications

| Trigger | Channels | Audience |
|---------|----------|----------|
| Session starts in 2h | WhatsApp + Push | Student + Parent |
| Session marked complete | Push | Student |
| Task created | Push + Email | Student |
| Task due in 24h (not submitted) | WhatsApp | Student + Parent |
| Task graded | Push | Student |
| Quiz published | Push | Student |
| Badge earned | In-app + Push | Student |
| New leaderboard rank | In-app | Student |
| Referral converted | WhatsApp | Referrer |
| Weekly progress report | Email | Parent + School Admin |
| Promo code expiry reminder | WhatsApp | Admin-defined list |

---

# 8. SESSION ENGAGEMENT DESIGN

## The Problem with Boring Sessions
Students drop off when sessions feel like school lectures. The goal is to make every 90 minutes feel like a game with clear progress.

## The Engagement Framework: BUILD → CHALLENGE → SHARE

### Every Core Session Must Have These 5 Elements:

**1. HOOK (0–5 min)**
Start with a real-world demo or question that surprises them.
- "Did you know the game Minecraft is written in Java? Today you'll learn the same logic."
- Show the finished project they'll build this session. Make them want it.

**2. LIVE BUILD (5–60 min)**
Engineer builds step-by-step — students build along in real time.
- Short micro-challenges every 15 min: "Pause. Add a score counter. You have 5 minutes. Go."
- Engineer monitors screens via Zoom "Request view" or shares solution after.

**3. MINI COMPETITION (60–75 min)**
A short live challenge where students compete or collaborate:
- "First one to add a second level wins 50 XP bonus"
- "Improve the game in any way you want — most creative change wins"
- Live quiz (2–3 questions, Kahoot-style)

**4. SHOWCASE (75–85 min)**
2-3 students share their screen and show what they built.
- Peers react with emoji in chat
- Engineer gives specific praise + one improvement tip

**5. MISSION BRIEF (85–90 min)**
End with the task assignment — make it feel like a mission, not homework:
- "Your mission before next session: add X. Submit your link by [date]."
- Show the XP they'll earn for completing it.

## Technical Support Session Design (4h)
These are CTA-led. Structure:
- **Hour 1**: Recap + fix blockers from last week
- **Hour 2**: Guided challenge (extend the project with a new feature)
- **Hour 3**: Open lab (student choice — build something extra)
- **Hour 4**: Show & tell (share screens, peer feedback, celebrate)

## Anti-Boring Rules
- No session longer than 90 min without a break
- Minimum 1 interactive element every 15 minutes
- Never read slides → always build live
- Engineer must know every student's name by Session 2
- If a student is struggling, CTA takes them to a breakout room — session moves on
- End every session with "Next session you'll build [specific exciting thing]"

---

# 9. MARKETING & VIRALITY STRATEGY

## 9.1 The Growth Model

### Acquisition Channels (ranked by cost-effectiveness)
1. **Referral (students/parents)** — highest conversion, zero cost
2. **School partnerships** — bulk enrollment, credibility
3. **WhatsApp group seeding** — organic, high trust
4. **Instagram Reels** — reach teens directly
5. **Facebook groups** — reach parents
6. **IATS school Facebook page** — existing audience

### The Core Viral Loop
```
Student enrolls
  → Gets referral code
    → Shares with friends
      → Friends get discount + enroll
        → Student gets XP + credit
          → Student shares their project publicly
            → Project gets seen → more interest → loop repeats
```

### Virality Multipliers
- **Public project showcase** page (`genzcoders.academy/showcase`) — students share their live app/game URL on social media. This is the most powerful organic acquisition.
- **Badge sharing** — "I just earned the 🤖 Robot Maker badge on ElSewedy GenZ Coders!" shareable image
- **Leaderboard screenshots** — students naturally screenshot and share their rank
- **Certificate on course completion** — shareable PDF + LinkedIn-ready image

## 9.2 Pre-Launch Virality (Now → 8 June)

### Phase A: FOMO Creation (12–20 May)
- Post countdown: "Opening in X days" with a teaser — no course details yet
- Create the itch: "Something is launching for Egypt's next generation of builders"
- Run a "Guess what we're building" contest → winner gets free enrollment
- Target: 50 followers before full reveal

### Phase B: Full Reveal (20–25 May)
- Full course reveal + pricing post
- Early Bird countdown (ends 25 May) — create urgency
- Engineer intro Reels: 30-second each, Alaa / Fady / Aya talking about their specialization (not scripted — authentic)
- Post student project mockups: "This is what students will build this summer"

### Phase C: Social Proof Sprint (25 May – 7 June)
- "X students already registered!" updated every 3-5 days
- Screenshot real WhatsApp enthusiasm (with permission)
- School visit recaps: "We visited [school] today — students are excited"
- Engineer Q&A on Instagram Stories (question box)

### Phase D: Launch Day Maximization (8 June)
- Go live on Instagram during Session 1 (brief, 2-3 minutes)
- Post student screenshots after Session 1 with permission
- First Day recap Reel posted within 2 hours of session end
- Message every enrolled student's parent individually — make them feel special

## 9.3 Ongoing Content Strategy

### Instagram (primary — teens)
**Reels**: 3× per week
- Format 1: "In this course, students will build [X]" — show a demo
- Format 2: Student success / progress clip
- Format 3: Behind the scenes — CTAs, engineers prepping

**Stories**: Daily during active enrollment periods
- Polls ("Which course would you pick?")
- Quizzes ("True or false: you need to know math to code")
- Countdown stickers for session days
- Question box (engineer answers in next story)

**Feed Posts**: 2× per week
- Course spotlight (one course per week, rotating)
- Badge/achievement celebration ("50 students enrolled!")
- Team content

### Facebook (primary — parents)
**Posts**: 3× per week
- Longer format — explain the "why" of each course
- Parent testimonials (after Phase 1)
- Safety and structure reassurance posts
- Payment and enrollment how-to posts

### WhatsApp (highest conversion)
- 3-4 broadcasts per week maximum
- Separate groups: Parents | Students | School Coordinators
- Content: session links, reminders, XP announcements, enrollment nudges
- Personal responses to every parent who messages

### Content Pillars (for all channels)
| Pillar | What to Post | Frequency |
|--------|-------------|-----------|
| 🚀 Inspire | "Students who learn this become X" — aspirational | 2× week |
| 🛠️ Show the work | Project demos, student builds, before/after | 2× week |
| 👤 Trust | Engineer profiles, school partnerships, parent quotes | 1× week |
| 🎓 Educate | "What is Scratch?", "Why C++?" — demystify | 1× week |
| 🔥 Urgency | Countdown, spots filling, early bird ending | Daily last 7 days |

## 9.4 School Partnership Activation as a Marketing Channel

Each partner school is a distribution node. Activate each one:
1. Provide a branded WhatsApp message they can forward to all parent groups
2. Provide a story graphic they can post on their school Instagram
3. Provide a print-ready A4 poster for notice boards
4. Give school coordinator a unique promo code for tracking (e.g., `STEM2026`) → 15% off for all students who use it

## 9.5 Paid Growth (Phase 2 Budget, Optional)
If organic hits ceiling:
- Instagram/Facebook ads: target parents in Cairo aged 30-50 with children interest
- Google Search: "تعلم البرمجة للأطفال في مصر"
- Retargeting: anyone who visited the website but didn't enroll

## 9.6 Retention & Re-Enrollment Strategy

After Phase 1 ends, convert graduates to Phase 2:
- "Alumni Early Access" — email/WhatsApp offer 48h before public Phase 2 launch
- Graduates who refer a new student get first month of Phase 2 free
- "Your child completed Robot Build — the next level is Embedded Systems" (parent message)
- Certificate + showcase post creates an emotional milestone that makes dropping out feel costly

---

# 10. CONTENT CALENDAR

## Pre-Launch (12 May – 7 June 2026)

### Week 1 (12–16 May): Brand Launch
| Day | Platform | Content |
|-----|----------|---------|
| Mon 12 | FB + IG | Academy launch announcement post. All 5 courses. Launch date. Enrollment link. |
| Tue 13 | IG Story | Poll: "Which course would you take? 🎮🤖💻🔧" |
| Wed 14 | FB | Intro to engineering team: Alaa, Fady, Aya |
| Thu 15 | IG Reel | 30s: "What you'll build this summer" — montage of project types |
| Fri 16 | FB + IG | Scratch course spotlight — who it's for, what they build |
| WhatsApp | — | Welcome messages to all groups. Pin enrollment form. |

### Week 2 (17–23 May): Course Reveal + School Visits
| Day | Platform | Content |
|-----|----------|---------|
| Mon 17 | FB + IG | Intro C++ spotlight + IATS school visit recap |
| Tue 18 | IG Story | "Meet your CTA" — introduce one CTA with a short intro |
| Wed 19 | FB | Advanced C++ spotlight — OOP, SFML game |
| Thu 20 | IG Reel | "Why C++ matters for your future" — Alaa 30s video |
| Fri 21 | FB + IG | Robot Build spotlight — circuit to robot journey |
| WhatsApp | — | Share course schedule PDF. Invite reactions. |
| Sat 22 | IG Story | Q&A story — "Ask our engineers anything" |

### Week 3 (24–30 May): Social Proof + Early Bird End
| Day | Platform | Content |
|-----|----------|---------|
| Mon 24 | FB + IG | Web App AI spotlight + "first 20 students get 10% off — ends tomorrow" |
| Tue 25 | FB + IG | LAST CHANCE — Early Bird closing tonight. Countdown graphic. |
| Wed 26 | FB | "20 students already registered!" social proof post |
| Thu 27 | IG Reel | Behind the scenes — CTAs in training |
| Fri 28 | FB + IG | School partnership announcement — "Proud to partner with [School]" |
| WhatsApp | — | FAQ session. Aya answers parent questions live in group. |

### Week 4 (31 May – 6 June): Maximum Urgency
| Day | Platform | Content |
|-----|----------|---------|
| Mon 31 | FB + IG | "8 days to launch" — countdown graphic with all 5 courses |
| Tue 1 Jun | FB | "Meet your engineer" — Alaa feature |
| Wed 2 | IG Story | Countdown: 6 days. Enrollment closes 7 June. |
| Thu 3 | FB | "Meet your engineer" — Fady feature |
| Fri 4 | IG + FB | "Meet your engineer" — Aya feature |
| Sat 5 | IG Reel | "This is what Day 1 will look like" — preview |
| Sun 6 | FB + IG | "Tomorrow is the last day to enroll" — final urgency |
| WhatsApp | — | Final enrollment push. Session link + checklist. |

### Launch Week (7–8 June)
| Day | Content |
|-----|---------|
| Sun 7 | "Enrollment CLOSES today" — all channels. |
| Mon 8 | Launch day post + "We are LIVE". Tag all partner schools. Post session highlights story. Recap Reel after session. |

---

## Post-Launch (Ongoing, June–August)
- Weekly session recap post (show what students built this week)
- Weekly top performer shoutout (with student permission)
- CTA spotlight rotation (one per month)
- Monthly "Student of the Month" with project feature
- Milestone posts: "100 sessions delivered!" / "First robot built!"

---

# 11. LAUNCH ROADMAP

## Development Phases for the Platform

### MVP (Launch Day, 8 June 2026)
Must be live on Day 1:
- Student registration + profile
- Course listing page (public) with enrollment form/payment
- Cohort schedule view (upcoming sessions)
- WhatsApp/email notification for session reminder
- Basic attendance marking (engineer tool)
- Task submission (link/text)
- Admin dashboard (Alaa) — enrollment list, revenue

### Phase 1.5 (July 2026)
- Parent dashboard
- Quiz engine (basic MCQ)
- XP + badge system
- Leaderboard
- Progress dashboard for students

### Phase 2.0 (September 2026)
- Full referral code system with rewards
- Promo code builder (admin)
- School admin portal
- Public project showcase
- Live quiz (Kahoot-style)
- CTA grading portal
- Streak system + weekly challenges
- Certificate generation

### Phase 2.5 (Q4 2026)
- Mobile app (React Native)
- AI-powered learning suggestions ("You struggled with loops — here are 3 practice questions")
- Parent weekly report automation (PDF)
- Advanced analytics for academy admin

---

## Critical Technical Decisions for Day 1

1. **Use Google Forms for enrollment initially** — get paying students first, build the platform second.
2. **Use Zoom for sessions** — don't build video infrastructure. Embed or just share links.
3. **Use WhatsApp Business** for all notifications — highest open rate in Egypt.
4. **Use Paymob** for payments — best Egyptian market support, easy integration.
5. **Deploy on Railway or Render** — no DevOps needed, auto-deploy from GitHub.
6. **PostgreSQL on Neon.tech** — free tier, scales, managed.

---

*ElSewedy GenZ Coders — Full Product Specification v1.0*
*Prepared: May 2026 | Confidential Internal Document*
*"Build skills. Track progress. Launch careers."*
