using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at_utc",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deactivated_by_user_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deactivation_reason",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_condition",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "FinalConsumer");

            migrationBuilder.AddColumn<string>(
                name: "zone",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_distributor_status_tax_zone",
                table: "customers",
                columns: new[] { "distributor_id", "status", "tax_condition", "zone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_distributor_status_tax_zone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deactivated_at_utc",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deactivated_by_user_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deactivation_reason",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "tax_condition",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "zone",
                table: "customers");
        }
    }
}
