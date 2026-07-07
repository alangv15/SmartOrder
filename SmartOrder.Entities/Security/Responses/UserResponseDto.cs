namespace SmartOrder.Entities.Security.Responses
{
    public class UserResponseDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }
}

