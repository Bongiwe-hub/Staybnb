using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staybnb.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Guests",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Guests",
                table: "Bookings");
        }
    }
}
