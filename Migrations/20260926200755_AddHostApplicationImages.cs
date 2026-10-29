using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staybnb.Migrations
{
    /// <inheritdoc />
    public partial class AddHostApplicationImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HostApplications_AspNetUsers_ApplicantId",
                table: "HostApplications");

            migrationBuilder.CreateTable(
                name: "HostApplicationImages",
                columns: table => new
                {
                    HostApplicationImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HostApplicationId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostApplicationImages", x => x.HostApplicationImageId);
                    table.ForeignKey(
                        name: "FK_HostApplicationImages_HostApplications_HostApplicationId",
                        column: x => x.HostApplicationId,
                        principalTable: "HostApplications",
                        principalColumn: "HostApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostApplicationImages_HostApplicationId",
                table: "HostApplicationImages",
                column: "HostApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_HostApplications_AspNetUsers_ApplicantId",
                table: "HostApplications",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HostApplications_AspNetUsers_ApplicantId",
                table: "HostApplications");

            migrationBuilder.DropTable(
                name: "HostApplicationImages");

            migrationBuilder.AddForeignKey(
                name: "FK_HostApplications_AspNetUsers_ApplicantId",
                table: "HostApplications",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
