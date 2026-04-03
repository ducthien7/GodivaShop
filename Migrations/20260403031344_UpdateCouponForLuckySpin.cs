using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodivaShop.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCouponForLuckySpin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Coupons");
        }
    }
}
