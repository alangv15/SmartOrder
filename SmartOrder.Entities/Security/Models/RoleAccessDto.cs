namespace SmartOrder.Entities.Security.Models
{
    public class RoleAccessDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
