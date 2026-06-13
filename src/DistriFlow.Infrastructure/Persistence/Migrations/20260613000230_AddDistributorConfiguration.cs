using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributorConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "order_number",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "distributor_configurations",
                columns: table => new
                {
                    distributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_policy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    price_fallback_policy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    order_number_prefix = table.Column<string>(type: "text", nullable: true),
                    next_order_number = table.Column<long>(type: "bigint", nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_distributor_configurations", x => x.distributor_id);
                });

            migrationBuilder.Sql(
                """
                WITH numbered_orders AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY distributor_id
                            ORDER BY started_at_utc, id) AS sequence
                    FROM orders
                )
                UPDATE orders AS orders_to_update
                SET order_number = 'PED-' || LPAD(numbered_orders.sequence::text, 6, '0')
                FROM numbered_orders
                WHERE orders_to_update.id = numbered_orders.id;

                INSERT INTO distributor_configurations (
                    distributor_id,
                    stock_policy,
                    price_fallback_policy,
                    order_number_prefix,
                    next_order_number,
                    time_zone,
                    created_at_utc,
                    updated_at_utc)
                SELECT
                    distributors.id,
                    'RequireAvailableStock',
                    'UseLastValidPrice',
                    'PED',
                    COALESCE(order_counts.total + 1, 1),
                    'UTC',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM distributors
                LEFT JOIN (
                    SELECT distributor_id, COUNT(*) AS total
                    FROM orders
                    GROUP BY distributor_id
                ) AS order_counts
                    ON order_counts.distributor_id = distributors.id;

                INSERT INTO stocks (
                    id,
                    distributor_id,
                    product_id,
                    available,
                    sale_unit)
                SELECT
                    gen_random_uuid(),
                    products.distributor_id,
                    products.id,
                    0,
                    products.sale_unit
                FROM products
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM stocks
                    WHERE stocks.distributor_id = products.distributor_id
                      AND stocks.product_id = products.id);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "order_number",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_distributor_id_order_number",
                table: "orders",
                columns: new[] { "distributor_id", "order_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "distributor_configurations");

            migrationBuilder.DropIndex(
                name: "ix_orders_distributor_id_order_number",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_number",
                table: "orders");
        }
    }
}
