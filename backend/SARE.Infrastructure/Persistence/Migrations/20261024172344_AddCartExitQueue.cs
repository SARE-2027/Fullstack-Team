using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartExitQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "reconciled_basket_weight_g",
                table: "sessions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions",
                sql: "basket_weight_g IS NULL OR basket_weight_g >= 0");

            migrationBuilder.AddColumn<int>(
                name: "placement_sequence",
                table: "session_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "exit_pending_uncertainty_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "exit_pending_weight_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_items_placement_sequence",
                table: "session_items",
                sql: "placement_sequence >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_exit_pending",
                table: "cart_movements",
                sql: "exit_pending_weight_g >= 0 AND exit_pending_uncertainty_g BETWEEN 0 AND 1000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions");

            migrationBuilder.AddColumn<int>(
                name: "reconciled_basket_weight_g",
                table: "sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions",
                sql: "(basket_weight_g IS NULL OR basket_weight_g >= 0) AND (reconciled_basket_weight_g IS NULL OR reconciled_basket_weight_g >= 0)");

            migrationBuilder.DropCheckConstraint(
                name: "ck_session_items_placement_sequence",
                table: "session_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_exit_pending",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "placement_sequence",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "exit_pending_uncertainty_g",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "exit_pending_weight_g",
                table: "cart_movements");
        }
    }
}
