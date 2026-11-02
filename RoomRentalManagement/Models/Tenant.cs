
namespace RoomRentalManagement.Models
{
    public class Tenant
    {
        public int TenantID { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string CCCD { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }

        // Trạng thái duyệt hồ sơ người thuê
        public string Status { get; set; } = "Pending";

        public int? UserID { get; set; }

        public User? User { get; set; }

        // Chủ trọ quản lý hồ sơ người thuê
        public int? LandlordID { get; set; }

        public User? Landlord { get; set; }
    }
}
