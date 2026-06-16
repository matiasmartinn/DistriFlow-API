using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLinePriceTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "order_lines",
                newName: "applied_unit_price");

            migrationBuilder.AddColumn<decimal>(
                name: "source_unit_price",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "price_list_version",
                table: "order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_origin",
                table: "order_lines",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "LastValidPrice");

            migrationBuilder.Sql(
                """
                UPDATE order_lines
                SET source_unit_price = applied_unit_price,
                    price_origin = 'LastValidPrice';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source_unit_price",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "price_list_version",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "price_origin",
                table: "order_lines");

            migrationBuilder.RenameColumn(
                name: "applied_unit_price",
                table: "order_lines",
                newName: "unit_price");
        }
    }
}
