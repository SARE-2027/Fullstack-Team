using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateCatalogIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_session_items_zone",
                table: "session_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_zone",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "zone",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "zone",
                table: "detection_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "zone",
                table: "session_items",
                type: "text",
                nullable: false,
                defaultValue: "basket");

            migrationBuilder.AddColumn<string>(
                name: "zone",
                table: "detection_events",
                type: "text",
                nullable: false,
                defaultValue: "basket");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_items_zone",
                table: "session_items",
                sql: "zone IN ('basket', 'tray')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_zone",
                table: "detection_events",
                sql: "zone IN ('basket', 'tray')");
        }
    }
}
