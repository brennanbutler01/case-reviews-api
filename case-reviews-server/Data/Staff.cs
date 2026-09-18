namespace case_reviews_server.Data;

public class Staff
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ORNumber { get; set; } = string.Empty;
    public Office Office { get; set; }
    public string? CreatedBy { get; set; }
    public ICollection<Review>? Reviews { get; set; }
}