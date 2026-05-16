namespace GenZCoders.Models;

public class CourseModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProjectOutcome { get; set; } = string.Empty;
}
