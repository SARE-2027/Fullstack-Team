using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SARE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    battery_pct = table.Column<short>(type: "smallint", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sw_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carts", x => x.id);
                    table.CheckConstraint("ck_carts_battery_pct", "battery_pct IS NULL OR battery_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_carts_status", "status IN ('active', 'disabled')");
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    nfc_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN ('customer', 'staff', 'admin')");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_ar = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_minor = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    close_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.CheckConstraint("ck_sessions_close_reason", "close_reason IS NULL OR close_reason IN ('staff_closed', 'abandoned')");
                    table.CheckConstraint("ck_sessions_closed_at", "closed_at IS NULL OR closed_at >= started_at");
                    table.CheckConstraint("ck_sessions_status", "status IN ('open', 'closed', 'abandoned')");
                    table.CheckConstraint("ck_sessions_total_minor", "total_minor >= 0");
                    table.ForeignKey(
                        name: "fk_sessions_carts_cart_id",
                        column: x => x.cart_id,
                        principalTable: "carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sessions_users_closed_by",
                        column: x => x.closed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_options_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    price_minor = table.Column<int>(type: "integer", nullable: false),
                    weight_g = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.CheckConstraint("ck_product_variants_barcode", "length(trim(barcode)) > 0");
                    table.CheckConstraint("ck_product_variants_price_minor", "price_minor >= 0");
                    table.CheckConstraint("ck_product_variants_weight_g", "weight_g > 0");
                    table.ForeignKey(
                        name: "fk_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_option_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_option_values_product_options_product_option_id",
                        column: x => x.product_option_id,
                        principalTable: "product_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price_minor = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_items", x => x.id);
                    table.CheckConstraint("ck_session_items_removed_at", "removed_at IS NULL OR removed_at >= added_at");
                    table.CheckConstraint("ck_session_items_source", "source IN ('vision', 'scanner', 'manual')");
                    table.CheckConstraint("ck_session_items_unit_price_minor", "unit_price_minor >= 0");
                    table.ForeignKey(
                        name: "fk_session_items_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_session_items_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shelf_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shelf_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_delta_g = table.Column<int>(type: "integer", nullable: false),
                    is_matched = table.Column<bool>(type: "boolean", nullable: false),
                    matched_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shelf_events", x => x.id);
                    table.CheckConstraint("ck_shelf_events_matched_session", "NOT is_matched OR matched_session_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_shelf_events_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shelf_events_sessions_matched_session_id",
                        column: x => x.matched_session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "variant_option_values",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variant_option_values", x => new { x.variant_id, x.option_value_id });
                    table.ForeignKey(
                        name: "fk_variant_option_values_product_option_values_option_value_id",
                        column: x => x.option_value_id,
                        principalTable: "product_option_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_variant_option_values_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detection_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    detected_barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    confidence = table.Column<float>(type: "real", nullable: true),
                    weight_delta_g = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    final_barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_detection_events", x => x.id);
                    table.CheckConstraint("ck_detection_events_confidence", "confidence IS NULL OR confidence BETWEEN 0 AND 1");
                    table.CheckConstraint("ck_detection_events_outcome", "outcome IN ('accepted', 'corrected', 'rejected', 'unknown')");
                    table.CheckConstraint("ck_detection_events_source", "source IN ('vision', 'scanner', 'manual')");
                    table.ForeignKey(
                        name: "fk_detection_events_session_items_session_item_id",
                        column: x => x.session_item_id,
                        principalTable: "session_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_detection_events_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_detection_events_session_id_created_at",
                table: "detection_events",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_detection_events_session_item_id",
                table: "detection_events",
                column: "session_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_values_product_option_id",
                table: "product_option_values",
                column: "product_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_options_product_id",
                table: "product_options",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_barcode",
                table: "product_variants",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_session_items_session_id",
                table: "session_items",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_session_items_variant_id",
                table: "session_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_cart_id",
                table: "sessions",
                column: "cart_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_closed_by",
                table: "sessions",
                column: "closed_by");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_status",
                table: "sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shelf_events_created_at",
                table: "shelf_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_shelf_events_matched_session_id",
                table: "shelf_events",
                column: "matched_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_shelf_events_variant_id",
                table: "shelf_events",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_nfc_uid",
                table: "users",
                column: "nfc_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variant_option_values_option_value_id",
                table: "variant_option_values",
                column: "option_value_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detection_events");

            migrationBuilder.DropTable(
                name: "shelf_events");

            migrationBuilder.DropTable(
                name: "variant_option_values");

            migrationBuilder.DropTable(
                name: "session_items");

            migrationBuilder.DropTable(
                name: "product_option_values");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "product_options");

            migrationBuilder.DropTable(
                name: "carts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
