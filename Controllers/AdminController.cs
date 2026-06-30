using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = AcademyRole.AcademyAdmin)]
public class AdminController(
    IAdminDashboardService dashboardService,
    AcademyDbContext db,
    IZoomService zoomService) : ControllerBase
{
    // ── Dashboard ──────────────────────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => Ok(await dashboardService.GetDashboardAsync(cancellationToken));

    // ── Database Repair (DEV ONLY) ──────────────────────────────────────────
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("fix-db")]
    public async Task<IActionResult> FixDb()
    {
        if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
            return NotFound();
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE CourseApplications ADD PaymentMethod nvarchar(max) NULL;"); } catch {}
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE CourseApplications ADD PaymentReceiptPendingReview bit NOT NULL DEFAULT 0;"); } catch {}
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE CourseApplications ADD PaymentReceiptUrl nvarchar(max) NULL;"); } catch {}

        return Ok("Database schema patched successfully.");
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
    {
        var courses = await db.Courses
            .AsNoTracking()
            .Include(x => x.Modules.OrderBy(m => m.SortOrder))
            .OrderBy(x => x.SortOrder)
            .Select(c => new AdminCourseDto(
                c.Id, c.Slug, c.Title, c.Subtitle, c.Description, c.ShortDescription,
                c.Outcome, c.MinimumAge, c.MaximumAge, c.PriceEgp, c.CoverImageUrl, c.ImageUrl,
                c.IconName, c.ColorHex, c.SkillsTaughtJson, c.Phase, c.SortOrder,
                c.CoreSessions, c.SupportSessions, c.Level, c.IsActive, c.IsFeatured, c.IsDeleted,
                c.Modules.OrderBy(m => m.SortOrder)
                         .Select(m => new CourseModuleDto(m.SortOrder, m.Title, m.ProjectOutcome))
                         .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("courses/{courseId}")]
    public async Task<IActionResult> GetCourse(string courseId, CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Include(x => x.Modules.OrderBy(m => m.SortOrder))
            .FirstOrDefaultAsync(x => x.Id.ToString() == courseId || x.Slug == courseId, cancellationToken);

        if (course is null) return NotFound(new { message = "Course not found" });

        var roundsCount = await db.Cohorts.CountAsync(x => x.CourseId == course.Id, cancellationToken);
        var enrollmentsCount = await db.CohortEnrollments.CountAsync(x => x.Cohort.CourseId == course.Id, cancellationToken);
        var materialsCount = await db.CourseMaterials.CountAsync(x => x.CourseId == course.Id, cancellationToken);
        var sessionsCount = await db.SessionInstances.CountAsync(x => x.Cohort.CourseId == course.Id, cancellationToken);
        var activeRoundsCount = await db.Cohorts.CountAsync(x => x.CourseId == course.Id && x.Status == CohortStatus.Active, cancellationToken);

        return Ok(new
        {
            course.Id,
            course.Slug,
            course.Title,
            course.Subtitle,
            course.Description,
            course.ShortDescription,
            course.Outcome,
            course.MinimumAge,
            course.MaximumAge,
            course.PriceEgp,
            course.CoverImageUrl,
            course.ImageUrl,
            course.IconName,
            course.ColorHex,
            course.SkillsTaughtJson,
            course.Phase,
            course.SortOrder,
            course.CoreSessions,
            course.SupportSessions,
            course.Level,
            course.IsActive,
            course.IsFeatured,
            course.IsDeleted,
            Modules = course.Modules.OrderBy(m => m.SortOrder)
                .Select(m => new CourseModuleDto(m.SortOrder, m.Title, m.ProjectOutcome))
                .ToList(),
            RoundsCount = roundsCount,
            ActiveRoundsCount = activeRoundsCount,
            EnrollmentsCount = enrollmentsCount,
            MaterialsCount = materialsCount,
            SessionsCount = sessionsCount
        });
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse(AdminCourseCreateRequest request, CancellationToken cancellationToken)
    {
        if (await db.Courses.AnyAsync(c => c.Slug == request.Slug, cancellationToken))
            return Conflict(new { message = "A course with this slug already exists." });

        var course = new Course
        {
            Slug            = request.Slug.Trim().ToLowerInvariant(),
            Title           = request.Title,
            Subtitle        = request.Subtitle,
            Description     = request.Description,
            ShortDescription = request.ShortDescription,
            Outcome         = request.Outcome,
            MinimumAge      = request.MinimumAge,
            MaximumAge      = request.MaximumAge,
            PriceEgp        = request.PriceEgp,
            CoverImageUrl   = request.CoverImageUrl,
            ImageUrl        = request.ImageUrl,
            IconName        = request.IconName,
            ColorHex        = request.ColorHex,
            SkillsTaughtJson = request.SkillsTaughtJson ?? "[]",
            Phase           = request.Phase,
            SortOrder       = request.SortOrder,
            CoreSessions    = request.CoreSessions,
            SupportSessions = request.SupportSessions,
            Level           = request.Level,
            IsActive        = request.IsActive,
            IsFeatured      = request.IsFeatured,
            IsDeleted       = false
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        // Save sessions if any passed
        if (request.CourseSessions != null)
        {
            foreach (var s in request.CourseSessions)
            {
                db.CourseSessions.Add(new CourseSession
                {
                    CourseId = course.Id,
                    Title = s.Title,
                    Description = s.Description ?? "",
                    SessionNumber = s.SessionNumber,
                    DurationMinutes = s.DurationMinutes,
                    SessionType = Enum.TryParse<SessionType>(s.SessionType, out var st) ? st : SessionType.Core,
                    SortOrder = s.SortOrder
                });
            }
        }

        // Save lessons if any passed
        if (request.Lessons != null)
        {
            foreach (var l in request.Lessons)
            {
                db.CourseLessons.Add(new CourseLesson
                {
                    CourseId = course.Id,
                    Title = l.Title,
                    Summary = l.Summary,
                    ContentMarkdown = l.ContentMarkdown,
                    WeekNumber = l.WeekNumber,
                    SessionType = Enum.TryParse<SessionType>(l.SessionType, out var st) ? st : SessionType.Core,
                    SortOrder = l.SortOrder,
                    IsPublished = true
                });
            }
        }

        // Save materials if any passed
        if (request.Materials != null)
        {
            foreach (var m in request.Materials)
            {
                db.CourseMaterials.Add(new CourseMaterial
                {
                    CourseId = course.Id,
                    Title = m.Title,
                    MaterialType = Enum.TryParse<CourseMaterialType>(m.MaterialType, out var mt) ? mt : CourseMaterialType.Pdf,
                    Url = m.Url,
                    Description = m.Description,
                    IsDownloadable = m.IsDownloadable,
                    IsPublished = true
                });
            }
        }

        // Save questions if any passed
        if (request.Questions != null)
        {
            foreach (var q in request.Questions)
            {
                db.CourseApplicationQuestions.Add(new CourseApplicationQuestion
                {
                    CourseId = course.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = Enum.TryParse<ApplicationQuestionType>(q.QuestionType, out var qt) ? qt : ApplicationQuestionType.Mcq,
                    OptionsJson = q.OptionsJson ?? "[]",
                    CorrectAnswer = q.CorrectAnswer,
                    IsRequired = q.IsRequired,
                    SortOrder = q.SortOrder,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { course.Id, course.Slug });
    }

    [HttpPut("courses/{id:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid id, AdminCourseCreateRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FindAsync([id], cancellationToken);
        if (course is null) return NotFound();

        course.Title            = request.Title;
        course.Subtitle         = request.Subtitle;
        course.Description      = request.Description;
        course.ShortDescription = request.ShortDescription;
        course.Outcome          = request.Outcome;
        course.MinimumAge       = request.MinimumAge;
        course.MaximumAge       = request.MaximumAge;
        course.PriceEgp         = request.PriceEgp;
        course.CoverImageUrl    = request.CoverImageUrl;
        course.ImageUrl         = request.ImageUrl;
        course.IconName         = request.IconName;
        course.ColorHex         = request.ColorHex;
        course.SkillsTaughtJson = request.SkillsTaughtJson ?? "[]";
        course.Phase            = request.Phase;
        course.SortOrder        = request.SortOrder;
        course.CoreSessions     = request.CoreSessions;
        course.SupportSessions  = request.SupportSessions;
        course.Level            = request.Level;
        course.IsActive         = request.IsActive;
        course.IsFeatured       = request.IsFeatured;
        if (request.IsDeleted.HasValue) course.IsDeleted = request.IsDeleted.Value;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { course.Id });
    }

    [HttpDelete("courses/{id:guid}")]
    public async Task<IActionResult> ArchiveCourse(Guid id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FindAsync([id], cancellationToken);
        if (course is null) return NotFound();
        course.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { archived = true });
    }

    // ── Cohorts (Course Rounds) ────────────────────────────────────────────
    [HttpGet("rounds")]
    public async Task<IActionResult> GetRounds([FromQuery] string? courseId, CancellationToken cancellationToken)
    {
        var query = db.Cohorts.AsNoTracking().Include(x => x.Course).AsQueryable();
        if (!string.IsNullOrEmpty(courseId))
        {
            if (Guid.TryParse(courseId, out var parsedGuid))
            {
                query = query.Where(x => x.CourseId == parsedGuid);
            }
            else
            {
                query = query.Where(x => x.Course != null && x.Course.Slug == courseId);
            }
        }

        var rounds = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.CourseId,
                CourseTitle         = r.Course == null ? "" : r.Course.Title,
                CourseSlug          = r.Course == null ? "" : r.Course.Slug,
                r.Name,
                r.Slug,
                Status              = r.Status.ToString(),
                StartDate           = r.StartDate.ToString("yyyy-MM-dd"),
                r.MaxStudents,
                CurrentStudents     = r.CohortEnrollments.Count(),
                r.IsEnrollmentOpen,
                r.AutoAcceptPaidApplications,
                r.RequireEngineerApproval,
                r.ZoomMeetingId,
                r.ZoomJoinUrl,
                r.ZoomStartUrl
            })
            .ToListAsync(cancellationToken);

        return Ok(rounds);
    }

    [HttpGet("rounds/{idOrSlug}")]
    public async Task<IActionResult> GetRound(string idOrSlug, CancellationToken cancellationToken)
    {
        try
        {
        var query = db.Cohorts
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.EngineerUser)
            .Include(x => x.CohortEnrollments)
                .ThenInclude(e => e.StudentUser)
            .AsQueryable();

        Cohort? round;
        if (Guid.TryParse(idOrSlug, out var id))
        {
            round = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        else
        {
            round = await query.FirstOrDefaultAsync(x => x.Slug == idOrSlug, cancellationToken);
        }

        if (round is null) return NotFound();

        var roundId = round.Id;

        var weeks = await db.SessionInstances
            .AsNoTracking()
            .Where(x => x.CohortId == roundId)
            .OrderBy(x => x.WeekNumber)
            .Select(w => new
            {
                w.Id,
                w.WeekNumber,
                w.WeekTitle,
                SessionType = w.SessionType.ToString(),
                w.ScheduledAt,
                w.DurationMinutes,
                Status = w.Status.ToString(),
                w.SessionLink,
                w.RecordingUrl
            })
            .ToListAsync(cancellationToken);

        var materials = await db.CourseMaterials
            .AsNoTracking()
            .Where(x => x.CourseId == round.CourseId || x.CohortId == roundId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(m => new
            {
                m.Id, m.Title,
                MaterialType = m.MaterialType.ToString(),
                m.Url, m.IsDownloadable, m.IsPublished
            })
            .ToListAsync(cancellationToken);

        var studentUserIds = round.CohortEnrollments.Select(e => e.StudentUserId).ToList();
        var applicationsMap = await db.CourseApplications
            .AsNoTracking()
            .Where(x => x.CourseId == round.CourseId && studentUserIds.Contains(x.StudentUserId))
            .GroupBy(x => x.StudentUserId)
            .Select(g => g.OrderByDescending(x => x.SubmittedAt).FirstOrDefault())
            .Where(x => x != null)
            .ToDictionaryAsync(x => x!.StudentUserId, x => x!.Id, cancellationToken);

        var students = round.CohortEnrollments.Select(e => new
        {
            e.StudentUserId,
            StudentName = e.StudentUser == null ? "" : (e.StudentUser.FirstName + " " + e.StudentUser.LastName).Trim(),
            Email = e.StudentUser?.Email ?? "",
            e.EnrolledAt,
            ApplicationId = applicationsMap.TryGetValue(e.StudentUserId, out var appId) ? appId : (Guid?)null
        }).ToList();

        var quizzes = await db.Quizzes
            .AsNoTracking()
            .Where(x => x.CohortId == roundId)
            .Select(q => new
            {
                q.Id,
                q.Title,
                QuizType = q.QuizType.ToString(),
                q.TimeLimitMinutes,
                q.MaxAttempts,
                q.PassScore,
                q.XpReward,
                q.IsPublished,
                QuestionsCount = db.Questions.Count(x => x.QuizId == q.Id)
            })
            .ToListAsync(cancellationToken);

        var tasks = await db.LearningTasks
            .AsNoTracking()
            .Where(x => x.CohortId == roundId)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Instructions,
                TaskType = t.TaskType.ToString(),
                SubmissionType = t.SubmissionType.ToString(),
                t.MaxScore,
                t.XpReward,
                t.DueHoursAfterSession
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            round.Id,
            round.CourseId,
            CourseTitle = round.Course?.Title ?? "",
            CourseSlug = round.Course?.Slug ?? "",
            round.Name,
            round.Slug,
            Status = round.Status.ToString(),
            StartDate = round.StartDate.ToString("yyyy-MM-dd"),
            round.MaxStudents,
            round.CurrentStudents,
            round.IsEnrollmentOpen,
            round.AutoAcceptPaidApplications,
            round.RequireEngineerApproval,
            round.ZoomMeetingId,
            round.ZoomJoinUrl,
            round.ZoomStartUrl,
            InstructorName = round.EngineerUser == null ? null : (round.EngineerUser.FirstName + " " + round.EngineerUser.LastName).Trim(),
            Weeks = weeks,
            Materials = materials,
            Students = students,
            Quizzes = quizzes,
            Tasks = tasks
        });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to load round details", error = ex.Message });
        }
    }

    [HttpPost("rounds")]
    public async Task<IActionResult> CreateRound(AdminRoundCreateRequest request, CancellationToken cancellationToken)
    {
        var cohort = new Cohort
        {
            CourseId                  = request.CourseId,
            Name                      = request.Name,
            Slug                      = request.Slug.Trim().ToLowerInvariant(),
            StartDate                 = request.StartDate,
            MaxStudents               = request.MaxStudents,
            IsEnrollmentOpen          = request.IsEnrollmentOpen,
            AutoAcceptPaidApplications = request.AutoAcceptPaidApplications,
            RequireEngineerApproval   = request.RequireEngineerApproval,
            Status                    = CohortStatus.Upcoming
        };

        db.Cohorts.Add(cohort);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { cohort.Id, cohort.Slug });
    }

    [HttpPut("rounds/weeks/{id:guid}")]
    public async Task<IActionResult> UpdateRoundWeek(Guid id, [FromBody] UpdateRoundWeekRequest request, CancellationToken cancellationToken)
    {
        var week = await db.SessionInstances.FindAsync([id], cancellationToken);
        if (week is null) return NotFound();

        week.WeekTitle = request.WeekTitle ?? week.WeekTitle;
        if (Enum.TryParse<SessionType>(request.SessionType, out var st))
            week.SessionType = st;
        if (Enum.TryParse<SessionStatus>(request.Status, out var ws))
            week.Status = ws;
            
        week.SessionLink = request.SessionLink;
        week.RecordingUrl = request.RecordingUrl;
        week.WeekNumber = request.WeekNumber;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { week.Id });
    }

    [HttpPost("rounds/{cohortId:guid}/weeks")]
    public async Task<IActionResult> CreateRoundWeek(Guid cohortId, [FromBody] UpdateRoundWeekRequest request, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts.FindAsync([cohortId], cancellationToken);
        if (cohort is null) return NotFound("Cohort not found");

        var maxWeekNum = await db.SessionInstances
            .Where(x => x.CohortId == cohortId)
            .Select(x => (int?)x.WeekNumber)
            .MaxAsync(cancellationToken) ?? 0;

        Enum.TryParse<SessionType>(request.SessionType, out var st);
        Enum.TryParse<SessionStatus>(request.Status, out var ws);

        var courseSession = await db.CourseSessions.FirstOrDefaultAsync(x => x.CourseId == cohort.CourseId, cancellationToken);
        if (courseSession is null)
        {
            courseSession = new CourseSession
            {
                CourseId = cohort.CourseId,
                Title = request.WeekTitle ?? $"Session {maxWeekNum + 1}",
                SessionType = st,
                DurationMinutes = 120
            };
            db.CourseSessions.Add(courseSession);
            await db.SaveChangesAsync(cancellationToken);
        }

        var instance = new SessionInstance
        {
            CohortId = cohortId,
            CourseSessionId = courseSession.Id,
            WeekNumber = request.WeekNumber > 0 ? request.WeekNumber : (maxWeekNum + 1),
            WeekTitle = request.WeekTitle ?? $"Session {maxWeekNum + 1}",
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(7),
            DurationMinutes = 120,
            SessionLink = request.SessionLink,
            RecordingUrl = request.RecordingUrl,
            SessionType = st,
            Status = ws
        };

        db.SessionInstances.Add(instance);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { instance.Id });
    }

    // ── Application Questions ──────────────────────────────────────────────
    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions([FromQuery] Guid? courseId, CancellationToken cancellationToken)
    {
        var query = db.CourseApplicationQuestions.AsNoTracking().Where(x => x.IsActive);
        if (courseId.HasValue) query = query.Where(x => x.CourseId == courseId.Value);

        var questions = await query
            .OrderBy(x => x.CourseId)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ApplicationQuestionDto(
                x.Id, x.CourseId, x.CohortId, x.QuestionType,
                x.QuestionText, x.HelpText, x.OptionsJson,
                x.IsRequired, x.AutoGrade, x.SortOrder))
            .ToListAsync(cancellationToken);

        return Ok(questions);
    }

    [HttpDelete("questions/{id:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken cancellationToken)
    {
        var q = await db.CourseApplicationQuestions.FindAsync([id], cancellationToken);
        if (q is null) return NotFound();
        q.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { archived = true });
    }

    // ── Pending Applications ───────────────────────────────────────────────
    [HttpGet("applications/pending")]
    public async Task<IActionResult> PendingApplications(CancellationToken cancellationToken)
    {
        var apps = await db.CourseApplications
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .Include(x => x.Cohort)
            .Where(x => x.Status == ApplicationStatus.UnderReview ||
                        x.Status == ApplicationStatus.Submitted ||
                        x.Status == ApplicationStatus.QuestionsPassed)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                CourseTitle   = x.Course == null ? "" : x.Course.Title,
                RoundName     = x.Cohort == null ? "" : x.Cohort.Name,
                StudentName   = x.StudentUser == null ? "" : x.StudentUser.FirstName + " " + x.StudentUser.LastName,
                StudentEmail  = x.StudentUser == null ? "" : (x.StudentUser.Email ?? ""),
                Status        = x.Status.ToString(),
                x.QuestionsPassed,
                x.PaymentUnlocked,
                x.PaymentCompleted,
                ReviewDecision = x.ReviewDecision.ToString(),
                x.ApplicationScore,
                SubmittedAt   = x.SubmittedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync(cancellationToken);

        return Ok(apps);
    }

    [HttpPut("applications/{applicationId:guid}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid applicationId, [FromBody] AdminUpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var app = await db.CourseApplications.FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found");

        app.Status = Enum.Parse<ApplicationStatus>(request.Status);
        
        // If accepting, create enrollment automatically
        if (request.Status == "Accepted")
        {
            app.AcceptedAt = DateTimeOffset.UtcNow;
            
            // Get course price
            var course = await db.Courses.FindAsync([app.CourseId], cancellationToken);
            var price = course?.PriceEgp ?? 0;

            // Determine Cohort to enroll into
            Cohort? targetCohort = null;
            if (app.CohortId.HasValue)
            {
                targetCohort = await db.Cohorts
                    .Include(c => c.CohortEnrollments)
                    .FirstOrDefaultAsync(c => c.Id == app.CohortId.Value, cancellationToken);
            }
            
            // If no cohort is assigned to application or if assigned cohort is full, find/create another one
            if (targetCohort == null || targetCohort.CurrentStudents >= targetCohort.MaxStudents)
            {
                // Find an active open cohort for this course that is not full
                targetCohort = await db.Cohorts
                    .Include(c => c.CohortEnrollments)
                    .Where(c => c.CourseId == app.CourseId && c.IsEnrollmentOpen && c.CurrentStudents < c.MaxStudents)
                    .OrderBy(c => c.StartDate)
                    .FirstOrDefaultAsync(cancellationToken);
                    
                if (targetCohort == null)
                {
                    var courseTitle = course?.Title ?? "Course";
                    var courseSlug = course?.Slug ?? "course";
                    
                    var cohortCount = await db.Cohorts.CountAsync(c => c.CourseId == app.CourseId, cancellationToken);
                    var newRoundName = $"{courseTitle} - Round {cohortCount + 1}";
                    var newRoundSlug = $"{courseSlug}-round-{cohortCount + 1}";
                    
                    targetCohort = new Cohort
                    {
                        CourseId = app.CourseId,
                        Name = newRoundName,
                        Slug = newRoundSlug,
                        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        MaxStudents = 20, // Default to 20
                        CurrentStudents = 0,
                        IsEnrollmentOpen = true,
                        AutoAcceptPaidApplications = false,
                        RequireEngineerApproval = true,
                        Status = CohortStatus.Upcoming
                    };
                    db.Cohorts.Add(targetCohort);
                    await db.SaveChangesAsync(cancellationToken);
                }
                
                // Update the application's cohort ID if it doesn't cause a duplicate
                var duplicateExists = await db.CourseApplications.AnyAsync(x => 
                    x.CourseId == app.CourseId && 
                    x.CohortId == targetCohort.Id && 
                    x.StudentUserId == app.StudentUserId && 
                    x.Id != app.Id, cancellationToken);
                    
                if (!duplicateExists)
                {
                    app.CohortId = targetCohort.Id;
                }
            }
            
            // Check if enrollment already exists
            var existingEnrollment = await db.Enrollments
                .AnyAsync(x => x.StudentUserId == app.StudentUserId && x.CourseId == app.CourseId, cancellationToken);
            
            if (!existingEnrollment)
            {
                var enrollment = new Enrollment
                {
                    StudentUserId = app.StudentUserId,
                    CourseId = app.CourseId,
                    CohortId = targetCohort.Id,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    Status = "Active",
                    FinalPriceEgp = price,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Enrollments.Add(enrollment);
                await db.SaveChangesAsync(cancellationToken); // Save to get enrollment ID
                
                // Add CohortEnrollment
                db.CohortEnrollments.Add(new CohortEnrollment
                {
                    CohortId = targetCohort.Id,
                    StudentUserId = app.StudentUserId,
                    EnrollmentId = enrollment.Id,
                    EnrolledAt = DateTimeOffset.UtcNow
                });
                
                // Update cohort's student count
                targetCohort.CurrentStudents += 1;
            }
        }
        
        await db.SaveChangesAsync(cancellationToken);
        
        await db.NotificationMessages.AddAsync(new NotificationMessage
        {
            RecipientUserId = app.StudentUserId,
            Subject = request.Status == "Accepted" ? "Application Accepted" : "Application Update",
            Body = request.Status == "Accepted" 
                ? "Congratulations! Your application has been accepted. You can now access your course."
                : "Your application status has been updated. Please check for more details.",
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Queued
        }, cancellationToken);
        
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = true, status = request.Status });
    }

    // ── Get All Applications (not just pending) ─────────────────────────────
    [HttpGet("applications")]
    public async Task<IActionResult> GetAllApplications([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = db.CourseApplications
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .Include(x => x.Cohort)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(x => x.Status.ToString() == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var apps = await query
            .OrderByDescending(x => x.SubmittedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                CourseTitle   = x.Course == null ? "" : x.Course.Title,
                RoundName     = x.Cohort == null ? "" : x.Cohort.Name,
                StudentName   = x.StudentUser == null ? "" : x.StudentUser.FirstName + " " + x.StudentUser.LastName,
                StudentEmail  = x.StudentUser == null ? "" : (x.StudentUser.Email ?? ""),
                Status        = x.Status.ToString(),
                x.QuestionsPassed,
                x.PaymentUnlocked,
                x.PaymentCompleted,
                x.ApplicationScore,
                SubmittedAt   = x.SubmittedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items = apps,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    // ── Rounds CRUD ─────────────────────────────────────────────────────────
    [HttpPut("rounds/{id:guid}")]
    public async Task<IActionResult> UpdateRound(Guid id, AdminRoundUpdateRequest request, CancellationToken cancellationToken)
    {
        var round = await db.Cohorts.FindAsync([id], cancellationToken);
        if (round is null) return NotFound();

        round.Name = request.Name;
        round.StartDate = DateOnly.FromDateTime(request.StartDate.DateTime);
        round.MaxStudents = request.MaxStudents;
        round.IsEnrollmentOpen = request.IsEnrollmentOpen;
        round.AutoAcceptPaidApplications = request.AutoAcceptPaidApplications;
        round.RequireEngineerApproval = request.RequireEngineerApproval;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = true });
    }

    [HttpDelete("rounds/{id:guid}")]
    public async Task<IActionResult> DeleteRound(Guid id, CancellationToken cancellationToken)
    {
        var round = await db.Cohorts.FindAsync([id], cancellationToken);
        if (round is null) return NotFound();
        
        // Don't delete if there are enrollments
        var hasEnrollments = await db.Enrollments.AnyAsync(x => x.CohortId == id, cancellationToken);
        if (hasEnrollments) return BadRequest(new { message = "Cannot delete round with active enrollments" });

        db.Cohorts.Remove(round);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    // ── Notifications Broadcast ────────────────────────────────────────────
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> Broadcast(AdminBroadcastRequest request, CancellationToken cancellationToken)
    {
        var userIds = await db.Users.Select(u => u.Id).ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            db.NotificationMessages.Add(new NotificationMessage
            {
                RecipientUserId = userId,
                Subject         = request.Title,
                Body            = request.Body,
                Channel         = NotificationChannel.InApp,
                Status          = NotificationStatus.Queued
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { queued = userIds.Count });
    }

    [HttpPost("rounds/{cohortId:guid}/notify")]
    public async Task<IActionResult> NotifyRoundStudents(Guid cohortId, AdminBroadcastRequest request, CancellationToken cancellationToken)
    {
        var studentIds = await db.CohortEnrollments
            .Where(e => e.CohortId == cohortId)
            .Select(e => e.StudentUserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in studentIds)
        {
            db.NotificationMessages.Add(new NotificationMessage
            {
                RecipientUserId = userId,
                Subject = request.Title,
                Body = request.Body,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Queued
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { queued = studentIds.Count });
    }

    // ── Students ─────────────────────────────────────────────────────────────
    [HttpGet("students")]
    public async Task<IActionResult> GetStudents([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = db.Enrollments
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x => 
                (x.StudentUser != null && (x.StudentUser.FirstName + " " + x.StudentUser.LastName).ToLower().Contains(lower)) ||
                (x.StudentUser != null && x.StudentUser.Email != null && x.StudentUser.Email.ToLower().Contains(lower)) ||
                (x.Course != null && x.Course.Title.ToLower().Contains(lower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var students = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                StudentId = x.StudentUserId,
                StudentName = x.StudentUser == null ? "Unknown" : x.StudentUser.FirstName + " " + x.StudentUser.LastName,
                StudentEmail = x.StudentUser == null ? "" : x.StudentUser.Email ?? "",
                StudentXp = x.StudentUser == null ? 0 : x.StudentUser.TotalXp,
                CourseName = x.Course == null ? "Unknown" : x.Course.Title,
                x.EnrollmentStatus,
                x.CreatedAt,
                x.FinalPriceEgp
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items = students,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    // ── Enrollments ─────────────────────────────────────────────────────────
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetEnrollments([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = db.Enrollments
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .Include(x => x.Cohort)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var enrollments = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                StudentName = x.StudentUser == null ? "Unknown" : x.StudentUser.FirstName + " " + x.StudentUser.LastName,
                StudentEmail = x.StudentUser == null ? "" : x.StudentUser.Email ?? "",
                CourseName = x.Course == null ? "Unknown" : x.Course.Title,
                CohortName = x.Cohort == null ? "" : x.Cohort.Name,
                x.EnrollmentStatus,
                x.FinalPriceEgp,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items = enrollments,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    // ── Admin Enroll Student ─────────────────────────────────────────────────
    [HttpPost("enrollments")]
    public async Task<IActionResult> CreateEnrollment([FromBody] AdminCreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var student = await db.Users.FindAsync([request.StudentUserId], cancellationToken);
        if (student is null) return NotFound("Student not found");

        var course = await db.Courses.FindAsync([request.CourseId], cancellationToken);
        if (course is null) return NotFound("Course not found");

        Cohort? cohort = null;
        if (request.CourseRoundId.HasValue)
            cohort = await db.Cohorts.FindAsync([request.CourseRoundId.Value], cancellationToken);

        var existing = await db.Enrollments.AnyAsync(x =>
            x.StudentUserId == request.StudentUserId && x.CourseId == request.CourseId, cancellationToken);
        if (existing) return Conflict("Student is already enrolled in this course");

        var enrollment = new Enrollment
        {
            StudentUserId = request.StudentUserId,
            CourseId = request.CourseId,
            CohortId = cohort?.Id,
            EnrollmentStatus = EnrollmentStatus.Active,
            Status = "Active",
            UnitPriceEgp = course.PriceEgp,
            FinalPriceEgp = course.PriceEgp,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Enrollments.Add(enrollment);

        await db.NotificationMessages.AddAsync(new NotificationMessage
        {
            RecipientUserId = request.StudentUserId,
            Subject = "Enrolled in Course",
            Body = $"You have been enrolled in {course.Title}. Access your course room to begin.",
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Queued
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { id = enrollment.Id });
    }

    // ── Analytics Summary ────────────────────────────────────────────────────
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
    {
        var thisMonth = DateTimeOffset.UtcNow.AddMonths(-1);
        
        var monthlyRevenue = await db.EnrollmentOrders
            .Where(x => x.PaymentStatus == PaymentStatus.Paid && x.CreatedAt >= thisMonth)
            .SumAsync(x => x.TotalAmountEgp, cancellationToken);

        var monthlyEnrollments = await db.Enrollments
            .CountAsync(x => x.CreatedAt >= thisMonth, cancellationToken);

        var totalRevenue = await db.EnrollmentOrders
            .Where(x => x.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(x => x.TotalAmountEgp, cancellationToken);

        var activeStudents = await db.Enrollments
            .CountAsync(x => x.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);

        return Ok(new
        {
            monthlyRevenue,
            monthlyEnrollments,
            totalRevenue,
            activeStudents,
            totalCourses = await db.Courses.CountAsync(x => x.IsActive, cancellationToken),
            totalApplications = await db.CourseApplications.CountAsync(cancellationToken),
            pendingApplications = await db.CourseApplications.CountAsync(x => x.Status == ApplicationStatus.Submitted || x.Status == ApplicationStatus.UnderReview, cancellationToken)
        });
    }

    // ── Sessions ─────────────────────────────────────────────────────────────
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await db.SessionInstances
            .AsNoTracking()
            .Include(x => x.Cohort)
                .ThenInclude(c => c.Course)
            .Include(x => x.CourseSession)
            .OrderBy(x => x.ScheduledAt)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                Title = x.CourseSession == null ? "Unknown Session" : x.CourseSession.Title,
                CourseName = x.Cohort != null && x.Cohort.Course != null ? x.Cohort.Course.Title : "Unknown Course",
                CohortName = x.Cohort == null ? "" : x.Cohort.Name,
                ScheduledAt = x.ScheduledAt.ToString("yyyy-MM-dd HH:mm"),
                x.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    // ── Course Sessions (CourseSession) ───────────────────────────────────────
    [HttpGet("course-sessions/{courseId:guid}")]
    public async Task<IActionResult> GetCourseSessions(Guid courseId, CancellationToken cancellationToken)
    {
        var sessions = await db.CourseSessions
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.SessionType,
                x.SortOrder,
                x.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpPost("course-sessions")]
    public async Task<IActionResult> CreateCourseSession(AdminCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = new CourseSession
        {
            CourseId = request.CourseId,
            Title = request.Title,
            Description = request.Description ?? "",
            SessionType = Enum.Parse<SessionType>(request.SessionType),
            SortOrder = request.SortOrder,
            DurationMinutes = request.DurationMinutes
        };

        db.CourseSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { session.Id });
    }

    [HttpPut("course-sessions/{id:guid}")]
    public async Task<IActionResult> UpdateCourseSession(Guid id, AdminCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await db.CourseSessions.FindAsync([id], cancellationToken);
        if (session is null) return NotFound();

        session.Title = request.Title;
        session.Description = request.Description ?? "";
        session.SessionType = Enum.Parse<SessionType>(request.SessionType);
        session.SortOrder = request.SortOrder;
        session.DurationMinutes = request.DurationMinutes;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = true });
    }

    [HttpDelete("course-sessions/{id:guid}")]
    public async Task<IActionResult> DeleteCourseSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await db.CourseSessions.FindAsync([id], cancellationToken);
        if (session is null) return NotFound();

        db.CourseSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    [HttpGet("rounds/{cohortId:guid}/weeks")]
    public async Task<IActionResult> GetRoundWeeks(Guid cohortId, CancellationToken cancellationToken)
    {
        var weeks = await db.SessionInstances
            .AsNoTracking()
            .Where(x => x.CohortId == cohortId)
            .OrderBy(x => x.WeekNumber)
            .Select(w => new
            {
                w.Id,
                w.WeekNumber,
                w.WeekTitle,
                SessionType = w.SessionType.ToString(),
                w.ScheduledAt,
                w.DurationMinutes,
                Status = w.Status.ToString(),
                w.SessionLink,
                w.RecordingUrl
            })
            .ToListAsync(cancellationToken);

        return Ok(weeks);
    }

    [HttpDelete("rounds/{cohortId:guid}/weeks/{id:guid}")]
    public async Task<IActionResult> DeleteRoundWeek(Guid cohortId, Guid id, CancellationToken cancellationToken)
    {
        var week = await db.SessionInstances.FirstOrDefaultAsync(x => x.Id == id && x.CohortId == cohortId, cancellationToken);
        if (week is null) return NotFound();
        db.SessionInstances.Remove(week);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    [HttpDelete("materials/{id:guid}")]
    public async Task<IActionResult> DeleteCourseMaterial(Guid id, CancellationToken cancellationToken)
    {
        var material = await db.CourseMaterials.FindAsync([id], cancellationToken);
        if (material is null) return NotFound();
        db.CourseMaterials.Remove(material);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> DeleteLearningTask(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.LearningTasks.FindAsync([id], cancellationToken);
        if (task is null) return NotFound();
        db.LearningTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    [HttpDelete("quizzes/{id:guid}")]
    public async Task<IActionResult> DeleteQuiz(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes.FindAsync([id], cancellationToken);
        if (quiz is null) return NotFound();
        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = true });
    }

    // ── Tasks & Quizzes (Admin Fallback) ─────────────────────────────────────
    [HttpGet("rounds/{cohortId:guid}/tasks")]
    public async Task<IActionResult> GetRoundTasks(Guid cohortId, CancellationToken cancellationToken)
    {
        var tasks = await db.LearningTasks
            .AsNoTracking()
            .Where(x => x.CohortId == cohortId)
            .Select(t => new
            {
                t.Id, t.Title, t.Description, t.Instructions,
                TaskType = t.TaskType.ToString(),
                SubmissionType = t.SubmissionType.ToString(),
                t.MaxScore, t.XpReward, t.DueHoursAfterSession
            })
            .ToListAsync(cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("rounds/{cohortId:guid}/quizzes")]
    public async Task<IActionResult> GetRoundQuizzes(Guid cohortId, CancellationToken cancellationToken)
    {
        var quizzes = await db.Quizzes
            .AsNoTracking()
            .Where(x => x.CohortId == cohortId)
            .Select(q => new
            {
                q.Id, q.Title,
                QuizType = q.QuizType.ToString(),
                q.TimeLimitMinutes, q.MaxAttempts, q.PassScore,
                q.XpReward, q.IsPublished,
                QuestionsCount = db.Questions.Count(x => x.QuizId == q.Id)
            })
            .ToListAsync(cancellationToken);
        return Ok(quizzes);
    }

    [HttpGet("rounds/{cohortId:guid}/students")]
    public async Task<IActionResult> GetRoundStudents(Guid cohortId, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts
            .AsNoTracking()
            .Include(x => x.CohortEnrollments)
                .ThenInclude(e => e.StudentUser)
            .FirstOrDefaultAsync(x => x.Id == cohortId, cancellationToken);

        if (cohort is null) return NotFound();

        var studentUserIds = cohort.CohortEnrollments.Select(e => e.StudentUserId).ToList();
        var applicationsMap = await db.CourseApplications
            .AsNoTracking()
            .Where(x => studentUserIds.Contains(x.StudentUserId))
            .GroupBy(x => x.StudentUserId)
            .Select(g => g.OrderByDescending(x => x.SubmittedAt).FirstOrDefault())
            .Where(x => x != null)
            .ToDictionaryAsync(x => x!.StudentUserId, x => x!.Id, cancellationToken);

        var students = cohort.CohortEnrollments.Select(e => new
        {
            e.StudentUserId,
            StudentName = e.StudentUser == null ? "" : (e.StudentUser.FirstName + " " + e.StudentUser.LastName).Trim(),
            Email = e.StudentUser?.Email ?? "",
            e.EnrolledAt,
            ApplicationId = applicationsMap.TryGetValue(e.StudentUserId, out var appId) ? appId : (Guid?)null
        }).ToList();

        return Ok(students);
    }

    // ── Materials (Admin) ────────────────────────────────────────────────────
    [HttpGet("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> GetCourseMaterials(Guid courseId, CancellationToken cancellationToken)
    {
        var materials = await db.CourseMaterials
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Title,
                MaterialType = m.MaterialType.ToString(),
                m.Url,
                m.IsDownloadable,
                m.IsPublished,
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(materials);
    }

    [HttpPost("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> UploadCourseMaterial(Guid courseId, AdminMaterialUploadRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FindAsync([courseId], cancellationToken);
        if (course is null) return NotFound(new { message = "Course not found" });

        var material = new CourseMaterial
        {
            CourseId = courseId,
            Title = request.Title,
            MaterialType = Enum.Parse<CourseMaterialType>(request.MaterialType),
            Url = request.Url,
            IsDownloadable = request.IsDownloadable,
            IsPublished = true
        };

        db.CourseMaterials.Add(material);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { material.Id });
    }

    // ── Session Instance Management (Admin) ─────────────────────────────────
    [HttpPut("sessions/{id:guid}")]
    public async Task<IActionResult> UpdateSessionInstance(Guid id, AdminSessionInstanceRequest request, CancellationToken cancellationToken)
    {
        var instance = await db.SessionInstances.FindAsync([id], cancellationToken);
        if (instance is null) return NotFound();

        instance.Status = Enum.Parse<SessionStatus>(request.Status);
        if (request.SessionLink is not null) instance.SessionLink = request.SessionLink;
        if (request.RecordingUrl is not null) instance.RecordingUrl = request.RecordingUrl;
        if (request.WeekNumber.HasValue) instance.WeekNumber = request.WeekNumber.Value;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = true });
    }

    // ── Round Zoom Configuration ────────────────────────────────────────────
    [HttpPost("rounds/{id:guid}/zoom-generate")]
    public async Task<IActionResult> GenerateRoundZoom(Guid id, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            
        if (cohort is null) return NotFound(new { message = "Cohort not found." });

        try
        {
            var topic = $"Live Session: {cohort.Course?.Title ?? "Course"} - {cohort.Name}";
            var durationMinutes = 90; // Default to 90 mins session
            
            var meeting = await zoomService.CreateMeetingAsync(topic, durationMinutes, cancellationToken);

            cohort.ZoomMeetingId = meeting.Id.ToString();
            cohort.ZoomJoinUrl = meeting.JoinUrl;
            cohort.ZoomStartUrl = meeting.StartUrl;

            await db.SaveChangesAsync(cancellationToken);
            
            return Ok(new 
            { 
                success = true,
                zoomMeetingId = cohort.ZoomMeetingId,
                zoomJoinUrl = cohort.ZoomJoinUrl,
                zoomStartUrl = cohort.ZoomStartUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rounds/{id:guid}/zoom")]
    public async Task<IActionResult> UpdateRoundZoom(Guid id, AdminRoundZoomRequest request, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts.FindAsync([id], cancellationToken);
        if (cohort is null) return NotFound();

        cohort.ZoomMeetingId = request.ZoomMeetingId;
        cohort.ZoomJoinUrl = request.ZoomJoinUrl;
        cohort.ZoomStartUrl = request.ZoomStartUrl;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = true });
    }
}

public record AdminUpdateStatusRequest(string Status);
public record AdminRoundUpdateRequest(string Name, DateTimeOffset StartDate, int MaxStudents, bool IsEnrollmentOpen, bool AutoAcceptPaidApplications, bool RequireEngineerApproval);
public record AdminCourseSessionRequest(Guid CourseId, string Title, string? Description, string SessionType, int SortOrder, int DurationMinutes);
public record AdminMaterialUploadRequest(string Title, string MaterialType, string Url, bool IsDownloadable);
public record AdminSessionInstanceRequest(string Status, string? SessionLink, string? RecordingUrl, int? WeekNumber);
public record AdminRoundZoomRequest(string? ZoomMeetingId, string? ZoomJoinUrl, string? ZoomStartUrl);
public record AdminCreateEnrollmentRequest(Guid StudentUserId, Guid CourseId, Guid? CourseRoundId);
