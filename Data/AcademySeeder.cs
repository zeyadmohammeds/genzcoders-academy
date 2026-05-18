using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Data;

public static class AcademySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AcademyDbContext>();

        foreach (var role in AcademyRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                    Description = $"ElSewedy GenZ Coders {role} role"
                });
            }
        }

        // --- SEED USERS ---
        await EnsureUserAsync(userManager, "admin@genz.academy", "Academy123!", AcademyRole.AcademyAdmin, "Super", "Admin");
        await EnsureUserAsync(userManager, "cta@genz.academy", "Academy123!", AcademyRole.Cta, "Learning", "Advisor");
        await EnsureUserAsync(userManager, "student@genz.academy", "Academy123!", AcademyRole.Student, "Demo", "Student");

        // Specific user from request
        var specificUserId = new Guid("c24b043c-e66d-45fc-ba4b-bf13b22e2057");
        var specUser = await userManager.FindByIdAsync(specificUserId.ToString());
        if (specUser == null)
        {
            specUser = new ApplicationUser
            {
                Id = specificUserId,
                UserName = "user@genz.academy",
                Email = "user@genz.academy",
                EmailConfirmed = true,
                FirstName = "Premium",
                LastName = "User",
                RoleKey = AcademyRole.Student,
                IsActive = true
            };
            var result = await userManager.CreateAsync(specUser, "Academy123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(specUser, AcademyRole.Student);
            }
        }
        else
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(specUser);
            await userManager.ResetPasswordAsync(specUser, token, "Academy123!");
            specUser.EmailConfirmed = true;
            await userManager.UpdateAsync(specUser);
        }

        var scratchCourse = await db.Courses.FirstOrDefaultAsync(x => x.Slug == "scratch");
        if (scratchCourse != null && scratchCourse.Id != new Guid("a1111111-1111-1111-1111-111111111111"))
        {
            db.XpTransactions.RemoveRange(db.XpTransactions);
            db.TaskSubmissions.RemoveRange(db.TaskSubmissions);
            db.QuizAttempts.RemoveRange(db.QuizAttempts);
            db.Questions.RemoveRange(db.Questions);
            db.Quizzes.RemoveRange(db.Quizzes);
            db.LearningTasks.RemoveRange(db.LearningTasks);
            db.CourseMaterials.RemoveRange(db.CourseMaterials);
            db.SessionInstances.RemoveRange(db.SessionInstances);
            db.AttendanceRecords.RemoveRange(db.AttendanceRecords);
            db.CohortEnrollments.RemoveRange(db.CohortEnrollments);
            db.Enrollments.RemoveRange(db.Enrollments);
            db.Cohorts.RemoveRange(db.Cohorts);
            db.CourseSessions.RemoveRange(db.CourseSessions);
            db.CourseApplicationQuestions.RemoveRange(db.CourseApplicationQuestions);
            db.CourseApplications.RemoveRange(db.CourseApplications);
            db.Courses.RemoveRange(db.Courses);
            await db.SaveChangesAsync();
        }

        if (!await db.Courses.AnyAsync())
        {
            var courses = new[]
            {
                Course("scratch", "Scratch Creative Coding", "Animations, games, logic, and creative confidence.", "A playable animated story or mini game.", 10, 500, "Beginner", "[\"logic\",\"animation\",\"events\",\"game design\"]"),
                Course("intro-cpp", "Intro to C++", "Variables, loops, functions, and problem-solving foundations.", "A console game and problem-solving portfolio.", 13, 600, "Beginner", "[\"variables\",\"loops\",\"functions\",\"problem solving\"]"),
                Course("advanced-cpp", "Advanced C++", "OOP, data structures, and game development with SFML.", "A polished 2D game prototype.", 15, 700, "Advanced", "[\"OOP\",\"data structures\",\"SFML\",\"game loops\"]"),
                Course("robot-build", "Electronics and Build Your Robot", "Circuits, Arduino, sensors, and physical computing.", "A working robot with sensors.", 13, 600, "Builder", "[\"circuits\",\"Arduino\",\"sensors\",\"robotics\"]"),
                Course("web-app-ai", "Build Web App with AI", "Ship a live web app using AI-assisted product workflows.", "A deployed web app with a public showcase link.", 13, 600, "Creator", "[\"HTML\",\"CSS\",\"AI tools\",\"deployment\"]")
            };

            var sort = 1;
            foreach (var course in courses)
            {
                course.SortOrder = sort++;
                course.Modules = Enumerable.Range(1, 8).Select(i => new CourseModule
                {
                    SortOrder = i,
                    Title = $"Session {i}: Build milestone {i}",
                    ProjectOutcome = i == 8 ? course.Outcome : $"A visible project increment for {course.Title}."
                }).ToList();

                course.CourseSessions = Enumerable.Range(1, 12).Select(i => new CourseSession
                {
                    SessionNumber = i,
                    SortOrder = i,
                    SessionType = i is 3 or 6 or 9 or 12 ? SessionType.TechnicalSupport : SessionType.Core,
                    DurationMinutes = i is 3 or 6 or 9 or 12 ? 240 : 90,
                    Title = i is 3 or 6 or 9 or 12 ? $"CTA Lab {i / 3}" : $"Core Build Session {i}",
                    Description = $"Project-first learning milestone for {course.Title}.",
                    Outcome = i == 12 ? course.Outcome : $"Students ship milestone {i} for {course.Title}.",
                    Principle = "Build, challenge, share."
                }).ToList();

                course.LiveSessions = Enumerable.Range(1, 4).Select(i => new LiveSession
                {
                    Title = $"{course.Title} live lab {i}",
                    StartsAt = DateTimeOffset.UtcNow.AddDays(7 + (i - 1) * 7),
                    HostName = i % 3 == 1 ? "Alaa Abdelrahman" : i % 3 == 2 ? "Fady" : "Aya",
                    ZoomMeetingId = "0000000000",
                    ZoomJoinUrl = "https://zoom.us/j/0000000000",
                    ZoomSdkSignatureEndpoint = "/api/live-sessions/zoom-signature"
                }).ToList();
            }

            db.Courses.AddRange(courses);
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await EnsureCourseDetailsAsync(db);
        await EnsureCourseRoundsAndQuestionsAsync(db);

        if (!await db.Schools.AnyAsync())
        {
            db.Schools.Add(new School
            {
                Name = "Founding Partner School",
                Type = SchoolType.Stem,
                City = "Cairo",
                CoordinatorName = "School Coordinator",
                CoordinatorEmail = "coordinator@example.com",
                Status = "founding_partner",
                PartnershipStatus = PartnershipStatus.FoundingPartner,
                MouSigned = true,
                PartnerSince = new DateOnly(2026, 5, 1)
            });
        }

        if (!await db.Badges.AnyAsync())
        {
            db.Badges.AddRange(
                Badge("first-launch", "First Launch", "Attend the first live session.", 100),
                Badge("on-fire", "On Fire", "Keep a 7-day learning streak.", 150),
                Badge("perfect-score", "Perfect Score", "Score 100% on any quiz.", 200),
                Badge("ambassador", "Ambassador", "Refer one enrolled student.", 300),
                Badge("builder", "Builder", "Submit your first project.", 150),
                Badge("deployed", "Deployed", "Deploy a live web app.", 250),
                Badge("mentor", "Mentor", "CTA milestone for grading 50 tasks.", 500));
        }

        if (!await db.PromoCodes.AnyAsync())
        {
            db.PromoCodes.AddRange(
                new PromoCode { Code = "PARTNER15", Description = "Partner school discount", DiscountType = DiscountType.Percentage, Value = 15, IsActive = true },
                new PromoCode { Code = "SUMMER25", Description = "All-course bundle discount", DiscountType = DiscountType.Percentage, Value = 25, AppliesToBundle = true, IsActive = true },
                new PromoCode { Code = "ROBOT50", Description = "50 EGP off Robot Build", DiscountType = DiscountType.FixedAmount, Value = 50, IsActive = true });
        }

        if (!await db.NotificationTemplates.AnyAsync())
        {
            db.NotificationTemplates.AddRange(
                Template("session.reminder", "whatsapp", "Session reminder", "Your session starts in 2 hours. Join from the academy dashboard."),
                Template("task.created", "email", "New mission unlocked", "A new task is available for your course."),
                Template("badge.earned", "in_app", "Badge earned", "You earned a new badge. Keep building."),
                Template("weekly.parent.report", "email", "Weekly progress report", "Here is your child's weekly academy progress."));
        }

        await db.SaveChangesAsync();

        // Seed specific user data for enrollment and progress
        if (specUser != null && !await db.StudentProfiles.AnyAsync(x => x.UserId == specUser.Id))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = specUser.Id,
                ReferralCode = "ZIZO99",
                TotalXp = 500,
                Level = 2,
                ExperienceLevel = ExperienceLevel.Advanced,
                InterestsJson = "[\"Electronics\", \"Robotics\"]",
                IsOnboardingCompleted = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-15)
            });

            var course = await db.Courses.FirstOrDefaultAsync(x => x.Slug == "robot-build");
            if (course != null)
            {
                var cohort = await db.Cohorts.FirstOrDefaultAsync(x => x.CourseId == course.Id);
                if (cohort != null)
                {
                    db.Enrollments.Add(new Enrollment
                    {
                        StudentUserId = specUser.Id,
                        CourseId = course.Id,
                        CohortId = cohort.Id,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        Status = "active",
                        UnitPriceEgp = course.PriceEgp,
                        FinalPriceEgp = course.PriceEgp
                    });

                    // Add a pending task submission
                    var task = await db.LearningTasks.FirstOrDefaultAsync(x => x.CohortId == cohort.Id);
                    if (task != null)
                    {
                        db.TaskSubmissions.Add(new TaskSubmission
                        {
                            StudentUserId = specUser.Id,
                            LearningTaskId = task.Id,
                            SubmissionUrl = "https://scratch.mit.edu/projects/demo",
                            SubmissionText = "My first robotics logic scene!",
                            Status = SubmissionStatus.Pending,
                            SubmittedAt = DateTimeOffset.UtcNow.AddHours(-5)
                        });
                    }
                }
            }
        }

        // --- SEED STUDENT DATA ---
        var demoStudent = await userManager.FindByEmailAsync("student@genz.academy");
        if (demoStudent != null)
        {
            var existingDemoProfile = await db.StudentProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.ReferralCode == "DEMO100");

            if (existingDemoProfile != null && existingDemoProfile.UserId != demoStudent.Id)
            {
                db.StudentProfiles.Remove(existingDemoProfile);
                await db.SaveChangesAsync();
            }

            if (!await db.StudentProfiles.AnyAsync(x => x.UserId == demoStudent.Id))
            {
                db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = demoStudent.Id,
                    ReferralCode = "DEMO100",
                    TotalXp = 100,
                    Level = 1,
                    ExperienceLevel = ExperienceLevel.New,
                    InterestsJson = "[\"Web apps\", \"AI\"]",
                    IsOnboardingCompleted = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
                });
            }

            var course = await db.Courses.FirstOrDefaultAsync(x => x.Slug == "scratch");
            Cohort? cohort = null;
            if (course != null)
            {
                cohort = await db.Cohorts.FirstOrDefaultAsync(x => x.CourseId == course.Id);
            }

            if (course != null && cohort != null)
            {
                // Ensure they have an application that is ready for payment
                if (!await db.CourseApplications.AnyAsync(x => x.StudentUserId == demoStudent.Id && x.CourseId == course.Id))
                {
                    db.CourseApplications.Add(new CourseApplication
                    {
                        StudentUserId = demoStudent.Id,
                        CourseId = course.Id,
                        CohortId = cohort.Id,
                        Status = ApplicationStatus.Accepted,
                        PaymentUnlocked = true,
                        SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
                    });
                }

                if (!await db.Enrollments.AnyAsync(x => x.StudentUserId == demoStudent.Id))
                {
                    var enrollment = new Enrollment
                    {
                        StudentUserId = demoStudent.Id,
                        CourseId = course.Id,
                        CohortId = cohort.Id,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        Status = "active",
                        UnitPriceEgp = course.PriceEgp,
                        FinalPriceEgp = course.PriceEgp,
                        CompletedAt = null
                    };
                    db.Enrollments.Add(enrollment);
                }

                // Add some XP
                db.XpTransactions.Add(new XpTransaction
                {
                    StudentUserId = demoStudent.Id,
                    Amount = 100,
                    Description = "Profile completion",
                    SourceType = XpSourceType.Bonus
                });

                db.XpTransactions.Add(new XpTransaction
                {
                    StudentUserId = demoStudent.Id,
                    Amount = 250,
                    Description = "Course enrollment bonus",
                    SourceType = XpSourceType.Bonus
                });
            }

            if (cohort != null && !await db.LearningTasks.AnyAsync(x => x.CohortId == cohort.Id))
            {
                var task1 = new LearningTask
                {
                    CohortId = cohort.Id,
                    Title = "Build your first scene",
                    Description = "Use Scratch to create an animated scene with 2 sprites.",
                    XpReward = 50,
                    TaskType = TaskType.Project,
                    SubmissionType = SubmissionType.Link
                };
                db.LearningTasks.Add(task1);

                var task2 = new LearningTask
                {
                    CohortId = cohort.Id,
                    Title = "Loops Challenge",
                    Description = "Repeat a dance movement 10 times using a loop block.",
                    XpReward = 50,
                    TaskType = TaskType.Design, // Using Design as alternative for Exercise
                    SubmissionType = SubmissionType.Link
                };
                db.LearningTasks.Add(task2);
            }

            if (!await db.NotificationMessages.AnyAsync(x => x.RecipientUserId == demoStudent.Id))
            {
                db.NotificationMessages.Add(new NotificationMessage
                {
                    RecipientUserId = demoStudent.Id,
                    Subject = "Welcome to the Academy!",
                    Body = "We're excited to have you here. Start your first session now.",
                    Channel = NotificationChannel.InApp,
                    Status = NotificationStatus.Sent
                });
                db.NotificationMessages.Add(new NotificationMessage
                {
                    RecipientUserId = demoStudent.Id,
                    Subject = "New Badge: First Launch",
                    Body = "Congratulations! You earned your first badge.",
                    Channel = NotificationChannel.InApp,
                    Status = NotificationStatus.Sent
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureCourseDetailsAsync(AcademyDbContext db)
    {
        var courses = await db.Courses
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.ShortDescription,
                x.Description,
                x.Outcome,
                x.PriceEgp,
                x.SortOrder,
                x.Phase,
                x.SkillsTaughtJson,
                SessionCount = x.CourseSessions.Count
            })
            .ToListAsync();

        var sort = 1;
        foreach (var course in courses.OrderBy(x => x.PriceEgp).ThenBy(x => x.Title))
        {
            await db.Courses
                .Where(x => x.Id == course.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SortOrder, course.SortOrder == 0 ? sort : course.SortOrder)
                    .SetProperty(x => x.IsActive, true)
                    .SetProperty(x => x.Phase, course.Phase == 0 ? 1 : course.Phase)
                    .SetProperty(x => x.Description, string.IsNullOrWhiteSpace(course.Description) ? course.ShortDescription : course.Description)
                    .SetProperty(x => x.SkillsTaughtJson, string.IsNullOrWhiteSpace(course.SkillsTaughtJson) ? "[]" : course.SkillsTaughtJson));

            if (course.SessionCount == 0)
            {
                db.CourseSessions.AddRange(Enumerable.Range(1, 12).Select(i => new CourseSession
                {
                    CourseId = course.Id,
                    SessionNumber = i,
                    SortOrder = i,
                    SessionType = i is 3 or 6 or 9 or 12 ? SessionType.TechnicalSupport : SessionType.Core,
                    DurationMinutes = i is 3 or 6 or 9 or 12 ? 240 : 90,
                    Title = i is 3 or 6 or 9 or 12 ? $"CTA Lab {i / 3}" : $"Core Build Session {i}",
                    Description = $"Project-first learning milestone for {course.Title}.",
                    Outcome = i == 12 ? course.Outcome : $"Students ship milestone {i} for {course.Title}.",
                    Principle = "Build, challenge, share."
                }));
            }

            sort++;
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task EnsureCourseRoundsAndQuestionsAsync(AcademyDbContext db)
    {
        var courses = await db.Courses.AsNoTracking().ToListAsync();
        var now = DateTimeOffset.UtcNow;
        var weekFromNow = DateOnly.FromDateTime(now.Date.AddDays(7));
        var threeMonthsLater = DateOnly.FromDateTime(now.Date.AddMonths(3));
        var monthName = now.AddDays(7).ToString("MMMM yyyy");
        foreach (var course in courses)
        {
            var roundSlug = $"{course.Slug}-{now:yyyy-MM}";
            var round = await db.Cohorts.FirstOrDefaultAsync(x => x.Slug == roundSlug);
            if (round is null)
            {
                round = new Cohort
                {
                    CourseId = course.Id,
                    Name = $"{course.Title} - {monthName}",
                    Slug = roundSlug,
                    Description = $"{monthName} cohort for {course.Title}.",
                    StartDate = weekFromNow,
                    EndDate = threeMonthsLater,
                    MaxStudents = 20,
                    Mode = CourseRoundMode.Online,
                    Status = CohortStatus.Upcoming,
                    IsEnrollmentOpen = true,
                    AutoAcceptPaidApplications = false,
                    RequireEngineerApproval = true,
                    ZoomMeetingId = "0000000000",
                    ZoomJoinUrl = "https://zoom.us/j/0000000000"
                };
                db.Cohorts.Add(round);
                await db.SaveChangesAsync();
            }

            if (!await db.SessionInstances.AnyAsync(x => x.CohortId == round.Id))
            {
                var sessions = await db.CourseSessions
                    .AsNoTracking()
                    .Where(x => x.CourseId == course.Id)
                    .OrderBy(x => x.SessionNumber)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    db.SessionInstances.Add(new SessionInstance
                    {
                        CohortId = round.Id,
                        CourseSessionId = session.Id,
                        WeekNumber = session.SessionNumber,
                        WeekTitle = session.Title,
                        SessionType = session.SessionType,
                        ScheduledAt = now.AddDays(7 + (session.SessionNumber - 1) * 4),
                        DurationMinutes = session.DurationMinutes,
                        SessionLink = round.ZoomJoinUrl,
                        Status = session.SessionNumber == 1 ? SessionStatus.Live : SessionStatus.Scheduled
                    });
                }
            }

            if (!await db.CourseApplicationQuestions.AnyAsync(x => x.CourseId == course.Id))
            {
                db.CourseApplicationQuestions.AddRange(
                    new CourseApplicationQuestion
                    {
                        CourseId = course.Id,
                        QuestionType = ApplicationQuestionType.Mcq,
                        QuestionText = $"Why do you want to join {course.Title}?",
                        OptionsJson = "[\"Build projects\",\"Only watch videos\",\"Skip sessions\"]",
                        CorrectAnswer = "Build projects",
                        SortOrder = 1
                    },
                    new CourseApplicationQuestion
                    {
                        CourseId = course.Id,
                        QuestionType = ApplicationQuestionType.TrueFalse,
                        QuestionText = "I understand this is a live project-based course with tasks and quizzes.",
                        OptionsJson = "[\"true\",\"false\"]",
                        CorrectAnswer = "true",
                        SortOrder = 2
                    },
                    new CourseApplicationQuestion
                    {
                        CourseId = course.Id,
                        QuestionType = ApplicationQuestionType.ShortAnswer,
                        QuestionText = "Tell us about one thing you want to build.",
                        AutoGrade = false,
                        SortOrder = 3
                    });
            }
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string role, string firstName, string lastName)
    {
        var user = await userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            // Create new user with password
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                RoleKey = role,
                IsActive = true
            };
            
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                Console.WriteLine($"Failed to create user {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                return;
            }
            
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            // User exists - generate password reset token and set password
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, password);
            if (!resetResult.Succeeded)
            {
                Console.WriteLine($"Failed to reset password for {email}: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }
            
            // Update role if needed
            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
            
            // Update properties
            user.EmailConfirmed = true;
            user.IsActive = true;
            user.RoleKey = role;
            user.FirstName = firstName;
            user.LastName = lastName;
            await userManager.UpdateAsync(user);
        }
        
        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static Course Course(string slug, string title, string description, string outcome, int age, decimal price, string level, string skillsJson)
    {
        var (icon, color, subtitle, idStr) = slug switch
        {
            "scratch" => ("scratch", "#7C3AED", "Ages 10–13 · Beginner", "a1111111-1111-1111-1111-111111111111"),
            "intro-cpp" => ("code", "#3B82F6", "Ages 13+ · Foundations", "b2222222-2222-2222-2222-222222222222"),
            "advanced-cpp" => ("lightning", "#EF4444", "Ages 15+ · Advanced", "c3333333-3333-3333-3333-333333333333"),
            "robot-build" => ("robot", "#F59E0B", "Ages 12–17 · Maker", "d4444444-4444-4444-4444-444444444444"),
            "web-app-ai" => ("globe", "#10B981", "Ages 13+ · Creator", "e5555555-5555-5555-5555-555555555555"),
            _ => ("book", "#6366F1", "", "f6666666-6666-6666-6666-666666666666")
        };
        return new Course
        {
            Id = new Guid(idStr),
            Slug = slug,
            Title = title,
            Description = description,
            ShortDescription = description,
            Subtitle = subtitle,
            Outcome = outcome,
            MinimumAge = age,
            PriceEgp = price,
            Level = level,
            SkillsTaughtJson = skillsJson,
            IconName = icon,
            ColorHex = color,
            CoreSessions = 8,
            SupportSessions = 4,
            IsFeatured = true,
        };
    }

    private static Badge Badge(string slug, string name, string description, int xpReward)
        => new() { Slug = slug, Name = name, Description = description, XpReward = xpReward };

    private static NotificationTemplate Template(string key, string channel, string subject, string body)
        => new() { Key = key, Channel = channel, Subject = subject, Body = body };
}
