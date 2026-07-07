namespace SmartOrder.Entities.Security.Models
{
    public class PermissionDto
    {
        public int PermissionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAssigned { get; set; }
    }
}
