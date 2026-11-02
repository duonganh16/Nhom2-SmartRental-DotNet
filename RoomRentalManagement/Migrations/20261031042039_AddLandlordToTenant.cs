using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomRentalManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLandlordToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LandlordID",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_LandlordID",
                table: "Tenants",
                column: "LandlordID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Users_LandlordID",
                table: "Tenants",
                column: "LandlordID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Users_LandlordID",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_LandlordID",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LandlordID",
                table: "Tenants");
        }
    }
}
