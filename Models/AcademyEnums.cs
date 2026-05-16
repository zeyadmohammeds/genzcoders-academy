namespace GenZCoders.Models;

public enum SchoolType { Secondary, Stem, Iats, International, Other }
public enum PartnershipStatus { Prospect, Negotiating, FoundingPartner, Active, Paused, Archived }
public enum SessionType { Core, TechnicalSupport, Workshop, Review, Showcase }
public enum CohortStatus { Upcoming, Active, Completed, Cancelled }
public enum SessionStatus { Scheduled, Live, Completed, Cancelled }
public enum AttendanceStatus { Present, Absent, Late, Excused }
public enum OrderType { Single, Bundle }
public enum PaymentStatus { Pending, Paid, Failed, Refunded, PartiallyRefunded }
public enum EnrollmentStatus { Pending, Active, Completed, Dropped, Refunded }
public enum TaskType { Code, Design, Project, Reflection, Research }
public enum SubmissionType { File, Link, Text, Image, Repository }
public enum SubmissionStatus { Pending, Graded, ReturnedForRevision, Accepted }
public enum QuizType { Formative, MidCourse, FinalExam, Bonus, LiveChallenge }
public enum QuestionType { Mcq, TrueFalse, ShortAnswer, CodeOutput, Match }
public enum AnswerRevealPolicy { Immediately, AfterDeadline, Never }
public enum XpSourceType { Attendance, Task, Quiz, Streak, Referral, Bonus, Badge, Challenge, Project }
public enum DiscountType { Percentage, FixedAmount }
public enum NotificationChannel { InApp, Email, Sms, WhatsApp, Push }
public enum NotificationStatus { Queued, Sent, Failed, Read }
public enum ProjectVisibility { Private, ParentsOnly, SchoolOnly, Public }
public enum LeadStatus { New, Contacted, Qualified, Converted, Lost }
public enum ApplicationStatus { Draft, Submitted, QuestionsPassed, PaymentPending, Paid, UnderReview, Accepted, Rejected, Waitlisted, Cancelled }
public enum ApplicationQuestionType { Mcq, TrueFalse, ShortAnswer }
public enum ApplicationReviewDecision { Pending, Accepted, Rejected, NeedsManualReview }
public enum CourseMaterialType { Pdf, PowerPoint, Video, Link, CodeRepository, Worksheet, Recording }
public enum CourseAccessStatus { Locked, ApplicationRequired, PaymentRequired, PendingApproval, Open }
public enum CourseRoundMode { Online, Hybrid, InPerson }
public enum ZoomRole { Student, Instructor }
public enum VerificationPurpose { EmailVerification, PasswordReset, LoginOtp }
public enum VerificationStatus { Pending, Used, Expired, Revoked }
public enum ExperienceLevel { New, Beginner, Intermediate, Advanced }
public enum CartStatus { Active, Converted, Abandoned }
