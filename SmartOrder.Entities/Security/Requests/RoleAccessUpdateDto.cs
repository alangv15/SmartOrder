namespace SmartOrder.Entities.Security.Requests
{
    public class RoleAccessUpdateDto
    {
        public List<int> PermissionIds { get; set; } = new();
    }
}
