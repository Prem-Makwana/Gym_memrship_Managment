using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gym_memrship_Managment.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleDailyAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberId_Date",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberId_Date",
                table: "Attendances",
                columns: new[] { "MemberId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberId_Date",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberId_Date",
                table: "Attendances",
                columns: new[] { "MemberId", "Date" },
                unique: true,
                filter: "[CheckOutTime] IS NULL");
        }
    }
}
