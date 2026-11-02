using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomRentalManagement.Data;
using RoomRentalManagement.Models;

namespace RoomRentalManagement.Controllers
{
    [Authorize(Roles = "Admin,Landlord")]
    public class TenantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tenants
        public async Task<IActionResult> Index()
        {
            var query = _context.Tenants.AsNoTracking();

            // Chủ trọ chỉ xem người thuê thuộc quyền quản lý của mình
            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            // Admin vẫn xem được toàn bộ người thuê
            var tenants = await query
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return View(tenants);
        }

        // Admin xem danh sách hồ sơ người thuê chờ duyệt
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            var tenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.Status == "Pending")
                .Include(t => t.Landlord)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return View(tenants);
        }

        // Admin duyệt hồ sơ người thuê
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfile(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null)
                return NotFound();

            if (tenant.Status != "Pending")
                return BadRequest();

            tenant.Status = "Approved";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt hồ sơ người thuê.";

            return RedirectToAction(nameof(Pending));
        }

        // Admin từ chối hồ sơ người thuê
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProfile(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null)
                return NotFound();

            if (tenant.Status != "Pending")
                return BadRequest();

            tenant.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã từ chối hồ sơ người thuê.";

            return RedirectToAction(nameof(Pending));
        }
        // Admin mở trang cấp tài khoản cho người thuê
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> CreateAccount(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null)
                return NotFound();

            if (tenant.Status != "Approved" || tenant.UserID != null)
                return BadRequest("Hồ sơ chưa được duyệt hoặc đã có tài khoản.");

            return View(tenant);
        }

        // Admin tạo tài khoản cho người thuê đã được duyệt
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(
            int id,
            string username,
            string password)
        {
            username = username?.Trim() ?? "";

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null)
                return NotFound();

            if (tenant.Status != "Approved" || tenant.UserID != null)
                return BadRequest("Hồ sơ chưa được duyệt hoặc đã có tài khoản.");

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                password.Length < 10)
            {
                ModelState.AddModelError("",
                    "Tên đăng nhập không được để trống và mật khẩu phải có ít nhất 10 ký tự.");

                return View(tenant);
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError(nameof(username),
                    "Tên đăng nhập đã tồn tại.");

                return View(tenant);
            }

            var user = new User
            {
                Username = username,
                FullName = tenant.FullName,
                Role = "Tenant",
                Status = "Active"
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, password);

            _context.Users.Add(user);

            tenant.User = user;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cấp tài khoản cho người thuê thành công.";

            return RedirectToAction(nameof(Details), new { id = tenant.TenantID });
        }
        // GET: Tenants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Tenants.AsNoTracking();

            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            var tenant = await query
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null) return NotFound();

            return View(tenant);
        }
        // GET: Tenants/Create
        public IActionResult Create() => View();

        // POST: Tenants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("FullName,Phone,CCCD,DateOfBirth,Address")] Tenant tenant)
        {
            ValidateTenant(tenant);

            if (!string.IsNullOrWhiteSpace(tenant.CCCD) &&
                await _context.Tenants.AnyAsync(t => t.CCCD == tenant.CCCD.Trim()))
            {
                ModelState.AddModelError(nameof(tenant.CCCD), "Số CCCD này đã tồn tại.");
            }

            if (!ModelState.IsValid) return View(tenant);

            tenant.FullName = tenant.FullName.Trim();
            tenant.Phone = tenant.Phone.Trim();
            tenant.CCCD = tenant.CCCD.Trim();
            tenant.Address = string.IsNullOrWhiteSpace(tenant.Address)
                ? null : tenant.Address.Trim();

            // Gắn chủ trọ đang đăng nhập và đặt hồ sơ chờ Admin duyệt
            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                tenant.LandlordID = landlordId;
            }
            else if (User.IsInRole("Admin"))
            {
                tenant.LandlordID = null;
            }

            tenant.UserID = null;
            tenant.Status = "Pending";

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm người thuê thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Tenants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Tenants.AsQueryable();

            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            var tenant = await query.FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null) return NotFound();

            return View(tenant);
        }

        // POST: Tenants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("TenantID,FullName,Phone,CCCD,DateOfBirth,Address")] Tenant input)
        {
            if (id != input.TenantID) return NotFound();

            ValidateTenant(input);

            if (!string.IsNullOrWhiteSpace(input.CCCD) &&
                await _context.Tenants.AnyAsync(t => t.CCCD == input.CCCD.Trim() && t.TenantID != id))
            {
                ModelState.AddModelError(nameof(input.CCCD), "Số CCCD này đã được người thuê khác sử dụng.");
            }

            if (!ModelState.IsValid) return View(input);

            var query = _context.Tenants.AsQueryable();

            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            var tenant = await query.FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null) return NotFound();

            // Chỉ cập nhật các trường người dùng được phép sửa; giữ nguyên UserID.
            tenant.FullName = input.FullName.Trim();
            tenant.Phone = input.Phone.Trim();
            tenant.CCCD = input.CCCD.Trim();
            tenant.DateOfBirth = input.DateOfBirth;
            tenant.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin người thuê.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Tenants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Tenants.AsNoTracking();

            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            var tenant = await query
                .FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null) return NotFound();

            ViewBag.CanDelete = !await HasRelatedRecords(id.Value);

            return View(tenant);
        }

        // POST: Tenants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var query = _context.Tenants.AsQueryable();

            if (User.IsInRole("Landlord"))
            {
                var userIdValue = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (!int.TryParse(userIdValue, out int landlordId))
                    return Forbid();

                query = query.Where(t => t.LandlordID == landlordId);
            }

            var tenant = await query.FirstOrDefaultAsync(t => t.TenantID == id);

            if (tenant == null) return NotFound();

            // Hợp đồng/yêu cầu đang tham chiếu người thuê thì không cho xóa.
            if (await HasRelatedRecords(id))
            {
                TempData["Error"] = "Không thể xóa người thuê này vì đã có hợp đồng hoặc yêu cầu liên quan.";
                return RedirectToAction(nameof(Index));
            }

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa người thuê.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateTenant(Tenant tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.FullName))
                ModelState.AddModelError(nameof(tenant.FullName), "Vui lòng nhập họ tên.");

            if (string.IsNullOrWhiteSpace(tenant.Phone))
                ModelState.AddModelError(nameof(tenant.Phone), "Vui lòng nhập số điện thoại.");

            if (string.IsNullOrWhiteSpace(tenant.CCCD))
                ModelState.AddModelError(nameof(tenant.CCCD), "Vui lòng nhập số CCCD.");

            if (tenant.DateOfBirth.HasValue && tenant.DateOfBirth.Value.Date > DateTime.Today)
                ModelState.AddModelError(nameof(tenant.DateOfBirth), "Ngày sinh không được ở tương lai.");
        }

        private async Task<bool> HasRelatedRecords(int tenantId)
        {
            var hasContract = await _context.Contracts.AnyAsync(c => c.TenantID == tenantId);
            var hasRequest = await _context.Requests.AnyAsync(r => r.TenantID == tenantId);
            return hasContract || hasRequest;
        }
    }
}
