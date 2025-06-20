using gaming_shop_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace gaming_shop_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserAPIController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserAPIController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Lấy danh sách tất cả user
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userManager.Users
                .Select(u => new {
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.IsBanned,
                    u.EmailConfirmed
                }).ToList();
            return Ok(users);
        }

        // Xem chi tiết một user
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.IsBanned,
                user.EmailConfirmed
            });
        }

        // Khoá tài khoản
        [HttpPost("{id}/ban")]
        public async Task<IActionResult> BanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.IsBanned) return BadRequest("User đã bị khoá.");

            user.IsBanned = true;
            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Khoá tài khoản thành công." });
        }

        // Mở khoá tài khoản
        [HttpPost("{id}/unban")]
        public async Task<IActionResult> UnbanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (!user.IsBanned) return BadRequest("User đang hoạt động.");

            user.IsBanned = false;
            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Mở khoá tài khoản thành công." });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Ok(new { message = "Xóa tài khoản thành công." });

            return BadRequest(new { message = "Xóa tài khoản thất bại.", errors = result.Errors });
        }
        [HttpPost("{id}/set-roles")]

        public async Task<IActionResult> SetRoles(string id, [FromBody] SetRolesDto model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User không tồn tại.");

            // Lấy các role hợp lệ trong hệ thống
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            // Kiểm tra các role truyền lên có hợp lệ không
            var invalidRoles = model.Roles.Except(allRoles, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalidRoles.Any())
                return BadRequest(new { message = $"Role không hợp lệ: {string.Join(", ", invalidRoles)}" });

            // Xóa hết role cũ
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return BadRequest(new { message = "Không thể xóa role cũ.", errors = removeResult.Errors });

            // Thêm role mới
            var addResult = await _userManager.AddToRolesAsync(user, model.Roles);
            if (!addResult.Succeeded)
                return BadRequest(new { message = "Không thể thêm role mới.", errors = addResult.Errors });

            return Ok(new { message = "Cập nhật role thành công." });
        }
    }
    public class SetRolesDto
    {
        public List<string> Roles { get; set; }
    }
}
