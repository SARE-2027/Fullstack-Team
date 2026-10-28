using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpCartMovementContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_sessions_session_id",
                table: "cart_movement_items");

            migrationBuilder.DropIndex(
                name: "ix_cart_movement_items_session_id_created_at",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "scan_index",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "session_id",
                table: "cart_movement_items");

            // Fill only missing barcode snapshots before making them required.
            // Financial prices and historical weight snapshots are left untouched.
            migrationBuilder.Sql("""
                UPDATE session_items AS item SET barcode = variant.barcode
                FROM product_variants AS variant
                WHERE item.variant_id = variant.id AND (item.barcode IS NULL OR item.barcode = '');

                UPDATE cart_movement_items SET effect = 'no_change'
                WHERE effect = 'identity_confirmed';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "barcode",
                table: "session_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items",
                sql: "effect IN ('none','added','removed','placement_confirmed','no_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items",
                sql: "position >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items");

            migrationBuilder.AlterColumn<string>(
                name: "barcode",
                table: "session_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<int>(
                name: "scan_index",
                table: "cart_movement_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "session_id",
                table: "cart_movement_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // The parent retains the session identity, so restoring the duplicate is deterministic.
            migrationBuilder.Sql("""
                UPDATE cart_movement_items AS item SET session_id = movement.session_id
                FROM cart_movements AS movement WHERE item.movement_id = movement.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_cart_movement_items_session_id_created_at",
                table: "cart_movement_items",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items",
                sql: "effect IN ('none','added','removed','placement_confirmed','identity_confirmed','no_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items",
                sql: "position >= 0 AND (scan_index IS NULL OR scan_index >= 0)");

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_sessions_session_id",
                table: "cart_movement_items",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
