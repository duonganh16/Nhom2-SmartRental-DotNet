using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomRentalManagement.Data;
using RoomRentalManagement.Models;
using System.Security.Claims;

namespace RoomRentalManagement.Controllers
{
    [Authorize(Roles = "Admin,Landlord")]
    public class BuildingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuildingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserID =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private IQueryable<Building> AccessibleBuildings()
        {
            if (User.IsInRole("Admin"))
                return _context.Buildings;

            return _context.Buildings
                .Where(b => b.LandlordID == CurrentUserID);
        }

        // Danh sách tòa nhà
        public async Task<IActionResult> Index()
        {
            var buildings = await AccessibleBuildings()
                .Include(b => b.Landlord)
                .Include(b => b.Rooms)
                .OrderBy(b => b.BuildingName)
                .ToListAsync();

            return View(buildings);
        }

        // Chi tiết tòa nhà
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var building = await AccessibleBuildings()
                .Include(b => b.Landlord)
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.BuildingID == id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        // Thêm tòa nhà
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BuildingName,Address,Description")] Building building)
        {
            if (!ModelState.IsValid)
                return View(building);

            if (User.IsInRole("Landlord"))
            {
                building.LandlordID = CurrentUserID;
            }
            else
            {
                ModelState.AddModelError(
                    "LandlordID",
                    "Chức năng thêm tòa nhà dành cho chủ trọ."
                );

                return View(building);
            }

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm tòa nhà thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Sửa tòa nhà
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var building = await AccessibleBuildings()
                .FirstOrDefaultAsync(b => b.BuildingID == id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("BuildingID,BuildingName,Address,Description")] Building input)
        {
            if (id != input.BuildingID)
                return NotFound();

            var building = await AccessibleBuildings()
                .FirstOrDefaultAsync(b => b.BuildingID == id);

            if (building == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(input);

            building.BuildingName = input.BuildingName;
            building.Address = input.Address;
            building.Description = input.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tòa nhà thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Xóa tòa nhà
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var building = await AccessibleBuildings()
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.BuildingID == id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var building = await AccessibleBuildings()
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.BuildingID == id);

            if (building == null)
                return NotFound();

            if (building.Rooms.Any())
            {
                TempData["Error"] =
                    "Không thể xóa tòa nhà đang có phòng. Hãy xử lý các phòng trước.";

                return RedirectToAction(nameof(Index));
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa tòa nhà thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}