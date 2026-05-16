namespace GenZCoders.Models;

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public int XpReward { get; set; } = 100;
    public DateTimeOffset DueAt { get; set; }
}
