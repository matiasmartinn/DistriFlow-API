using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    distributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    sale_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stocks_distributor_id",
                table: "stocks",
                column: "distributor_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocks_distributor_id_product_id",
                table: "stocks",
                columns: new[] { "distributor_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocks");
        }
    }
}
