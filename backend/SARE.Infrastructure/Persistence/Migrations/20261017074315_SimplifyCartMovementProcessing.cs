using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCartMovementProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_cart_movement_items_corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movements_cart_movements_corrects_movement_id",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "ix_cart_movements_corrects_movement_id",
                table: "cart_movements");

            migrationBuilder.DropIndex(
                name: "ix_cart_movements_session_id_first_sequence",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_applied_weight",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_sequence",
                table: "cart_movements");

            migrationBuilder.DropIndex(
                name: "ix_cart_movement_items_corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropIndex(
                name: "ix_cart_movement_items_movement_id",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "last_observed_movement_sequence",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "basket_change_applied",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "corrects_movement_id",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "request_hash",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "final_barcode",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "model_version",
                table: "cart_movement_items");

            migrationBuilder.RenameColumn(
                name: "first_sequence",
                table: "cart_movements",
                newName: "sequence");

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "cart_movement_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "scan_index",
                table: "cart_movement_items",
                type: "integer",
                nullable: true);

            // Backfill stable positions before adding their unique index, and preserve existing scan links.
            migrationBuilder.Sql("""
                WITH positions AS (
                    SELECT id, (ROW_NUMBER() OVER (PARTITION BY movement_id ORDER BY created_at, id) - 1)::integer AS position
                    FROM cart_movement_items
                )
                UPDATE cart_movement_items item SET position = positions.position
                FROM positions WHERE item.id = positions.id;

                UPDATE cart_movement_items item
                SET scan_index = scan.position, target_session_item_id = NULL
                FROM cart_movement_items scan
                WHERE item.related_scan_item_id = scan.id AND item.movement_id = scan.movement_id;

                UPDATE cart_movement_items item SET target_session_item_id = scan.session_item_id
                FROM cart_movement_items scan
                WHERE item.related_scan_item_id = scan.id AND item.movement_id <> scan.movement_id;

                UPDATE cart_movements movement SET sequence = receipt.sequence
                FROM (SELECT movement_id, MIN(sequence) AS sequence FROM cart_event_receipts GROUP BY movement_id) receipt
                WHERE movement.id = receipt.movement_id;
                """);

            migrationBuilder.DropColumn(
                name: "related_scan_item_id",
                table: "cart_movement_items");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions",
                sql: "last_movement_sequence >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movements_session_id_sequence",
                table: "cart_movements",
                columns: new[] { "session_id", "sequence" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_sequence",
                table: "cart_movements",
                sql: "sequence > 0");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movement_items_movement_id_position",
                table: "cart_movement_items",
                columns: new[] { "movement_id", "position" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items",
                sql: "position >= 0 AND (scan_index IS NULL OR scan_index >= 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "ix_cart_movements_session_id_sequence",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_sequence",
                table: "cart_movements");

            migrationBuilder.DropIndex(
                name: "ix_cart_movement_items_movement_id_position",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_position",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "position",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "scan_index",
                table: "cart_movement_items");

            migrationBuilder.RenameColumn(
                name: "sequence",
                table: "cart_movements",
                newName: "first_sequence");

            migrationBuilder.AddColumn<int>(
                name: "last_observed_movement_sequence",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "basket_change_applied",
                table: "cart_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "corrects_movement_id",
                table: "cart_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_hash",
                table: "cart_movements",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "corrects_item_id",
                table: "cart_movement_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_barcode",
                table: "cart_movement_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_version",
                table: "cart_movement_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "related_scan_item_id",
                table: "cart_movement_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions",
                sql: "last_movement_sequence >= 0 AND last_observed_movement_sequence >= 0 AND last_observed_movement_sequence <= last_movement_sequence");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movements_corrects_movement_id",
                table: "cart_movements",
                column: "corrects_movement_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cart_movements_session_id_first_sequence",
                table: "cart_movements",
                columns: new[] { "session_id", "first_sequence" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_applied_weight",
                table: "cart_movements",
                sql: "NOT basket_change_applied OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_sequence",
                table: "cart_movements",
                sql: "first_sequence > 0");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movement_items_corrects_item_id",
                table: "cart_movement_items",
                column: "corrects_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cart_movement_items_movement_id",
                table: "cart_movement_items",
                column: "movement_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_cart_movement_items_corrects_item_id",
                table: "cart_movement_items",
                column: "corrects_item_id",
                principalTable: "cart_movement_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movements_cart_movements_corrects_movement_id",
                table: "cart_movements",
                column: "corrects_movement_id",
                principalTable: "cart_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
