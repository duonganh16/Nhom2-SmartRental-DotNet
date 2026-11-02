
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using RoomRentalManagement.Data;
using RoomRentalManagement.Models;

[Authorize(Roles = "Admin,Landlord,Tenant")]
public class RoomsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserID =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Phòng thuộc quyền quản lý của Admin hoặc Landlord
    private IQueryable<Room> AccessibleRooms()
    {
        if (User.IsInRole("Admin"))
            return _context.Rooms;

        if (User.IsInRole("Landlord"))
        {
            return _context.Rooms.Where(r =>
                r.LandlordID == CurrentUserID);
        }

        return _context.Rooms.Where(r => false);
    }

    // Phòng đang thuê của chính tài khoản Tenant
    private IQueryable<Room> TenantRooms()
    {
        return _context.Rooms.Where(r =>
            _context.Contracts.Any(c =>
                c.RoomID == r.RoomID &&
                c.Status == "Active" &&
                c.Tenant != null &&
                c.Tenant.UserID == CurrentUserID));
    }

    // Danh sách phòng
    public async Task<IActionResult> Index()
    {
        IQueryable<Room> rooms;

        if (User.IsInRole("Tenant"))
        {
            rooms = TenantRooms();
        }
        else
        {
            rooms = AccessibleRooms();
        }

        var result = await rooms
            .AsNoTracking()
            .Include(r => r.Building)
            .OrderBy(r => r.Building!.BuildingName)
            .ThenBy(r => r.RoomName)
            .ToListAsync();

        return View(result);
    }

    // Chi tiết phòng
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        IQueryable<Room> rooms = User.IsInRole("Tenant")
            ? TenantRooms()
            : AccessibleRooms();

        var room = await rooms
            .AsNoTracking()
            .Include(r => r.Building)
            .FirstOrDefaultAsync(r => r.RoomID == id);

        if (room == null)
            return NotFound();

        return View(room);
    }

    // GET: Rooms/Create
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> Create()
    {
        var buildings = _context.Buildings.AsNoTracking();

        if (User.IsInRole("Landlord"))
        {
            buildings = buildings.Where(
                b => b.LandlordID == CurrentUserID);
        }

        ViewBag.Buildings = new SelectList(
            await buildings.OrderBy(b => b.BuildingName).ToListAsync(),
            "BuildingID",
            "BuildingName"
        );

        return View();
    }

    // POST: Rooms/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> Create(
        [Bind("BuildingID,RoomName,Price,Area,Status,Description")]
        Room room)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.BuildingID == room.BuildingID);

        if (building == null)
        {
            ModelState.AddModelError(
                "BuildingID",
                "Tòa nhà không tồn tại.");
        }
        else if (User.IsInRole("Landlord") &&
                 building.LandlordID != CurrentUserID)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var buildings = _context.Buildings.AsNoTracking();

            if (User.IsInRole("Landlord"))
            {
                buildings = buildings.Where(
                    b => b.LandlordID == CurrentUserID);
            }

            ViewBag.Buildings = new SelectList(
                await buildings.OrderBy(b => b.BuildingName).ToListAsync(),
                "BuildingID",
                "BuildingName",
                room.BuildingID
            );

            return View(room);
        }

        room.LandlordID = building!.LandlordID;

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã thêm phòng thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Rooms/Edit/5
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await AccessibleRooms()
            .FirstOrDefaultAsync(r => r.RoomID == id);

        if (room == null)
            return NotFound();

        return View(room);
    }

    // POST: Rooms/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("RoomID,RoomName,Price,Area,Status,Description")]
        Room input)
    {
        if (id != input.RoomID)
            return NotFound();

        var room = await AccessibleRooms()
            .FirstOrDefaultAsync(r => r.RoomID == id);

        if (room == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(input);

        room.RoomName = input.RoomName;
        room.Price = input.Price;
        room.Area = input.Area;
        room.Status = input.Status;
        room.Description = input.Description;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật phòng thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Rooms/Delete/5
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await AccessibleRooms()
            .Include(r => r.Building)
            .FirstOrDefaultAsync(r => r.RoomID == id);

        if (room == null)
            return NotFound();

        return View(room);
    }

    // POST: Rooms/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Landlord")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var room = await AccessibleRooms()
            .FirstOrDefaultAsync(r => r.RoomID == id);

        if (room == null)
            return NotFound();

        var hasContracts = await _context.Contracts
            .AnyAsync(c => c.RoomID == id);

        if (hasContracts)
        {
            TempData["Error"] =
                "Không thể xóa phòng vì đã có hợp đồng liên quan.";

            return RedirectToAction(nameof(Index));
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã xóa phòng thành công.";

        return RedirectToAction(nameof(Index));
    }
}
