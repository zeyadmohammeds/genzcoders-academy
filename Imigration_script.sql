IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [RoleKey] nvarchar(max) NOT NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Courses] (
    [Id] uniqueidentifier NOT NULL,
    [Slug] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [ShortDescription] nvarchar(max) NOT NULL,
    [Outcome] nvarchar(max) NOT NULL,
    [MinimumAge] int NOT NULL,
    [PriceEgp] decimal(18,2) NOT NULL,
    [CoreSessions] int NOT NULL,
    [SupportSessions] int NOT NULL,
    [Level] nvarchar(max) NOT NULL,
    [IsFeatured] bit NOT NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id])
);

CREATE TABLE [PartnershipLeads] (
    [Id] uniqueidentifier NOT NULL,
    [SchoolName] nvarchar(max) NOT NULL,
    [ContactName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PartnershipLeads] PRIMARY KEY ([Id])
);

CREATE TABLE [Schools] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [CoordinatorName] nvarchar(max) NOT NULL,
    [CoordinatorEmail] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [PartnerDiscountPercent] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_Schools] PRIMARY KEY ([Id])
);

CREATE TABLE [StudentProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Age] int NOT NULL,
    [GradeLevel] nvarchar(max) NOT NULL,
    [XpTotal] int NOT NULL,
    [StreakDays] int NOT NULL,
    [ReferralCode] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_StudentProfiles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Assignments] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Brief] nvarchar(max) NOT NULL,
    [XpReward] int NOT NULL,
    [DueAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assignments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CourseModules] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [SortOrder] int NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [ProjectOutcome] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CourseModules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseModules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Enrollments] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [PromoCode] nvarchar(max) NULL,
    [FinalPriceEgp] decimal(18,2) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Enrollments_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [LiveSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [StartsAt] datetimeoffset NOT NULL,
    [DurationMinutes] int NOT NULL,
    [HostName] nvarchar(max) NOT NULL,
    [ZoomMeetingId] nvarchar(max) NOT NULL,
    [ZoomJoinUrl] nvarchar(max) NOT NULL,
    [ZoomSdkSignatureEndpoint] nvarchar(max) NULL,
    [EmbedEnabled] bit NOT NULL,
    CONSTRAINT [PK_LiveSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LiveSessions_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_Assignments_CourseId] ON [Assignments] ([CourseId]);

CREATE INDEX [IX_CourseModules_CourseId] ON [CourseModules] ([CourseId]);

CREATE UNIQUE INDEX [IX_Courses_Slug] ON [Courses] ([Slug]);

CREATE UNIQUE INDEX [IX_Enrollments_CourseId_StudentUserId] ON [Enrollments] ([CourseId], [StudentUserId]);

CREATE INDEX [IX_Enrollments_StudentUserId] ON [Enrollments] ([StudentUserId]);

CREATE INDEX [IX_LiveSessions_CourseId] ON [LiveSessions] ([CourseId]);

CREATE UNIQUE INDEX [IX_StudentProfiles_ReferralCode] ON [StudentProfiles] ([ReferralCode]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513012741_InitialAcademyPlatform', N'10.0.8');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AspNetRoleClaims] DROP CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId];

ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId];

ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId];

ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];

ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId];

ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId];

ALTER TABLE [CourseModules] DROP CONSTRAINT [FK_CourseModules_Courses_CourseId];

ALTER TABLE [Enrollments] DROP CONSTRAINT [FK_Enrollments_AspNetUsers_StudentUserId];

ALTER TABLE [Enrollments] DROP CONSTRAINT [FK_Enrollments_Courses_CourseId];

ALTER TABLE [LiveSessions] DROP CONSTRAINT [FK_LiveSessions_Courses_CourseId];

DROP TABLE [Assignments];

ALTER TABLE [StudentProfiles] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [StudentProfiles] ADD [LastActiveAt] datetimeoffset NULL;

ALTER TABLE [StudentProfiles] ADD [ParentUserId] uniqueidentifier NULL;

ALTER TABLE [StudentProfiles] ADD [ReferredByUserId] uniqueidentifier NULL;

ALTER TABLE [StudentProfiles] ADD [SchoolId] uniqueidentifier NULL;

ALTER TABLE [StudentProfiles] ADD [StreakFreezes] int NOT NULL DEFAULT 0;

ALTER TABLE [Schools] ADD [Address] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [BundleDiscountPercent] decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Schools] ADD [City] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [Schools] ADD [Email] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Schools] ADD [LogoUrl] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [MouDocumentUrl] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [MouSigned] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Schools] ADD [MouSignedAt] datetimeoffset NULL;

ALTER TABLE [Schools] ADD [PartnerSince] date NULL;

ALTER TABLE [Schools] ADD [PartnershipStatus] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Schools] ADD [Phone] nvarchar(max) NULL;

ALTER TABLE [Schools] ADD [Type] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [PartnershipLeads] ADD [AssignedTo] nvarchar(max) NULL;

ALTER TABLE [PartnershipLeads] ADD [FollowUpAt] datetimeoffset NULL;

ALTER TABLE [PartnershipLeads] ADD [LeadStatus] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [PartnershipLeads] ADD [Source] nvarchar(max) NULL;

ALTER TABLE [LiveSessions] ADD [CohortId] uniqueidentifier NULL;

ALTER TABLE [LiveSessions] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [LiveSessions] ADD [HostUserId] uniqueidentifier NULL;

ALTER TABLE [LiveSessions] ADD [RecordingUrl] nvarchar(max) NULL;

ALTER TABLE [LiveSessions] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [LiveSessions] ADD [ZoomMeetingPassword] nvarchar(max) NULL;

ALTER TABLE [Enrollments] ADD [CohortId] uniqueidentifier NULL;

ALTER TABLE [Enrollments] ADD [CompletedAt] datetimeoffset NULL;

ALTER TABLE [Enrollments] ADD [DiscountAmountEgp] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Enrollments] ADD [EnrollmentOrderId] uniqueidentifier NULL;

ALTER TABLE [Enrollments] ADD [EnrollmentStatus] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Enrollments] ADD [UnitPriceEgp] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Courses] ADD [ColorHex] nvarchar(max) NULL;

ALTER TABLE [Courses] ADD [CoverImageUrl] nvarchar(max) NULL;

ALTER TABLE [Courses] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [Courses] ADD [Description] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Courses] ADD [IconName] nvarchar(max) NULL;

ALTER TABLE [Courses] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Courses] ADD [MaximumAge] int NULL;

ALTER TABLE [Courses] ADD [Phase] int NOT NULL DEFAULT 0;

ALTER TABLE [Courses] ADD [SkillsTaughtJson] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Courses] ADD [SortOrder] int NOT NULL DEFAULT 0;

ALTER TABLE [Courses] ADD [Subtitle] nvarchar(max) NULL;

ALTER TABLE [AspNetUsers] ADD [City] nvarchar(max) NULL;

ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetimeoffset NULL;

ALTER TABLE [AspNetUsers] ADD [PreferredLanguage] nvarchar(max) NULL;

ALTER TABLE [AspNetUsers] ADD [UpdatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [AspNetUsers] ADD [VerifiedAt] datetimeoffset NULL;

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [Action] nvarchar(max) NOT NULL,
    [EntityName] nvarchar(max) NOT NULL,
    [EntityId] uniqueidentifier NULL,
    [ChangesJson] nvarchar(max) NOT NULL,
    [IpAddress] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Badges] (
    [Id] uniqueidentifier NOT NULL,
    [Slug] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IconUrl] nvarchar(max) NULL,
    [ColorHex] nvarchar(max) NULL,
    [XpReward] int NOT NULL,
    [CriteriaJson] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Badges] PRIMARY KEY ([Id])
);

CREATE TABLE [Cohorts] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [EngineerUserId] uniqueidentifier NULL,
    [CtaUserId] uniqueidentifier NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [MaxStudents] int NOT NULL,
    [SessionLink] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Cohorts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Cohorts_AspNetUsers_CtaUserId] FOREIGN KEY ([CtaUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Cohorts_AspNetUsers_EngineerUserId] FOREIGN KEY ([EngineerUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Cohorts_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE TABLE [CourseSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [SessionNumber] int NOT NULL,
    [SessionType] nvarchar(50) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Outcome] nvarchar(max) NOT NULL,
    [Principle] nvarchar(max) NULL,
    [DurationMinutes] int NOT NULL,
    [MaterialsJson] nvarchar(max) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_CourseSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseSessions_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE TABLE [NotificationMessages] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientUserId] uniqueidentifier NULL,
    [Channel] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Destination] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NOT NULL,
    [ScheduledFor] datetimeoffset NULL,
    [SentAt] datetimeoffset NULL,
    [ReadAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_NotificationMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NotificationMessages_AspNetUsers_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [NotificationTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Key] nvarchar(450) NOT NULL,
    [Channel] nvarchar(450) NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY ([Id])
);

CREATE TABLE [ParentProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [WhatsAppNumber] nvarchar(max) NULL,
    [NotificationPreferencesJson] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ParentProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ParentProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [PromoCodes] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DiscountType] nvarchar(50) NOT NULL,
    [Value] decimal(18,2) NOT NULL,
    [MaxUses] int NULL,
    [UsedCount] int NOT NULL,
    [StartsAt] datetimeoffset NULL,
    [ExpiresAt] datetimeoffset NULL,
    [CourseId] uniqueidentifier NULL,
    [SchoolId] uniqueidentifier NULL,
    [AppliesToBundle] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PromoCodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromoCodes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]),
    CONSTRAINT [FK_PromoCodes_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id])
);

CREATE TABLE [Referrals] (
    [Id] uniqueidentifier NOT NULL,
    [ReferrerUserId] uniqueidentifier NOT NULL,
    [ReferredUserId] uniqueidentifier NULL,
    [ReferralCode] nvarchar(450) NOT NULL,
    [RegistrationXpAwarded] int NOT NULL,
    [EnrollmentXpAwarded] int NOT NULL,
    [DiscountCreditEgp] decimal(18,2) NOT NULL,
    [ConvertedToPaidEnrollment] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ConvertedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Referrals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Referrals_AspNetUsers_ReferredUserId] FOREIGN KEY ([ReferredUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Referrals_AspNetUsers_ReferrerUserId] FOREIGN KEY ([ReferrerUserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [SchoolCoordinators] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SchoolId] uniqueidentifier NOT NULL,
    [IsPrimary] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_SchoolCoordinators] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SchoolCoordinators_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_SchoolCoordinators_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id])
);

CREATE TABLE [StaffProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Bio] nvarchar(max) NOT NULL,
    [Specialization] nvarchar(max) NOT NULL,
    [LinkedInUrl] nvarchar(max) NULL,
    [IsCta] bit NOT NULL,
    [CtaSchoolId] uniqueidentifier NULL,
    [MentorXp] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_StaffProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StaffProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_StaffProfiles_Schools_CtaSchoolId] FOREIGN KEY ([CtaSchoolId]) REFERENCES [Schools] ([Id])
);

CREATE TABLE [WeeklyChallenges] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [XpReward] int NOT NULL,
    [StartsAt] datetimeoffset NOT NULL,
    [EndsAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_WeeklyChallenges] PRIMARY KEY ([Id])
);

CREATE TABLE [XpTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [Amount] int NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_XpTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_XpTransactions_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [StudentBadges] (
    [Id] uniqueidentifier NOT NULL,
    [BadgeId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [AwardedAt] datetimeoffset NOT NULL,
    [AwardedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_StudentBadges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentBadges_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_StudentBadges_Badges_BadgeId] FOREIGN KEY ([BadgeId]) REFERENCES [Badges] ([Id])
);

CREATE TABLE [Certificates] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [CertificateNumber] nvarchar(450) NOT NULL,
    [PdfUrl] nvarchar(max) NULL,
    [IssuedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Certificates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Certificates_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Certificates_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_Certificates_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE TABLE [CohortEnrollments] (
    [Id] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [EnrollmentId] uniqueidentifier NULL,
    [EnrolledAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_CohortEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CohortEnrollments_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_CohortEnrollments_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_CohortEnrollments_Enrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [Enrollments] ([Id])
);

CREATE TABLE [StudentQuestions] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [AssignedToUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ResolvedAt] datetimeoffset NULL,
    CONSTRAINT [PK_StudentQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentQuestions_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_StudentQuestions_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id])
);

CREATE TABLE [LearningTasks] (
    [Id] uniqueidentifier NOT NULL,
    [CourseSessionId] uniqueidentifier NULL,
    [CohortId] uniqueidentifier NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Instructions] nvarchar(max) NOT NULL,
    [TaskType] nvarchar(50) NOT NULL,
    [SubmissionType] nvarchar(50) NOT NULL,
    [MaxScore] int NOT NULL,
    [XpReward] int NOT NULL,
    [DueHoursAfterSession] int NOT NULL,
    [IsRequired] bit NOT NULL,
    [RubricJson] nvarchar(max) NOT NULL,
    [SampleSolutionUrl] nvarchar(max) NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_LearningTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LearningTasks_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_LearningTasks_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_LearningTasks_CourseSessions_CourseSessionId] FOREIGN KEY ([CourseSessionId]) REFERENCES [CourseSessions] ([Id])
);

CREATE TABLE [Quizzes] (
    [Id] uniqueidentifier NOT NULL,
    [CourseSessionId] uniqueidentifier NULL,
    [CohortId] uniqueidentifier NULL,
    [Title] nvarchar(max) NOT NULL,
    [QuizType] nvarchar(50) NOT NULL,
    [TimeLimitMinutes] int NULL,
    [MaxAttempts] int NOT NULL,
    [PassScore] int NOT NULL,
    [XpReward] int NOT NULL,
    [ShuffleQuestions] bit NOT NULL,
    [ShowAnswersAfter] nvarchar(50) NOT NULL,
    [AvailableFrom] datetimeoffset NULL,
    [AvailableUntil] datetimeoffset NULL,
    [IsPublished] bit NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Quizzes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Quizzes_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Quizzes_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_Quizzes_CourseSessions_CourseSessionId] FOREIGN KEY ([CourseSessionId]) REFERENCES [CourseSessions] ([Id])
);

CREATE TABLE [SessionInstances] (
    [Id] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NOT NULL,
    [CourseSessionId] uniqueidentifier NOT NULL,
    [ScheduledAt] datetimeoffset NOT NULL,
    [DurationMinutes] int NOT NULL,
    [SessionLink] nvarchar(max) NULL,
    [RecordingUrl] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_SessionInstances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SessionInstances_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_SessionInstances_CourseSessions_CourseSessionId] FOREIGN KEY ([CourseSessionId]) REFERENCES [CourseSessions] ([Id])
);

CREATE TABLE [EnrollmentOrders] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [OrderType] nvarchar(50) NOT NULL,
    [SubtotalEgp] decimal(18,2) NOT NULL,
    [DiscountAmountEgp] decimal(18,2) NOT NULL,
    [DiscountReason] nvarchar(max) NULL,
    [PromoCodeId] uniqueidentifier NULL,
    [ReferralCode] nvarchar(max) NULL,
    [TotalAmountEgp] decimal(18,2) NOT NULL,
    [PaymentStatus] nvarchar(50) NOT NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [PaymentReference] nvarchar(max) NULL,
    [PaidAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_EnrollmentOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EnrollmentOrders_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_EnrollmentOrders_PromoCodes_PromoCodeId] FOREIGN KEY ([PromoCodeId]) REFERENCES [PromoCodes] ([Id])
);

CREATE TABLE [StudentChallengeProgress] (
    [Id] uniqueidentifier NOT NULL,
    [WeeklyChallengeId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [CurrentValue] int NOT NULL,
    [TargetValue] int NOT NULL,
    [Completed] bit NOT NULL,
    [CompletedAt] datetimeoffset NULL,
    CONSTRAINT [PK_StudentChallengeProgress] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentChallengeProgress_WeeklyChallenges_WeeklyChallengeId] FOREIGN KEY ([WeeklyChallengeId]) REFERENCES [WeeklyChallenges] ([Id])
);

CREATE TABLE [TaskSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [LearningTaskId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [SubmissionUrl] nvarchar(max) NULL,
    [RepositoryUrl] nvarchar(max) NULL,
    [SubmissionText] nvarchar(max) NULL,
    [SubmittedAt] datetimeoffset NOT NULL,
    [IsLate] bit NOT NULL,
    [Score] int NULL,
    [Feedback] nvarchar(max) NULL,
    [RubricScoresJson] nvarchar(max) NOT NULL,
    [GradedByUserId] uniqueidentifier NULL,
    [GradedAt] datetimeoffset NULL,
    [XpAwarded] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_TaskSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskSubmissions_AspNetUsers_GradedByUserId] FOREIGN KEY ([GradedByUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_TaskSubmissions_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_TaskSubmissions_LearningTasks_LearningTaskId] FOREIGN KEY ([LearningTaskId]) REFERENCES [LearningTasks] ([Id])
);

CREATE TABLE [Questions] (
    [Id] uniqueidentifier NOT NULL,
    [QuizId] uniqueidentifier NOT NULL,
    [QuestionText] nvarchar(max) NOT NULL,
    [QuestionType] nvarchar(50) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [CodeSnippet] nvarchar(max) NULL,
    [Points] int NOT NULL,
    [Explanation] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Questions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id])
);

CREATE TABLE [QuizAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [QuizId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [AttemptNumber] int NOT NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [SubmittedAt] datetimeoffset NULL,
    [Score] int NULL,
    [Percentage] decimal(5,2) NULL,
    [Passed] bit NULL,
    [XpAwarded] int NOT NULL,
    [TimeTakenSeconds] int NULL,
    CONSTRAINT [PK_QuizAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizAttempts_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id])
);

CREATE TABLE [Attendance] (
    [Id] uniqueidentifier NOT NULL,
    [SessionInstanceId] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [JoinedAt] datetimeoffset NULL,
    [LeftAt] datetimeoffset NULL,
    [XpEarned] int NOT NULL,
    [MarkedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Attendance] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendance_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Attendance_SessionInstances_SessionInstanceId] FOREIGN KEY ([SessionInstanceId]) REFERENCES [SessionInstances] ([Id])
);

CREATE TABLE [PaymentTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [EnrollmentOrderId] uniqueidentifier NOT NULL,
    [Provider] nvarchar(max) NOT NULL,
    [ProviderTransactionId] nvarchar(max) NULL,
    [AmountEgp] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RawPayloadJson] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentTransactions_EnrollmentOrders_EnrollmentOrderId] FOREIGN KEY ([EnrollmentOrderId]) REFERENCES [EnrollmentOrders] ([Id])
);

CREATE TABLE [StudentProjects] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NULL,
    [TaskSubmissionId] uniqueidentifier NULL,
    [Slug] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ProjectUrl] nvarchar(max) NULL,
    [RepositoryUrl] nvarchar(max) NULL,
    [CoverImageUrl] nvarchar(max) NULL,
    [Visibility] nvarchar(50) NOT NULL,
    [IsFeatured] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_StudentProjects] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentProjects_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_StudentProjects_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]),
    CONSTRAINT [FK_StudentProjects_TaskSubmissions_TaskSubmissionId] FOREIGN KEY ([TaskSubmissionId]) REFERENCES [TaskSubmissions] ([Id])
);

CREATE TABLE [QuestionOptions] (
    [Id] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [OptionText] nvarchar(max) NOT NULL,
    [IsCorrect] bit NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_QuestionOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuestionOptions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id])
);

CREATE TABLE [QuestionAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [QuizAttemptId] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [SelectedOptionId] uniqueidentifier NULL,
    [TextAnswer] nvarchar(max) NULL,
    [IsCorrect] bit NULL,
    [PointsEarned] int NOT NULL,
    [AnsweredAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_QuestionAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuestionAnswers_QuestionOptions_SelectedOptionId] FOREIGN KEY ([SelectedOptionId]) REFERENCES [QuestionOptions] ([Id]),
    CONSTRAINT [FK_QuestionAnswers_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]),
    CONSTRAINT [FK_QuestionAnswers_QuizAttempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [QuizAttempts] ([Id])
);

CREATE INDEX [IX_StudentProfiles_SchoolId] ON [StudentProfiles] ([SchoolId]);

CREATE UNIQUE INDEX [IX_StudentProfiles_UserId] ON [StudentProfiles] ([UserId]);

CREATE INDEX [IX_LiveSessions_CohortId] ON [LiveSessions] ([CohortId]);

CREATE INDEX [IX_Enrollments_CohortId] ON [Enrollments] ([CohortId]);

CREATE INDEX [IX_Enrollments_EnrollmentOrderId] ON [Enrollments] ([EnrollmentOrderId]);

CREATE UNIQUE INDEX [IX_Attendance_SessionInstanceId_StudentUserId] ON [Attendance] ([SessionInstanceId], [StudentUserId]);

CREATE INDEX [IX_Attendance_StudentUserId] ON [Attendance] ([StudentUserId]);

CREATE UNIQUE INDEX [IX_Badges_Slug] ON [Badges] ([Slug]);

CREATE UNIQUE INDEX [IX_Certificates_CertificateNumber] ON [Certificates] ([CertificateNumber]);

CREATE INDEX [IX_Certificates_CohortId] ON [Certificates] ([CohortId]);

CREATE INDEX [IX_Certificates_CourseId] ON [Certificates] ([CourseId]);

CREATE UNIQUE INDEX [IX_Certificates_StudentUserId_CourseId] ON [Certificates] ([StudentUserId], [CourseId]);

CREATE UNIQUE INDEX [IX_CohortEnrollments_CohortId_StudentUserId] ON [CohortEnrollments] ([CohortId], [StudentUserId]);

CREATE INDEX [IX_CohortEnrollments_EnrollmentId] ON [CohortEnrollments] ([EnrollmentId]);

CREATE INDEX [IX_CohortEnrollments_StudentUserId] ON [CohortEnrollments] ([StudentUserId]);

CREATE UNIQUE INDEX [IX_Cohorts_CourseId_Name] ON [Cohorts] ([CourseId], [Name]);

CREATE INDEX [IX_Cohorts_CtaUserId] ON [Cohorts] ([CtaUserId]);

CREATE INDEX [IX_Cohorts_EngineerUserId] ON [Cohorts] ([EngineerUserId]);

CREATE UNIQUE INDEX [IX_CourseSessions_CourseId_SessionNumber] ON [CourseSessions] ([CourseId], [SessionNumber]);

CREATE INDEX [IX_EnrollmentOrders_PromoCodeId] ON [EnrollmentOrders] ([PromoCodeId]);

CREATE INDEX [IX_EnrollmentOrders_StudentUserId] ON [EnrollmentOrders] ([StudentUserId]);

CREATE INDEX [IX_LearningTasks_CohortId] ON [LearningTasks] ([CohortId]);

CREATE INDEX [IX_LearningTasks_CourseSessionId] ON [LearningTasks] ([CourseSessionId]);

CREATE INDEX [IX_LearningTasks_CreatedByUserId] ON [LearningTasks] ([CreatedByUserId]);

CREATE INDEX [IX_NotificationMessages_RecipientUserId_Status_CreatedAt] ON [NotificationMessages] ([RecipientUserId], [Status], [CreatedAt]);

CREATE UNIQUE INDEX [IX_NotificationTemplates_Key_Channel] ON [NotificationTemplates] ([Key], [Channel]);

CREATE UNIQUE INDEX [IX_ParentProfiles_UserId] ON [ParentProfiles] ([UserId]);

CREATE INDEX [IX_PaymentTransactions_EnrollmentOrderId] ON [PaymentTransactions] ([EnrollmentOrderId]);

CREATE UNIQUE INDEX [IX_PromoCodes_Code] ON [PromoCodes] ([Code]);

CREATE INDEX [IX_PromoCodes_CourseId] ON [PromoCodes] ([CourseId]);

CREATE INDEX [IX_PromoCodes_SchoolId] ON [PromoCodes] ([SchoolId]);

CREATE INDEX [IX_QuestionAnswers_QuestionId] ON [QuestionAnswers] ([QuestionId]);

CREATE INDEX [IX_QuestionAnswers_QuizAttemptId] ON [QuestionAnswers] ([QuizAttemptId]);

CREATE INDEX [IX_QuestionAnswers_SelectedOptionId] ON [QuestionAnswers] ([SelectedOptionId]);

CREATE INDEX [IX_QuestionOptions_QuestionId] ON [QuestionOptions] ([QuestionId]);

CREATE INDEX [IX_Questions_QuizId] ON [Questions] ([QuizId]);

CREATE UNIQUE INDEX [IX_QuizAttempts_QuizId_StudentUserId_AttemptNumber] ON [QuizAttempts] ([QuizId], [StudentUserId], [AttemptNumber]);

CREATE INDEX [IX_QuizAttempts_StudentUserId] ON [QuizAttempts] ([StudentUserId]);

CREATE INDEX [IX_Quizzes_CohortId] ON [Quizzes] ([CohortId]);

CREATE INDEX [IX_Quizzes_CourseSessionId] ON [Quizzes] ([CourseSessionId]);

CREATE INDEX [IX_Quizzes_CreatedByUserId] ON [Quizzes] ([CreatedByUserId]);

CREATE INDEX [IX_Referrals_ReferralCode] ON [Referrals] ([ReferralCode]);

CREATE INDEX [IX_Referrals_ReferredUserId] ON [Referrals] ([ReferredUserId]);

CREATE INDEX [IX_Referrals_ReferrerUserId] ON [Referrals] ([ReferrerUserId]);

CREATE INDEX [IX_SchoolCoordinators_SchoolId] ON [SchoolCoordinators] ([SchoolId]);

CREATE UNIQUE INDEX [IX_SchoolCoordinators_UserId_SchoolId] ON [SchoolCoordinators] ([UserId], [SchoolId]);

CREATE UNIQUE INDEX [IX_SessionInstances_CohortId_CourseSessionId] ON [SessionInstances] ([CohortId], [CourseSessionId]);

CREATE INDEX [IX_SessionInstances_CourseSessionId] ON [SessionInstances] ([CourseSessionId]);

CREATE INDEX [IX_StaffProfiles_CtaSchoolId] ON [StaffProfiles] ([CtaSchoolId]);

CREATE UNIQUE INDEX [IX_StaffProfiles_UserId] ON [StaffProfiles] ([UserId]);

CREATE UNIQUE INDEX [IX_StudentBadges_BadgeId_StudentUserId] ON [StudentBadges] ([BadgeId], [StudentUserId]);

CREATE INDEX [IX_StudentBadges_StudentUserId] ON [StudentBadges] ([StudentUserId]);

CREATE UNIQUE INDEX [IX_StudentChallengeProgress_WeeklyChallengeId_StudentUserId] ON [StudentChallengeProgress] ([WeeklyChallengeId], [StudentUserId]);

CREATE INDEX [IX_StudentProjects_CourseId] ON [StudentProjects] ([CourseId]);

CREATE UNIQUE INDEX [IX_StudentProjects_Slug] ON [StudentProjects] ([Slug]);

CREATE INDEX [IX_StudentProjects_StudentUserId] ON [StudentProjects] ([StudentUserId]);

CREATE INDEX [IX_StudentProjects_TaskSubmissionId] ON [StudentProjects] ([TaskSubmissionId]);

CREATE INDEX [IX_StudentQuestions_CohortId] ON [StudentQuestions] ([CohortId]);

CREATE INDEX [IX_StudentQuestions_StudentUserId] ON [StudentQuestions] ([StudentUserId]);

CREATE INDEX [IX_TaskSubmissions_GradedByUserId] ON [TaskSubmissions] ([GradedByUserId]);

CREATE UNIQUE INDEX [IX_TaskSubmissions_LearningTaskId_StudentUserId] ON [TaskSubmissions] ([LearningTaskId], [StudentUserId]);

CREATE INDEX [IX_TaskSubmissions_StudentUserId] ON [TaskSubmissions] ([StudentUserId]);

CREATE INDEX [IX_XpTransactions_StudentUserId_CreatedAt] ON [XpTransactions] ([StudentUserId], [CreatedAt]);

ALTER TABLE [AspNetRoleClaims] ADD CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]);

ALTER TABLE [AspNetUserClaims] ADD CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [AspNetUserLogins] ADD CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]);

ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [AspNetUserTokens] ADD CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [CourseModules] ADD CONSTRAINT [FK_CourseModules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]);

ALTER TABLE [Enrollments] ADD CONSTRAINT [FK_Enrollments_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [Enrollments] ADD CONSTRAINT [FK_Enrollments_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]);

ALTER TABLE [Enrollments] ADD CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]);

ALTER TABLE [Enrollments] ADD CONSTRAINT [FK_Enrollments_EnrollmentOrders_EnrollmentOrderId] FOREIGN KEY ([EnrollmentOrderId]) REFERENCES [EnrollmentOrders] ([Id]);

ALTER TABLE [LiveSessions] ADD CONSTRAINT [FK_LiveSessions_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]);

ALTER TABLE [LiveSessions] ADD CONSTRAINT [FK_LiveSessions_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]);

ALTER TABLE [StudentProfiles] ADD CONSTRAINT [FK_StudentProfiles_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513014457_AdvancedAcademyBackend', N'10.0.8');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [SessionInstances] ADD [SessionType] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [SessionInstances] ADD [WeekNumber] int NOT NULL DEFAULT 0;

ALTER TABLE [SessionInstances] ADD [WeekTitle] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Cohorts] ADD [AutoAcceptPaidApplications] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Cohorts] ADD [CurrentStudents] int NOT NULL DEFAULT 0;

ALTER TABLE [Cohorts] ADD [Description] nvarchar(max) NULL;

ALTER TABLE [Cohorts] ADD [IsEnrollmentOpen] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Cohorts] ADD [Mode] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Cohorts] ADD [RequireEngineerApproval] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Cohorts] ADD [Slug] nvarchar(450) NOT NULL DEFAULT N'';

ALTER TABLE [Cohorts] ADD [ZoomJoinUrl] nvarchar(max) NULL;

ALTER TABLE [Cohorts] ADD [ZoomMeetingId] nvarchar(max) NULL;

ALTER TABLE [Cohorts] ADD [ZoomStartUrl] nvarchar(max) NULL;

CREATE TABLE [CourseApplicationQuestions] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [QuestionType] nvarchar(50) NOT NULL,
    [QuestionText] nvarchar(max) NOT NULL,
    [HelpText] nvarchar(max) NULL,
    [OptionsJson] nvarchar(max) NOT NULL,
    [CorrectAnswer] nvarchar(max) NULL,
    [IsRequired] bit NOT NULL,
    [AutoGrade] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_CourseApplicationQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseApplicationQuestions_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_CourseApplicationQuestions_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE TABLE [CourseApplications] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ApplicationScore] decimal(5,2) NOT NULL,
    [QuestionsPassed] bit NOT NULL,
    [PaymentUnlocked] bit NOT NULL,
    [PaymentCompleted] bit NOT NULL,
    [EnrollmentOrderId] uniqueidentifier NULL,
    [ReviewedByUserId] uniqueidentifier NULL,
    [ReviewDecision] nvarchar(50) NOT NULL,
    [ReviewNotes] nvarchar(max) NULL,
    [SubmittedAt] datetimeoffset NOT NULL,
    [PaidAt] datetimeoffset NULL,
    [ReviewedAt] datetimeoffset NULL,
    [AcceptedAt] datetimeoffset NULL,
    CONSTRAINT [PK_CourseApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseApplications_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_CourseApplications_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_CourseApplications_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]),
    CONSTRAINT [FK_CourseApplications_EnrollmentOrders_EnrollmentOrderId] FOREIGN KEY ([EnrollmentOrderId]) REFERENCES [EnrollmentOrders] ([Id])
);

CREATE TABLE [CourseLessons] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [CourseSessionId] uniqueidentifier NULL,
    [WeekNumber] int NOT NULL,
    [SessionType] nvarchar(50) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [ContentMarkdown] nvarchar(max) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsPublished] bit NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_CourseLessons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseLessons_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_CourseLessons_CourseSessions_CourseSessionId] FOREIGN KEY ([CourseSessionId]) REFERENCES [CourseSessions] ([Id]),
    CONSTRAINT [FK_CourseLessons_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE TABLE [UserNotificationSettings] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [InAppEnabled] bit NOT NULL,
    [EmailEnabled] bit NOT NULL,
    [WhatsAppEnabled] bit NOT NULL,
    [SmsEnabled] bit NOT NULL,
    [WhatsAppNumber] nvarchar(max) NULL,
    [EmailOverride] nvarchar(max) NULL,
    [MutedTemplateKeysJson] nvarchar(max) NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserNotificationSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserNotificationSettings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [CourseApplicationAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [CourseApplicationId] uniqueidentifier NOT NULL,
    [CourseApplicationQuestionId] uniqueidentifier NOT NULL,
    [AnswerText] nvarchar(max) NOT NULL,
    [IsCorrect] bit NULL,
    [ScoreAwarded] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_CourseApplicationAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseApplicationAnswers_CourseApplicationQuestions_CourseApplicationQuestionId] FOREIGN KEY ([CourseApplicationQuestionId]) REFERENCES [CourseApplicationQuestions] ([Id]),
    CONSTRAINT [FK_CourseApplicationAnswers_CourseApplications_CourseApplicationId] FOREIGN KEY ([CourseApplicationId]) REFERENCES [CourseApplications] ([Id])
);

CREATE TABLE [CourseMaterials] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [CourseLessonId] uniqueidentifier NULL,
    [MaterialType] nvarchar(50) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsDownloadable] bit NOT NULL,
    [IsPublished] bit NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_CourseMaterials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseMaterials_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_CourseMaterials_CourseLessons_CourseLessonId] FOREIGN KEY ([CourseLessonId]) REFERENCES [CourseLessons] ([Id]),
    CONSTRAINT [FK_CourseMaterials_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id])
);

CREATE UNIQUE INDEX [IX_Cohorts_Slug] ON [Cohorts] ([Slug]);

CREATE UNIQUE INDEX [IX_CourseApplicationAnswers_CourseApplicationId_CourseApplicationQuestionId] ON [CourseApplicationAnswers] ([CourseApplicationId], [CourseApplicationQuestionId]);

CREATE INDEX [IX_CourseApplicationAnswers_CourseApplicationQuestionId] ON [CourseApplicationAnswers] ([CourseApplicationQuestionId]);

CREATE INDEX [IX_CourseApplicationQuestions_CohortId] ON [CourseApplicationQuestions] ([CohortId]);

CREATE INDEX [IX_CourseApplicationQuestions_CourseId_CohortId_SortOrder] ON [CourseApplicationQuestions] ([CourseId], [CohortId], [SortOrder]);

CREATE INDEX [IX_CourseApplications_CohortId] ON [CourseApplications] ([CohortId]);

CREATE UNIQUE INDEX [IX_CourseApplications_CourseId_CohortId_StudentUserId] ON [CourseApplications] ([CourseId], [CohortId], [StudentUserId]) WHERE [CohortId] IS NOT NULL;

CREATE INDEX [IX_CourseApplications_EnrollmentOrderId] ON [CourseApplications] ([EnrollmentOrderId]);

CREATE INDEX [IX_CourseApplications_StudentUserId] ON [CourseApplications] ([StudentUserId]);

CREATE INDEX [IX_CourseLessons_CohortId] ON [CourseLessons] ([CohortId]);

CREATE INDEX [IX_CourseLessons_CourseId_CohortId_WeekNumber_SortOrder] ON [CourseLessons] ([CourseId], [CohortId], [WeekNumber], [SortOrder]);

CREATE INDEX [IX_CourseLessons_CourseSessionId] ON [CourseLessons] ([CourseSessionId]);

CREATE INDEX [IX_CourseMaterials_CohortId] ON [CourseMaterials] ([CohortId]);

CREATE INDEX [IX_CourseMaterials_CourseId_CohortId_CourseLessonId] ON [CourseMaterials] ([CourseId], [CohortId], [CourseLessonId]);

CREATE INDEX [IX_CourseMaterials_CourseLessonId] ON [CourseMaterials] ([CourseLessonId]);

CREATE UNIQUE INDEX [IX_UserNotificationSettings_UserId] ON [UserNotificationSettings] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513022537_ApplicationCourseRoomNotificationWorkflow', N'10.0.8');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [StudentProfiles] ADD [ExperienceLevel] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [StudentProfiles] ADD [Goals] nvarchar(max) NULL;

ALTER TABLE [StudentProfiles] ADD [InterestsJson] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [StudentProfiles] ADD [IsOnboardingCompleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [StudentProfiles] ADD [NationalId] nvarchar(max) NULL;

ALTER TABLE [StudentProfiles] ADD [OnboardingCompletedAt] datetimeoffset NULL;

ALTER TABLE [StudentProfiles] ADD [OnboardingSkippedAt] datetimeoffset NULL;

ALTER TABLE [StudentProfiles] ADD [PreferredTrack] nvarchar(max) NULL;

ALTER TABLE [StudentProfiles] ADD [ProfileCompletionXpAwarded] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [StudentProfiles] ADD [SchoolName] nvarchar(max) NULL;

CREATE TABLE [EmailVerificationCodes] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [CodeHash] nvarchar(max) NOT NULL,
    [VerificationTokenHash] nvarchar(max) NULL,
    [Purpose] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AttemptCount] int NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UsedAt] datetimeoffset NULL,
    CONSTRAINT [PK_EmailVerificationCodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailVerificationCodes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [ShoppingCarts] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [PromoCode] nvarchar(max) NULL,
    [ReferralCode] nvarchar(max) NULL,
    [SubtotalEgp] decimal(18,2) NOT NULL,
    [DiscountAmountEgp] decimal(18,2) NOT NULL,
    [DiscountSummary] nvarchar(max) NULL,
    [TotalEgp] decimal(18,2) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ShoppingCarts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShoppingCarts_AspNetUsers_StudentUserId] FOREIGN KEY ([StudentUserId]) REFERENCES [AspNetUsers] ([Id])
);

CREATE TABLE [ShoppingCartItems] (
    [Id] uniqueidentifier NOT NULL,
    [ShoppingCartId] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CohortId] uniqueidentifier NULL,
    [UnitPriceEgp] decimal(18,2) NOT NULL,
    [DiscountAmountEgp] decimal(18,2) NOT NULL,
    [FinalPriceEgp] decimal(18,2) NOT NULL,
    [IsBundleItem] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ShoppingCartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShoppingCartItems_Cohorts_CohortId] FOREIGN KEY ([CohortId]) REFERENCES [Cohorts] ([Id]),
    CONSTRAINT [FK_ShoppingCartItems_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]),
    CONSTRAINT [FK_ShoppingCartItems_ShoppingCarts_ShoppingCartId] FOREIGN KEY ([ShoppingCartId]) REFERENCES [ShoppingCarts] ([Id])
);

CREATE INDEX [IX_EmailVerificationCodes_UserId_Purpose_Status_ExpiresAt] ON [EmailVerificationCodes] ([UserId], [Purpose], [Status], [ExpiresAt]);

CREATE INDEX [IX_ShoppingCartItems_CohortId] ON [ShoppingCartItems] ([CohortId]);

CREATE INDEX [IX_ShoppingCartItems_CourseId] ON [ShoppingCartItems] ([CourseId]);

CREATE UNIQUE INDEX [IX_ShoppingCartItems_ShoppingCartId_CourseId] ON [ShoppingCartItems] ([ShoppingCartId], [CourseId]);

CREATE INDEX [IX_ShoppingCarts_StudentUserId_Status] ON [ShoppingCarts] ([StudentUserId], [Status]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513024922_SecureAuthOnboardingReferralCart', N'10.0.8');

COMMIT;
GO

