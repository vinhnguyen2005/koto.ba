namespace Kotoba.Modules.Domain.Entities
{
    public class ReportCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;        
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
