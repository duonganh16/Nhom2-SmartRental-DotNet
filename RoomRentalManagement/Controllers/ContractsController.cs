
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RoomRentalManagement.Data;
using RoomRentalManagement.Models;

namespace RoomRentalManagement.Controllers
{
    [Authorize(Roles = "Admin,Landlord,Tenant")]
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContractsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserID =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Tòa nhà mà tài khoản hiện tại được phép quản lý
        private IQueryable<Building> AccessibleBuildings()
        {
            var buildings = _context.Buildings.AsQueryable();

            if (User.IsInRole("Admin"))
                return buildings;

            if (User.IsInRole("Landlord"))
                return buildings.Where(b => b.LandlordID == CurrentUserID);

            return buildings.Where(b => false);
        }

        // Phòng mà tài khoản hiện tại được phép quản lý
        private IQueryable<Room> AccessibleRooms()
        {
            var rooms = _context.Rooms.AsQueryable();

            if (User.IsInRole("Admin"))
                return rooms;

            if (User.IsInRole("Landlord"))
                return rooms.Where(r => r.LandlordID == CurrentUserID);

            return rooms.Where(r => false);
        }

        // Chỉ lấy phòng chưa có hợp đồng đang hiệu lực.
        // Khi sửa hợp đồng, vẫn giữ phòng của chính hợp đồng đó.
        private IQueryable<Room> AvailableRooms(
            int buildingId,
            int? currentContractId = null)
        {
            return AccessibleRooms()
                .Where(r => r.BuildingID == buildingId)
                .Where(r => !_context.Contracts.Any(c =>
                    c.RoomID == r.RoomID &&
                    c.Status == "Active" &&
                    (!currentContractId.HasValue ||
                     c.ContractID != currentContractId.Value)));
        }

        // Hợp đồng mà tài khoản hiện tại được phép xem
        private IQueryable<Contract> AccessibleContracts()
        {
            var contracts = _context.Contracts.AsQueryable();

            if (User.IsInRole("Admin"))
                return contracts;

            if (User.IsInRole("Landlord"))
            {
                return contracts.Where(c =>
                    c.Room != null &&
                    c.Room.LandlordID == CurrentUserID);
            }

            if (User.IsInRole("Tenant"))
            {
                return contracts.Where(c =>
                    c.Tenant != null &&
                    c.Tenant.UserID == CurrentUserID);
            }

            return contracts.Where(c => false);
        }

        public async Task<IActionResult> Index()
        {
            var contracts = await AccessibleContracts()
                .AsNoTracking()
                .Include(c => c.Room)
                .Include(c => c.Tenant)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View(contracts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await AccessibleContracts()
                .AsNoTracking()
                .Include(c => c.Room)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.ContractID == id);

            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // GET: Contracts/Create
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View(new Contract
            {
                StartDate = DateTime.Today,
                Status = "Active"
            });
        }

        // Lấy danh sách phòng còn trống theo tòa nhà
        [HttpGet]
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> GetAvailableRooms(int buildingId)
        {
            var buildingExists = await AccessibleBuildings()
                .AnyAsync(b => b.BuildingID == buildingId);

            if (!buildingExists)
                return Forbid();

            var rooms = await AvailableRooms(buildingId)
                .AsNoTracking()
                .OrderBy(r => r.RoomName)
                .Select(r => new
                {
                    roomID = r.RoomID,
                    roomName = r.RoomName
                })
                .ToListAsync();

            return Json(rooms);
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> Create(
            int buildingId,
            [Bind("RoomID,TenantID,StartDate,EndDate,Deposit,RentPrice,Status")]
            Contract contract)
        {
            var buildingExists = await AccessibleBuildings()
                .AnyAsync(b => b.BuildingID == buildingId);

            if (!buildingExists)
            {
                ModelState.AddModelError(
                    "buildingId",
                    "Vui lòng chọn tòa nhà hợp lệ.");
            }

            var room = await AvailableRooms(buildingId)
                .FirstOrDefaultAsync(r => r.RoomID == contract.RoomID);

            if (room == null)
            {
                ModelState.AddModelError(
                    nameof(contract.RoomID),
                    "Phòng không tồn tại, không thuộc tòa nhà đã chọn hoặc đã được thuê.");
            }

            await ValidateContractAsync(contract, null);

            if (ModelState.IsValid && room != null)
            {
                contract.Status = NormalizeStatus(contract.Status);

                if (IsActive(contract.Status))
                    room.Status = "đang thuê";

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã tạo hợp đồng thành công.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(
                buildingId,
                contract.RoomID,
                contract.TenantID);

            return View(contract);
        }

        // GET: Contracts/Edit/5
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await AccessibleContracts()
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.ContractID == id);

            if (contract == null)
                return NotFound();

            int? buildingId = contract.Room?.BuildingID;

            await LoadDropdownsAsync(
                buildingId,
                contract.RoomID,
                contract.TenantID,
                contract.ContractID);

            ViewBag.SelectedBuildingID = buildingId;

            return View(contract);
        }

        // POST: Contracts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> Edit(
            int id,
            int buildingId,
            [Bind("ContractID,RoomID,TenantID,StartDate,EndDate,Deposit,RentPrice,Status")]
            Contract input)
        {
            if (id != input.ContractID)
                return NotFound();

            var contract = await AccessibleContracts()
                .FirstOrDefaultAsync(c => c.ContractID == id);

            if (contract == null)
                return NotFound();

            var buildingExists = await AccessibleBuildings()
                .AnyAsync(b => b.BuildingID == buildingId);

            if (!buildingExists)
            {
                ModelState.AddModelError(
                    "buildingId",
                    "Vui lòng chọn tòa nhà hợp lệ.");
            }

            var newRoom = await AvailableRooms(buildingId, id)
                .FirstOrDefaultAsync(r => r.RoomID == input.RoomID);

            if (newRoom == null)
            {
                ModelState.AddModelError(
                    nameof(input.RoomID),
                    "Phòng không hợp lệ, không thuộc tòa nhà đã chọn hoặc đã được thuê.");
            }

            await ValidateContractAsync(input, id);

            if (!ModelState.IsValid || newRoom == null)
            {
                await LoadDropdownsAsync(
                    buildingId,
                    input.RoomID,
                    input.TenantID,
                    id);

                ViewBag.SelectedBuildingID = buildingId;

                return View(input);
            }

            var oldRoomId = contract.RoomID;

            contract.RoomID = input.RoomID;
            contract.TenantID = input.TenantID;
            contract.StartDate = input.StartDate;
            contract.EndDate = input.EndDate;
            contract.Deposit = input.Deposit;
            contract.RentPrice = input.RentPrice;
            contract.Status = NormalizeStatus(input.Status);

            // Nếu chuyển phòng hoặc hợp đồng không còn hiệu lực,
            // cập nhật lại trạng thái phòng cũ.
            if (oldRoomId != contract.RoomID || !IsActive(contract.Status))
            {
                var otherActive = await _context.Contracts.AnyAsync(c =>
                    c.ContractID != id &&
                    c.RoomID == oldRoomId &&
                    c.Status == "Active");

                if (!otherActive)
                {
                    var oldRoom = await _context.Rooms.FindAsync(oldRoomId);

                    if (oldRoom != null)
                        oldRoom.Status = "trống";
                }
            }

            if (IsActive(contract.Status))
                newRoom.Status = "đang thuê";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/Delete/5
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await AccessibleContracts()
                .AsNoTracking()
                .Include(c => c.Room)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.ContractID == id);

            if (contract == null)
                return NotFound();

            ViewBag.CanDelete = !await HasInvoicesAsync(id.Value);

            return View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Landlord")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await AccessibleContracts()
                .FirstOrDefaultAsync(c => c.ContractID == id);

            if (contract == null)
                return NotFound();

            if (await HasInvoicesAsync(id))
            {
                TempData["Error"] =
                    "Không thể xóa hợp đồng này vì đã có hóa đơn liên quan.";

                return RedirectToAction(nameof(Index));
            }

            var roomId = contract.RoomID;
            var wasActive = IsActive(contract.Status);

            _context.Contracts.Remove(contract);

            if (wasActive)
            {
                var otherActive = await _context.Contracts.AnyAsync(c =>
                    c.ContractID != id &&
                    c.RoomID == roomId &&
                    c.Status == "Active");

                if (!otherActive)
                {
                    var room = await _context.Rooms.FindAsync(roomId);

                    if (room != null)
                        room.Status = "trống";
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra dữ liệu hợp đồng
        private async Task ValidateContractAsync(
            Contract contract,
            int? currentContractId)
        {
            if (contract.RoomID <= 0 ||
                !await AccessibleRooms()
                    .AnyAsync(r => r.RoomID == contract.RoomID))
            {
                ModelState.AddModelError(
                    nameof(contract.RoomID),
                    "Vui lòng chọn phòng hợp lệ thuộc quyền quản lý.");
            }

            var tenantIsApproved = await _context.Tenants.AnyAsync(t =>
                t.TenantID == contract.TenantID &&
                t.Status == "Approved");

            if (contract.TenantID <= 0 || !tenantIsApproved)
            {
                ModelState.AddModelError(
                    nameof(contract.TenantID),
                    "Vui lòng chọn người thuê có hồ sơ đã được duyệt.");
            }

            if (contract.StartDate == default)
            {
                ModelState.AddModelError(
                    nameof(contract.StartDate),
                    "Vui lòng nhập ngày bắt đầu.");
            }

            if (contract.EndDate.HasValue &&
                contract.StartDate != default &&
                contract.EndDate.Value.Date < contract.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(contract.EndDate),
                    "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
            }

            if (contract.Deposit < 0)
            {
                ModelState.AddModelError(
                    nameof(contract.Deposit),
                    "Tiền đặt cọc không được âm.");
            }

            if (contract.RentPrice <= 0)
            {
                ModelState.AddModelError(
                    nameof(contract.RentPrice),
                    "Giá thuê phải lớn hơn 0.");
            }

            var allowedStatuses = new[]
            {
                "Active",
                "Expired",
                "Terminated"
            };

            if (string.IsNullOrWhiteSpace(contract.Status) ||
                !allowedStatuses.Contains(
                    contract.Status.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(contract.Status),
                    "Vui lòng chọn trạng thái hợp đồng hợp lệ.");
            }

            if (IsActive(contract.Status) && contract.RoomID > 0)
            {
                var roomHasOtherActive = await _context.Contracts.AnyAsync(c =>
                    c.RoomID == contract.RoomID &&
                    c.Status == "Active" &&
                    (!currentContractId.HasValue ||
                     c.ContractID != currentContractId.Value));

                if (roomHasOtherActive)
                {
                    ModelState.AddModelError(
                        nameof(contract.RoomID),
                        "Phòng này đã có hợp đồng đang hiệu lực.");
                }
            }
        }

        // Nạp dữ liệu cho các danh sách chọn
        private async Task LoadDropdownsAsync(
            int? selectedBuildingId = null,
            int? selectedRoomId = null,
            int? selectedTenantId = null,
            int? currentContractId = null)
        {
            var buildings = await AccessibleBuildings()
                .AsNoTracking()
                .OrderBy(b => b.BuildingName)
                .ToListAsync();

            ViewBag.BuildingID = new SelectList(
                buildings,
                "BuildingID",
                "BuildingName",
                selectedBuildingId);

            var rooms = new List<Room>();

            if (selectedBuildingId.HasValue)
            {
                rooms = await AvailableRooms(
                        selectedBuildingId.Value,
                        currentContractId)
                    .AsNoTracking()
                    .OrderBy(r => r.RoomName)
                    .ToListAsync();
            }

            ViewBag.RoomID = new SelectList(
                rooms,
                "RoomID",
                "RoomName",
                selectedRoomId);

            var tenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.Status == "Approved")
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.TenantID = new SelectList(
                tenants,
                "TenantID",
                "FullName",
                selectedTenantId);
        }

        private Task<bool> HasInvoicesAsync(int contractId)
        {
            return _context.Invoices.AnyAsync(
                i => i.ContractID == contractId);
        }

        private static bool IsActive(string? status)
        {
            return string.Equals(
                status,
                "Active",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? "Active"
                : status.Trim();
        }
    }
}
