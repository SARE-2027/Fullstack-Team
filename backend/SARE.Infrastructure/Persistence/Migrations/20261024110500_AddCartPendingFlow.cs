using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartPendingFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items");

            migrationBuilder.AddColumn<string>(
                name: "basket_state",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "verified");

            migrationBuilder.AddColumn<int>(
                name: "pending_weight_g",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "confirmed_by_customer",
                table: "cart_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "pending_weight_after_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pending_weight_before_g",
                table: "cart_movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "related_movement_id",
                table: "cart_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_movement_id",
                table: "cart_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unconfirmed_exit_weight_g",
                table: "cart_movements",
                type: "integer",
                nullable: true);

            // بيانات الإصدار القديم المعلقة لا تكفي لتقدير Pending بأمان؛ يراجعها الموظف.
            migrationBuilder.Sql("""
                UPDATE sessions AS s SET basket_state = 'needs_staff'
                WHERE EXISTS (SELECT 1 FROM cart_movements AS m
                    WHERE m.session_id = s.id AND m.status = 'needs_confirmation');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions",
                sql: "basket_state IN ('verified','pending','needs_staff')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions",
                sql: "pending_weight_g >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movements_pending_weight",
                table: "cart_movements",
                sql: "pending_weight_before_g >= 0 AND pending_weight_after_g >= 0 AND (unconfirmed_exit_weight_g IS NULL OR unconfirmed_exit_weight_g >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items",
                sql: "effect IN ('none','added','removed','placement_confirmed','no_change','pending_added','pending_removed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM sessions WHERE pending_weight_g > 0) THEN
                        RAISE EXCEPTION 'Resolve pending cart mass before downgrading.';
                    END IF;
                END $$;
                UPDATE cart_movement_items SET effect = 'no_change'
                WHERE effect IN ('pending_added','pending_removed');
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_basket_state",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_pending_weight",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movements_pending_weight",
                table: "cart_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items");

            migrationBuilder.DropColumn(
                name: "basket_state",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "pending_weight_g",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "confirmed_by_customer",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "pending_weight_after_g",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "pending_weight_before_g",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "related_movement_id",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "resolved_by_movement_id",
                table: "cart_movements");

            migrationBuilder.DropColumn(
                name: "unconfirmed_exit_weight_g",
                table: "cart_movements");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cart_movement_items_effect",
                table: "cart_movement_items",
                sql: "effect IN ('none','added','removed','placement_confirmed','no_change')");
        }
    }
}
