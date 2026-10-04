using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gym_memrship_Managment.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePTBookingQR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCodeHash",
                table: "MemberProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TrainerSlots",
                columns: table => new
                {
                    TrainerSlotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerSlots", x => x.TrainerSlotId);
                    table.ForeignKey(
                        name: "FK_TrainerSlots_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PTBookings",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    TrainerSlotId = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTBookings", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_PTBookings_MemberProfiles_MemberId",
                        column: x => x.MemberId,
                        principalTable: "MemberProfiles",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PTBookings_TrainerSlots_TrainerSlotId",
                        column: x => x.TrainerSlotId,
                        principalTable: "TrainerSlots",
                        principalColumn: "TrainerSlotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PTBookings_MemberId",
                table: "PTBookings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PTBookings_TrainerSlotId",
                table: "PTBookings",
                column: "TrainerSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerSlots_TrainerId",
                table: "TrainerSlots",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PTBookings");

            migrationBuilder.DropTable(
                name: "TrainerSlots");

            migrationBuilder.DropColumn(
                name: "QrCodeHash",
                table: "MemberProfiles");
        }
    }
}
