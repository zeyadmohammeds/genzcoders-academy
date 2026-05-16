using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Data;

public class AcademyDbContext(DbContextOptions<AcademyDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<PartnershipLead> PartnershipLeads => Set<PartnershipLead>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<SchoolCoordinator> SchoolCoordinators => Set<SchoolCoordinator>();
    public DbSet<CourseSession> CourseSessions => Set<CourseSession>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<CohortEnrollment> CohortEnrollments => Set<CohortEnrollment>();
    public DbSet<SessionInstance> SessionInstances => Set<SessionInstance>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<CourseLesson> CourseLessons => Set<CourseLesson>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
    public DbSet<CourseApplicationQuestion> CourseApplicationQuestions => Set<CourseApplicationQuestion>();
    public DbSet<CourseApplication> CourseApplications => Set<CourseApplication>();
    public DbSet<CourseApplicationAnswer> CourseApplicationAnswers => Set<CourseApplicationAnswer>();
    public DbSet<LearningTask> LearningTasks => Set<LearningTask>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<StudentQuestion> StudentQuestions => Set<StudentQuestion>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<WeeklyChallenge> WeeklyChallenges => Set<WeeklyChallenge>();
    public DbSet<StudentChallengeProgress> StudentChallengeProgress => Set<StudentChallengeProgress>();
    public DbSet<EnrollmentOrder> EnrollmentOrders => Set<EnrollmentOrder>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<UserNotificationSetting> UserNotificationSettings => Set<UserNotificationSetting>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StudentProject> StudentProjects => Set<StudentProject>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Course>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.PriceEgp).HasColumnType("decimal(18,2)");
            entity.HasMany(x => x.Modules).WithOne(x => x.Course).HasForeignKey(x => x.CourseId);
            entity.HasMany(x => x.CourseSessions).WithOne(x => x.Course).HasForeignKey(x => x.CourseId);
            entity.HasMany(x => x.LiveSessions).WithOne(x => x.Course).HasForeignKey(x => x.CourseId);
        });

        builder.Entity<CourseSession>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.SessionNumber }).IsUnique();
            entity.Property(x => x.SessionType).HasConversion<string>().HasMaxLength(50);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.Property(x => x.EnrollmentStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.UnitPriceEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountAmountEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.FinalPriceEgp).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.CourseId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.EnrollmentOrder).WithMany(x => x.Enrollments).HasForeignKey(x => x.EnrollmentOrderId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<EnrollmentOrder>(entity =>
        {
            entity.Property(x => x.OrderType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SubtotalEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountAmountEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalAmountEgp).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<School>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.PartnershipStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.PartnerDiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(x => x.BundleDiscountPercent).HasColumnType("decimal(5,2)");
        });

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.ReferralCode).IsUnique();
            entity.Property(x => x.ExperienceLevel).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(x => x.School).WithMany(x => x.Students).HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ParentProfile>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<StaffProfile>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<SchoolCoordinator>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.SchoolId }).IsUnique();
            entity.HasOne(x => x.School).WithMany(x => x.Coordinators).HasForeignKey(x => x.SchoolId);
        });

        builder.Entity<Cohort>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Mode).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.CourseId, x.Name }).IsUnique();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne(x => x.EngineerUser).WithMany().HasForeignKey(x => x.EngineerUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CtaUser).WithMany().HasForeignKey(x => x.CtaUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CohortEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.CohortId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<SessionInstance>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SessionType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.CohortId, x.CourseSessionId }).IsUnique();
        });

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("Attendance");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.SessionInstanceId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<LiveSession>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        });

        builder.Entity<CourseLesson>(entity =>
        {
            entity.Property(x => x.SessionType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.CourseId, x.CohortId, x.WeekNumber, x.SortOrder });
        });

        builder.Entity<CourseMaterial>(entity =>
        {
            entity.Property(x => x.MaterialType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.CourseId, x.CohortId, x.CourseLessonId });
        });

        builder.Entity<CourseApplicationQuestion>(entity =>
        {
            entity.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.CourseId, x.CohortId, x.SortOrder });
        });

        builder.Entity<CourseApplication>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ReviewDecision).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ApplicationScore).HasColumnType("decimal(5,2)");
            entity.HasIndex(x => new { x.CourseId, x.CohortId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<CourseApplicationAnswer>(entity =>
        {
            entity.HasIndex(x => new { x.CourseApplicationId, x.CourseApplicationQuestionId }).IsUnique();
        });

        builder.Entity<LearningTask>(entity =>
        {
            entity.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SubmissionType).HasConversion<string>().HasMaxLength(50);
            entity.HasMany(x => x.Submissions).WithOne(x => x.LearningTask).HasForeignKey(x => x.LearningTaskId);
        });

        builder.Entity<TaskSubmission>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.LearningTaskId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.GradedByUser).WithMany().HasForeignKey(x => x.GradedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Quiz>(entity =>
        {
            entity.Property(x => x.QuizType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ShowAnswersAfter).HasConversion<string>().HasMaxLength(50);
        });

        builder.Entity<Question>(entity =>
        {
            entity.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(50);
            entity.HasMany(x => x.Options).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId);
        });

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.Property(x => x.Percentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(x => new { x.QuizId, x.StudentUserId, x.AttemptNumber }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<XpTransaction>(entity =>
        {
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.StudentUserId, x.CreatedAt });
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Badge>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<StudentBadge>(entity =>
        {
            entity.HasIndex(x => new { x.BadgeId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.Badge).WithMany(x => x.StudentBadges).HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<StudentChallengeProgress>(entity =>
        {
            entity.HasIndex(x => new { x.WeeklyChallengeId, x.StudentUserId }).IsUnique();
            entity.HasOne(x => x.WeeklyChallenge).WithMany().HasForeignKey(x => x.WeeklyChallengeId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<PromoCode>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Value).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.AmountEgp).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Referral>(entity =>
        {
            entity.HasIndex(x => x.ReferralCode);
            entity.Property(x => x.DiscountCreditEgp).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.ReferrerUser).WithMany().HasForeignKey(x => x.ReferrerUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReferredUser).WithMany().HasForeignKey(x => x.ReferredUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShoppingCart>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SubtotalEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountAmountEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalEgp).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.StudentUserId, x.Status });
            entity.HasMany(x => x.Items).WithOne(x => x.ShoppingCart).HasForeignKey(x => x.ShoppingCartId);
        });

        builder.Entity<ShoppingCartItem>(entity =>
        {
            entity.Property(x => x.UnitPriceEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountAmountEgp).HasColumnType("decimal(18,2)");
            entity.Property(x => x.FinalPriceEgp).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.ShoppingCartId, x.CourseId }).IsUnique();
        });

        builder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.Key, x.Channel }).IsUnique();
        });

        builder.Entity<NotificationMessage>(entity =>
        {
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.RecipientUserId, x.Status, x.CreatedAt });
        });

        builder.Entity<UserNotificationSetting>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<EmailVerificationCode>(entity =>
        {
            entity.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.UserId, x.Purpose, x.Status, x.ExpiresAt });
        });

        builder.Entity<StudentProject>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(50);
        });

        builder.Entity<Certificate>(entity =>
        {
            entity.HasIndex(x => x.CertificateNumber).IsUnique();
            entity.HasIndex(x => new { x.StudentUserId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.StudentUser).WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<PartnershipLead>(entity =>
        {
            entity.Property(x => x.LeadStatus).HasConversion<string>().HasMaxLength(50);
        });

        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            if (!foreignKey.IsOwnership)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}
