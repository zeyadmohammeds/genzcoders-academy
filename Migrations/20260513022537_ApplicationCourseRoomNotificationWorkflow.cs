using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenZCoders.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationCourseRoomNotificationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                table: "SessionInstances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "SessionInstances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WeekTitle",
                table: "SessionInstances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptPaidApplications",
                table: "Cohorts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStudents",
                table: "Cohorts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cohorts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnrollmentOpen",
                table: "Cohorts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Cohorts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequireEngineerApproval",
                table: "Cohorts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Cohorts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZoomJoinUrl",
                table: "Cohorts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZoomMeetingId",
                table: "Cohorts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZoomStartUrl",
                table: "Cohorts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseApplicationQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    AutoGrade = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseApplicationQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseApplicationQuestions_Cohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "Cohorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseApplicationQuestions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApplicationScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QuestionsPassed = table.Column<bool>(type: "bit", nullable: false),
                    PaymentUnlocked = table.Column<bool>(type: "bit", nullable: false),
                    PaymentCompleted = table.Column<bool>(type: "bit", nullable: false),
                    EnrollmentOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewDecision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseApplications_AspNetUsers_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseApplications_Cohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "Cohorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseApplications_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseApplications_EnrollmentOrders_EnrollmentOrderId",
                        column: x => x.EnrollmentOrderId,
                        principalTable: "EnrollmentOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseLessons_Cohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "Cohorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseLessons_CourseSessions_CourseSessionId",
                        column: x => x.CourseSessionId,
                        principalTable: "CourseSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseLessons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailOverride = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MutedTemplateKeysJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseApplicationAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseApplicationQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    ScoreAwarded = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseApplicationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseApplicationAnswers_CourseApplicationQuestions_CourseApplicationQuestionId",
                        column: x => x.CourseApplicationQuestionId,
                        principalTable: "CourseApplicationQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseApplicationAnswers_CourseApplications_CourseApplicationId",
                        column: x => x.CourseApplicationId,
                        principalTable: "CourseApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseLessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaterialType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDownloadable = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Cohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "Cohorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterials_CourseLessons_CourseLessonId",
                        column: x => x.CourseLessonId,
                        principalTable: "CourseLessons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_Slug",
                table: "Cohorts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplicationAnswers_CourseApplicationId_CourseApplicationQuestionId",
                table: "CourseApplicationAnswers",
                columns: new[] { "CourseApplicationId", "CourseApplicationQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplicationAnswers_CourseApplicationQuestionId",
                table: "CourseApplicationAnswers",
                column: "CourseApplicationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplicationQuestions_CohortId",
                table: "CourseApplicationQuestions",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplicationQuestions_CourseId_CohortId_SortOrder",
                table: "CourseApplicationQuestions",
                columns: new[] { "CourseId", "CohortId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplications_CohortId",
                table: "CourseApplications",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplications_CourseId_CohortId_StudentUserId",
                table: "CourseApplications",
                columns: new[] { "CourseId", "CohortId", "StudentUserId" },
                unique: true,
                filter: "[CohortId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplications_EnrollmentOrderId",
                table: "CourseApplications",
                column: "EnrollmentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseApplications_StudentUserId",
                table: "CourseApplications",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLessons_CohortId",
                table: "CourseLessons",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLessons_CourseId_CohortId_WeekNumber_SortOrder",
                table: "CourseLessons",
                columns: new[] { "CourseId", "CohortId", "WeekNumber", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseLessons_CourseSessionId",
                table: "CourseLessons",
                column: "CourseSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CohortId",
                table: "CourseMaterials",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId_CohortId_CourseLessonId",
                table: "CourseMaterials",
                columns: new[] { "CourseId", "CohortId", "CourseLessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseLessonId",
                table: "CourseMaterials",
                column: "CourseLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_UserId",
                table: "UserNotificationSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseApplicationAnswers");

            migrationBuilder.DropTable(
                name: "CourseMaterials");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings");

            migrationBuilder.DropTable(
                name: "CourseApplicationQuestions");

            migrationBuilder.DropTable(
                name: "CourseApplications");

            migrationBuilder.DropTable(
                name: "CourseLessons");

            migrationBuilder.DropIndex(
                name: "IX_Cohorts_Slug",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "SessionInstances");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "SessionInstances");

            migrationBuilder.DropColumn(
                name: "WeekTitle",
                table: "SessionInstances");

            migrationBuilder.DropColumn(
                name: "AutoAcceptPaidApplications",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "CurrentStudents",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "IsEnrollmentOpen",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "RequireEngineerApproval",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "ZoomJoinUrl",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "ZoomMeetingId",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "ZoomStartUrl",
                table: "Cohorts");
        }
    }
}
