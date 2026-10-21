using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizeCartMovementProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "basket_weight_g",
                table: "sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_movement_sequence",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "last_observed_movement_sequence",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "reconciled_basket_weight_g",
                table: "sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "session_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "basket_camera_id",
                table: "session_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "basket_device_boot_id",
                table: "session_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "basket_presence",
                table: "session_items",
                type: "text",
                nullable: false,
                defaultValue: "unconfirmed");

            migrationBuilder.AddColumn<string>(
                name: "basket_track_id",
                table: "session_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expected_weight_g",
                table: "session_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "removal_requested_at",
                table: "session_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "weight_delta_g",
                table: "detection_events",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "action",
                table: "detection_events",
                type: "text",
                nullable: false,
                defaultValue: "enter");

            migrationBuilder.AddColumn<string>(
                name: "camera_id",
                table: "detection_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "effect",
                table: "detection_events",
                type: "text",
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<int>(
                name: "expected_weight_g",
                table: "detection_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "movement_id",
                table: "detection_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason_code",
                table: "detection_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "related_scan_item_id",
                table: "detection_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_hash",
                table: "detection_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "target_session_item_id",
                table: "detection_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "track_id",
                table: "detection_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unit_price_minor",
                table: "detection_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "detection_events",
                type: "uuid",
                nullable: true);

            // Preserve known historical decisions; current catalog weights are not historical snapshots.
            migrationBuilder.Sql("""
                UPDATE detection_events
                SET action = CASE WHEN source = 'scanner' THEN 'scan' ELSE 'enter' END,
                    effect = CASE WHEN outcome = 'accepted' THEN 'added' ELSE 'none' END;
                """);

            migrationBuilder.CreateTable(
                name: "cart_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_sequence = table.Column<int>(type: "integer", nullable: false),
                    device_boot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    weight_before_g = table.Column<int>(type: "integer", nullable: true),
                    weight_after_g = table.Column<int>(type: "integer", nullable: true),
                    weight_is_stable = table.Column<bool>(type: "boolean", nullable: false),
                    weight_tolerance_g = table.Column<int>(type: "integer", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    has_basket_change = table.Column<bool>(type: "boolean", nullable: false),
                    basket_change_applied = table.Column<bool>(type: "boolean", nullable: false),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_movements", x => x.id);
                    table.CheckConstraint("ck_cart_movements_applied_weight", "NOT basket_change_applied OR weight_is_stable");
                    table.CheckConstraint("ck_cart_movements_sequence", "first_sequence > 0");
                    table.CheckConstraint("ck_cart_movements_stable_weight", "NOT weight_is_stable OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL)");
                    table.CheckConstraint("ck_cart_movements_status", "status IN ('pending','accepted','needs_confirmation','cancelled')");
                    table.CheckConstraint("ck_cart_movements_time", "ended_at_utc >= started_at_utc");
                    table.CheckConstraint("ck_cart_movements_tolerance", "weight_tolerance_g BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_cart_movements_weights", "(weight_before_g IS NULL AND weight_after_g IS NULL) OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL AND weight_before_g >= 0 AND weight_after_g >= 0)");
                    table.ForeignKey(
                        name: "fk_cart_movements_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_event_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    device_boot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_event_receipts", x => x.id);
                    table.CheckConstraint("ck_cart_event_receipts_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_cart_event_receipts_cart_movements_movement_id",
                        column: x => x.movement_id,
                        principalTable: "cart_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cart_event_receipts_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions",
                sql: "(basket_weight_g IS NULL OR basket_weight_g >= 0) AND (reconciled_basket_weight_g IS NULL OR reconciled_basket_weight_g >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions",
                sql: "last_movement_sequence >= 0 AND last_observed_movement_sequence >= 0 AND last_observed_movement_sequence <= last_movement_sequence");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_items_presence",
                table: "session_items",
                sql: "basket_presence IN ('unconfirmed','inside','outside')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_items_weight",
                table: "session_items",
                sql: "expected_weight_g IS NULL OR expected_weight_g > 0");

            migrationBuilder.CreateIndex(
                name: "ix_detection_events_movement_id",
                table: "detection_events",
                column: "movement_id");

            migrationBuilder.CreateIndex(
                name: "ix_detection_events_variant_id",
                table: "detection_events",
                column: "variant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_action",
                table: "detection_events",
                sql: "action IN ('scan','enter','exit','cancel_scan')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_effect",
                table: "detection_events",
                sql: "effect IN ('none','added','removed','placement_confirmed','identity_confirmed','no_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_snapshot",
                table: "detection_events",
                sql: "(expected_weight_g IS NULL OR expected_weight_g > 0) AND (unit_price_minor IS NULL OR unit_price_minor >= 0)");

            migrationBuilder.CreateIndex(
                name: "ix_cart_event_receipts_movement_id",
                table: "cart_event_receipts",
                column: "movement_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_event_receipts_session_id_sequence",
                table: "cart_event_receipts",
                columns: new[] { "session_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cart_movements_session_id_first_sequence",
                table: "cart_movements",
                columns: new[] { "session_id", "first_sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_detection_events_cart_movements_movement_id",
                table: "detection_events",
                column: "movement_id",
                principalTable: "cart_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_detection_events_product_variants_variant_id",
                table: "detection_events",
                column: "variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_cart_movements_movement_id",
                table: "detection_events");

            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_product_variants_variant_id",
                table: "detection_events");

            migrationBuilder.DropTable(
                name: "cart_event_receipts");

            migrationBuilder.DropTable(
                name: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_weight",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_movement_sequence",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_session_items_presence",
                table: "session_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_session_items_weight",
                table: "session_items");

            migrationBuilder.DropIndex(
                name: "ix_detection_events_movement_id",
                table: "detection_events");

            migrationBuilder.DropIndex(
                name: "ix_detection_events_variant_id",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_action",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_effect",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_snapshot",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "basket_weight_g",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "last_movement_sequence",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "last_observed_movement_sequence",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "reconciled_basket_weight_g",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "basket_camera_id",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "basket_device_boot_id",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "basket_presence",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "basket_track_id",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "expected_weight_g",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "removal_requested_at",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "action",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "camera_id",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "effect",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "expected_weight_g",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "movement_id",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "reason_code",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "related_scan_item_id",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "request_hash",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "target_session_item_id",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "track_id",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "unit_price_minor",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "detection_events");

            migrationBuilder.AlterColumn<int>(
                name: "weight_delta_g",
                table: "detection_events",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
