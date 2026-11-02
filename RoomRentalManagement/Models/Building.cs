namespace RoomRentalManagement.Models
{
    public class Building
    {
        public int BuildingID { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Chủ nhà sở hữu tòa nhà
        public int LandlordID { get; set; }

        public User? Landlord { get; set; }

        // Danh sách phòng thuộc tòa nhà
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}