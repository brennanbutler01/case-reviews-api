using System.ComponentModel.DataAnnotations.Schema;

namespace case_reviews_server.Data;

[Table("Reviews")]
public class Review
{
    public Guid Id { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
    public Programs Program { get; set; }
    public DateTime ReviewDate { get; set; }
    public bool IsTargeted { get; set; }
    public Guid? StaffId { get; set; }
    public bool IsComplete { get; set; }
    public int CaseNumber { get; set; }
    public string? OtherComments { get; set; }
    public SNAPreportingSystems? ReportingSystem { get; set; }
    public MagiSubPrograms? MagiSubProgram { get; set; }
    public NonMagiSubPrograms? NonMagiSubProgram { get; set; }
    public MagiEligibles? MagiEligibles { get; set; }
    public ICollection<ReviewElement> ReviewElements { get; set; } = new List<ReviewElement>();
}