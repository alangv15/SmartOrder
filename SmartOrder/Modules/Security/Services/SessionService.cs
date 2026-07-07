using SmartOrder.Entities.Security.Responses;

namespace SmartOrder.Modules.Security.Services
{
    public class SessionService
    {
        private const string UserIdKey = "user_id";
        private const string UserNameKey = "user_name";
        private const string UserRoleKey = "user_role";
        private const string UserPermissionsKey = "user_permissions";

        public async Task SaveUserSessionAsync(UserResponseDto user)
        {
            await SaveValueAsync(UserIdKey, user.UserID.ToString());
            await SaveValueAsync(UserNameKey, user.FullName);
            await SaveValueAsync(UserRoleKey, user.RoleCode);
            await SaveValueAsync(UserPermissionsKey, string.Join("|", user.Permissions));
        }

        public async Task<bool> IsLoggedInAsync()
        {
            var userId = await GetValueAsync(UserIdKey);
            return !string.IsNullOrEmpty(userId);
        }

        public async Task<string> GetUserRoleAsync()
        {
            return await GetValueAsync(UserRoleKey);
        }

        public async Task<IReadOnlySet<string>> GetPermissionCodesAsync()
        {
            var permissions = await GetValueAsync(UserPermissionsKey);
            return permissions
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> HasPermissionAsync(string permissionCode)
        {
            var permissions = await GetPermissionCodesAsync();
            return permissions.Contains(permissionCode);
        }

        public async Task<string> GetUserIdAsync()
        {
            return await GetValueAsync(UserIdKey);
        }

        public async Task LogoutAsync()
        {
            RemoveValue(UserIdKey);
            RemoveValue(UserNameKey);
            RemoveValue(UserRoleKey);
            RemoveValue(UserPermissionsKey);
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync("//LoginPage", true);
        }

        private static async Task SaveValueAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value);
            }
            catch
            {
                Preferences.Set(key, value);
            }
        }

        private static async Task<string> GetValueAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key) ?? Preferences.Get(key, string.Empty);
            }
            catch
            {
                return Preferences.Get(key, string.Empty);
            }
        }

        private static void RemoveValue(string key)
        {
            try
            {
                SecureStorage.Remove(key);
            }
            catch
            {
            }

            Preferences.Remove(key);
        }
    }
}

