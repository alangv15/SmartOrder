namespace SmartOrder.Entities.Security.Models
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
    }
}
