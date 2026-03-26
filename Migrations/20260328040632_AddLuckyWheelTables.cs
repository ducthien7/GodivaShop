using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodivaShop.Migrations
{
    /// <inheritdoc />
    public partial class AddLuckyWheelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LuckyPrizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: true),
                    WinChance = table.Column<double>(type: "float", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FillColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyPrizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSpinHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrizeId = table.Column<int>(type: "int", nullable: false),
                    SpinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSpinHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSpinHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSpinHistories_LuckyPrizes_PrizeId",
                        column: x => x.PrizeId,
                        principalTable: "LuckyPrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSpinHistories_PrizeId",
                table: "UserSpinHistories",
                column: "PrizeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSpinHistories_UserId",
                table: "UserSpinHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSpinHistories");

            migrationBuilder.DropTable(
                name: "LuckyPrizes");
        }
    }
}
