using DataBridge.Models.Entities;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.Users;
using DataBridge.Repositories.Interfaces;

namespace DataBridge.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<UserListViewModel> GetListAsync(string? search, string? roleFilter)
        {
            var all = await _userRepo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                all = all.Where(u =>
                    u.Username.ToLower().Contains(search) ||
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(roleFilter) && Enum.TryParse<UserRole>(roleFilter, out var role))
            {
                all = all.Where(u => u.Role == role).ToList();
            }

            return new UserListViewModel
            {
                Users = all.Select(u => new UserRowViewModel
                {
                    Id        = u.Id,
                    Username  = u.Username,
                    FullName  = u.FullName,
                    Email     = u.Email,
                    Role      = u.Role,
                    IsActive  = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                }).ToList(),
                SearchQuery = search,
                RoleFilter  = roleFilter,
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(UserCreateViewModel vm)
        {
            if (await _userRepo.ExistsAsync(vm.Username))
                return (false, $"Username '{vm.Username}' is already taken.");

            var user = new User
            {
                Username     = vm.Username.Trim(),
                FullName     = vm.FullName.Trim(),
                Email        = vm.Email.Trim(),
                Role         = vm.Role,
                IsActive     = vm.IsActive,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                CreatedAt    = DateTime.Now,
            };

            await _userRepo.AddAsync(user);
            return (true, null);
        }

        public async Task<UserEditViewModel?> GetForEditAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return null;

            return new UserEditViewModel
            {
                Id        = user.Id,
                Username  = user.Username,
                FullName  = user.FullName,
                Email     = user.Email,
                Role      = user.Role,
                IsActive  = user.IsActive,
                CreatedAt = user.CreatedAt,
            };
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(UserEditViewModel vm, int currentUserId)
        {
            var user = await _userRepo.GetByIdAsync(vm.Id);
            if (user == null) return (false, "User not found.");

            // Prevent self-deactivation or self-role-downgrade
            if (user.Id == currentUserId)
            {
                if (!vm.IsActive)
                    return (false, "You cannot deactivate your own account.");
                if (vm.Role != UserRole.Admin)
                    return (false, "You cannot change your own role.");
            }

            user.FullName  = vm.FullName.Trim();
            user.Email     = vm.Email.Trim();
            user.Role      = vm.Role;
            user.IsActive  = vm.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);
            return (true, null);
        }

        public async Task<ResetPasswordViewModel?> GetForResetAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return null;

            return new ResetPasswordViewModel
            {
                Id       = user.Id,
                Username = user.Username,
                FullName = user.FullName,
            };
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordViewModel vm)
        {
            var user = await _userRepo.GetByIdAsync(vm.Id);
            if (user == null) return (false, "User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            user.UpdatedAt    = DateTime.Now;

            await _userRepo.UpdateAsync(user);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id, int currentUserId)
        {
            if (id == currentUserId)
                return (false, "You cannot deactivate your own account.");

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");

            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);
            return (true, null);
        }
    }
}
