namespace GenZCoders.Models;

public static class AcademyRole
{
    public const string Student = "student";
    public const string Parent = "parent";
    public const string Engineer = "engineer";
    public const string Cta = "cta";
    public const string SchoolAdmin = "school_admin";
    public const string AcademyAdmin = "academy_admin";

    public static readonly string[] All =
    [
        Student,
        Parent,
        Engineer,
        Cta,
        SchoolAdmin,
        AcademyAdmin
    ];
}
