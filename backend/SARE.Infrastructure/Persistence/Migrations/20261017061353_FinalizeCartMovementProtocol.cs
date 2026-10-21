using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeCartMovementProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1 FROM cart_movements
                        WHERE status <> 'cancelled'
                          AND (NOT is_complete OR (has_basket_change AND NOT weight_is_stable))
                    ) THEN
                        RAISE EXCEPTION 'Finish or reconcile unfinished prototype reports before upgrading the movement protocol.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_cart_movements_movement_id",
                table: "detection_events");

            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_product_variants_variant_id",
                table: "detection_events");

            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_session_items_session_item_id",
                table: "detection_events");

            migrationBuilder.DropForeignKey(
                name: "fk_detection_events_sessions_session_id",
                table: "detection_events");

            migrationBuilder.DropIndex(
                name: "ix_cart_movements_session_id_first_sequence",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_applied_weight",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_stable_weight",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_status",
                table: "cart_movements");

            migrationBuilder.DropPrimaryKey(
                name: "pk_detection_events",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_action",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_confidence",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_effect",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_outcome",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_snapshot",
                table: "detection_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_detection_events_source",
                table: "detection_events");

            migrationBuilder.DropColumn(
                name: "is_complete",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "weight_is_stable",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "request_hash",
                table: "detection_events");

            // Keep historical per-item weight readings without carrying them in the new runtime model.
            migrationBuilder.RenameColumn(
                name: "weight_delta_g",
                table: "detection_events",
                newName: "archived_item_weight_g");

            migrationBuilder.RenameTable(
                name: "detection_events",
                newName: "cart_movement_items");

            migrationBuilder.RenameIndex(
                name: "ix_detection_events_variant_id",
                table: "cart_movement_items",
                newName: "ix_cart_movement_items_variant_id");

            migrationBuilder.RenameIndex(
                name: "ix_detection_events_session_item_id",
                table: "cart_movement_items",
                newName: "ix_cart_movement_items_session_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_detection_events_session_id_created_at",
                table: "cart_movement_items",
                newName: "ix_cart_movement_items_session_id_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_detection_events_movement_id",
                table: "cart_movement_items",
                newName: "ix_cart_movement_items_movement_id");

            migrationBuilder.AddColumn<Guid>(
                name: "corrects_movement_id",
                table: "cart_movements",
                type: "uuid",
                nullable: true);

            // Standalone historical rows become movement children before enforcing the required FK.
            // Unknown old readings remain cancelled evidence; no prices or weights are fabricated.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE sare_movement_backfill ON COMMIT DROP AS
                SELECT item.id AS item_id, gen_random_uuid() AS movement_id, item.session_id,
                       (session.last_movement_sequence + ROW_NUMBER() OVER (
                           PARTITION BY item.session_id ORDER BY item.created_at, item.id))::integer AS sequence,
                       item.created_at, item.model_version, item.outcome
                FROM cart_movement_items item
                JOIN sessions session ON session.id = item.session_id
                WHERE item.movement_id IS NULL;

                INSERT INTO cart_movements
                    (id, session_id, first_sequence, device_boot_id, started_at_utc, ended_at_utc,
                     weight_tolerance_g, has_basket_change, basket_change_applied, model_version, status, request_hash)
                SELECT movement_id, session_id, sequence, '00000000-0000-0000-0000-000000000000'::uuid,
                       created_at, created_at, 0, false, false, model_version,
                       CASE WHEN outcome IN ('accepted', 'corrected') THEN 'accepted' ELSE 'cancelled' END, ''
                FROM sare_movement_backfill;

                UPDATE cart_movement_items item SET movement_id = backfill.movement_id
                FROM sare_movement_backfill backfill WHERE item.id = backfill.item_id;

                UPDATE cart_movement_items item SET outcome = 'rejected', reason_code = 'MIGRATED_UNRESOLVED_EVENT'
                FROM sare_movement_backfill backfill
                WHERE item.id = backfill.item_id AND item.outcome = 'unknown';

                UPDATE sessions session SET last_movement_sequence = backfill.sequence
                FROM (SELECT session_id, MAX(sequence) AS sequence FROM sare_movement_backfill GROUP BY session_id) backfill
                WHERE session.id = backfill.session_id;

                UPDATE cart_movements SET status = 'needs_confirmation' WHERE status = 'pending';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "movement_id",
                table: "cart_movement_items",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "corrects_item_id",
                table: "cart_movement_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_cart_movement_items",
                table: "cart_movement_items",
                column: "id");

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
                name: "ck_cart_movements_status",
                table: "cart_movements",
                sql: "status IN ('accepted','needs_confirmation','cancelled')");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movement_items_corrects_item_id",
                table: "cart_movement_items",
                column: "corrects_item_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_action",
                table: "cart_movement_items",
                sql: "action IN ('scan','enter','exit','cancel_scan')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_confidence",
                table: "cart_movement_items",
                sql: "confidence IS NULL OR confidence BETWEEN 0 AND 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items",
                sql: "effect IN ('none','added','removed','placement_confirmed','identity_confirmed','no_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_outcome",
                table: "cart_movement_items",
                sql: "outcome IN ('accepted', 'corrected', 'rejected', 'unknown')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_snapshot",
                table: "cart_movement_items",
                sql: "(expected_weight_g IS NULL OR expected_weight_g > 0) AND (unit_price_minor IS NULL OR unit_price_minor >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_source",
                table: "cart_movement_items",
                sql: "source IN ('vision', 'scanner', 'manual')");

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_cart_movement_items_corrects_item_id",
                table: "cart_movement_items",
                column: "corrects_item_id",
                principalTable: "cart_movement_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_cart_movements_movement_id",
                table: "cart_movement_items",
                column: "movement_id",
                principalTable: "cart_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_product_variants_variant_id",
                table: "cart_movement_items",
                column: "variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_session_items_session_item_id",
                table: "cart_movement_items",
                column: "session_item_id",
                principalTable: "session_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_movement_items_sessions_session_id",
                table: "cart_movement_items",
                column: "session_id",
                principalTable: "sessions",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM cart_movements WHERE corrects_movement_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Linked corrections exist; review them before rolling back the movement protocol.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_cart_movement_items_corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_cart_movements_movement_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_product_variants_variant_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_session_items_session_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movement_items_sessions_session_id",
                table: "cart_movement_items");

            migrationBuilder.DropForeignKey(
                name: "fk_cart_movements_cart_movements_corrects_movement_id",
                table: "cart_movements");

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
                name: "ck_cart_movements_status",
                table: "cart_movements");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cart_movement_items",
                table: "cart_movement_items");

            migrationBuilder.DropIndex(
                name: "ix_cart_movement_items_corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_action",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_confidence",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_outcome",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_snapshot",
                table: "cart_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_source",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "corrects_movement_id",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "corrects_item_id",
                table: "cart_movement_items");

            migrationBuilder.RenameTable(
                name: "cart_movement_items",
                newName: "detection_events");

            migrationBuilder.RenameIndex(
                name: "ix_cart_movement_items_variant_id",
                table: "detection_events",
                newName: "ix_detection_events_variant_id");

            migrationBuilder.RenameIndex(
                name: "ix_cart_movement_items_session_item_id",
                table: "detection_events",
                newName: "ix_detection_events_session_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_cart_movement_items_session_id_created_at",
                table: "detection_events",
                newName: "ix_detection_events_session_id_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_cart_movement_items_movement_id",
                table: "detection_events",
                newName: "ix_detection_events_movement_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_complete",
                table: "cart_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "weight_is_stable",
                table: "cart_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "movement_id",
                table: "detection_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "request_hash",
                table: "detection_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "archived_item_weight_g",
                table: "detection_events",
                newName: "weight_delta_g");

            migrationBuilder.Sql("""
                UPDATE cart_movements
                SET is_complete = true,
                    weight_is_stable = weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "pk_detection_events",
                table: "detection_events",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_movements_session_id_first_sequence",
                table: "cart_movements",
                columns: new[] { "session_id", "first_sequence" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_applied_weight",
                table: "cart_movements",
                sql: "NOT basket_change_applied OR weight_is_stable");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_stable_weight",
                table: "cart_movements",
                sql: "NOT weight_is_stable OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_status",
                table: "cart_movements",
                sql: "status IN ('pending','accepted','needs_confirmation','cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_action",
                table: "detection_events",
                sql: "action IN ('scan','enter','exit','cancel_scan')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_confidence",
                table: "detection_events",
                sql: "confidence IS NULL OR confidence BETWEEN 0 AND 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_effect",
                table: "detection_events",
                sql: "effect IN ('none','added','removed','placement_confirmed','identity_confirmed','no_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_outcome",
                table: "detection_events",
                sql: "outcome IN ('accepted', 'corrected', 'rejected', 'unknown')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_snapshot",
                table: "detection_events",
                sql: "(expected_weight_g IS NULL OR expected_weight_g > 0) AND (unit_price_minor IS NULL OR unit_price_minor >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_detection_events_source",
                table: "detection_events",
                sql: "source IN ('vision', 'scanner', 'manual')");

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

            migrationBuilder.AddForeignKey(
                name: "fk_detection_events_session_items_session_item_id",
                table: "detection_events",
                column: "session_item_id",
                principalTable: "session_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_detection_events_sessions_session_id",
                table: "detection_events",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
