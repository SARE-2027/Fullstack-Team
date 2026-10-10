using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateSessionCartConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "sessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "carts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "carts");

        }
    }
}
