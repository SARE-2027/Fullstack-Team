using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSingleOpenSessionPerCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_sessions_open_cart",
                table: "sessions",
                column: "cart_id",
                unique: true,
                filter: "status = 'open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_sessions_open_cart",
                table: "sessions");
        }
    }
}
