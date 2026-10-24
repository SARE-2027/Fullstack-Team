using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartWeightToleranceAndStaffAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_tolerance",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "weight_tolerance_g",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_audit_completed_at_utc",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pending_weight_uncertainty_g",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "staff_request_reason",
                table: "sessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "staff_requested_at",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            // الطلبات القديمة تظهر في قائمة المساعدة عند تحميل الداشبورد بعد التحديث.
            migrationBuilder.Sql("""
                UPDATE sessions SET staff_requested_at = last_activity_at,
                    staff_request_reason = 'MIGRATED_STAFF_REVIEW'
                WHERE basket_state = 'needs_staff' AND staff_requested_at IS NULL;
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_tolerance_percent",
                table: "session_items",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_tolerance_percent",
                table: "products",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "audit_id",
                table: "cart_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exit_weight_uncertainty_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_tolerance_percent",
                table: "cart_movement_items",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "cart_basket_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_stable_weight_g = table.Column<int>(type: "integer", nullable: true),
                    last_weight_sequence = table.Column<int>(type: "integer", nullable: true),
                    last_weight_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_scan_sequence = table.Column<int>(type: "integer", nullable: false),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_basket_audits", x => x.id);
                    table.CheckConstraint("ck_cart_basket_audits_completed", "(completed_at_utc IS NULL AND completed_by_user_id IS NULL) OR (completed_at_utc IS NOT NULL AND completed_at_utc >= started_at_utc AND completed_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_cart_basket_audits_weight", "last_stable_weight_g IS NULL OR last_stable_weight_g BETWEEN 0 AND 1000000");
                    table.ForeignKey(
                        name: "fk_cart_basket_audits_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_basket_audit_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_session_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unit_price_minor = table.Column<int>(type: "integer", nullable: false),
                    expected_weight_g = table.Column<int>(type: "integer", nullable: false),
                    weight_tolerance_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    scanned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_basket_audit_items", x => x.id);
                    table.CheckConstraint("ck_cart_basket_audit_items_price", "unit_price_minor >= 0");
                    table.CheckConstraint("ck_cart_basket_audit_items_weight", "expected_weight_g > 0");
                    table.CheckConstraint("ck_cart_basket_audit_items_weight_tolerance_percent", "weight_tolerance_percent BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "fk_cart_basket_audit_items_cart_basket_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "cart_basket_audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cart_basket_audit_items_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cart_basket_audit_items_session_items_original_session_item",
                        column: x => x.original_session_item_id,
                        principalTable: "session_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions",
                sql: "basket_state IN ('verified','pending','needs_staff','auditing')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions",
                sql: "pending_weight_g >= 0 AND pending_weight_uncertainty_g BETWEEN 0 AND 1000000");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_items_weight_tolerance_percent",
                table: "session_items",
                sql: "weight_tolerance_percent BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_weight_tolerance_percent",
                table: "products",
                sql: "weight_tolerance_percent IS NULL OR weight_tolerance_percent BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_exit_uncertainty",
                table: "cart_movements",
                sql: "exit_weight_uncertainty_g >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_weight_tolerance_percent",
                table: "cart_movement_items",
                sql: "weight_tolerance_percent BETWEEN 0 AND 100");

            migrationBuilder.CreateIndex(
                name: "ix_cart_basket_audit_items_audit_id",
                table: "cart_basket_audit_items",
                column: "audit_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_basket_audit_items_original_session_item_id",
                table: "cart_basket_audit_items",
                column: "original_session_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_basket_audit_items_variant_id",
                table: "cart_basket_audit_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_cart_basket_audits_session_id",
                table: "cart_basket_audits",
                column: "session_id",
                unique: true,
                filter: "completed_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM cart_basket_audits WHERE completed_at_utc IS NULL) THEN
                        RAISE EXCEPTION 'Finish active basket audits before rolling back this migration.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.AddColumn<int>(
                name: "weight_tolerance_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_tolerance",
                table: "cart_movements",
                sql: "weight_tolerance_g BETWEEN 0 AND 100");

            migrationBuilder.DropTable(
                name: "cart_basket_audit_items");

            migrationBuilder.DropTable(
                name: "cart_basket_audits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_session_items_weight_tolerance_percent",
                table: "session_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_weight_tolerance_percent",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_exit_uncertainty",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_weight_tolerance_percent",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "last_audit_completed_at_utc",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "pending_weight_uncertainty_g",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "staff_request_reason",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "staff_requested_at",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "weight_tolerance_percent",
                table: "session_items");

            migrationBuilder.DropColumn(
                name: "weight_tolerance_percent",
                table: "products");

            migrationBuilder.DropColumn(
                name: "audit_id",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "exit_weight_uncertainty_g",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "weight_tolerance_percent",
                table: "cart_movement_items");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions",
                sql: "basket_state IN ('verified','pending','needs_staff')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions",
                sql: "pending_weight_g >= 0");
        }
    }
}
