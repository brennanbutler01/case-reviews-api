using System.ComponentModel.DataAnnotations.Schema;

namespace case_reviews_server.Data;

[Table("MagiEligibles")]
public class MagiEligibles
{
    public Guid Id { get; set; }
    public int AdultsEligibleActual { get; set; }
    public int AdultsEligibleCoded { get; set; }
    public int AdultsNotEligibleActual { get; set; }
    public int AdultsNotEligibleCoded { get; set; }
    public int ChildrenEligibleActual { get; set; }
    public int ChildrenEligibleCoded { get; set; }
    public int ChildrenNotEligibleActual { get; set; }
    public int ChildrenNotEligibleCoded { get; set; }
    public Guid? ReviewId { get; set; }
}