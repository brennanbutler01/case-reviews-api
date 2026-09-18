using System.ComponentModel.DataAnnotations.Schema;

namespace case_reviews_server.Data;

[Table("ReviewElements")]
public class ReviewElement
{
    public Guid Id { get; set; }
    public ProgramReviewElement ReviewedElement { get; set; }
    public Programs Program { get; set; }
    public bool IsReviewed { get; set; }
    public bool IsError { get; set; }
    public bool HasAction { get; set; }
    public string Comments { get; set; } = string.Empty;
    public Guid? ReviewId { get; set; }
}