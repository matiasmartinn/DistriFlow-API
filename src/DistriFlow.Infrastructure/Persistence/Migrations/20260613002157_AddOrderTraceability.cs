using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "started_at_utc",
                table: "orders",
                newName: "created_at_utc");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "confirmed_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Online");

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.Sql(
                """
                UPDATE orders
                SET created_by_user_id = sales_rep_id,
                    origin = 'Online',
                    row_version = 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "confirmed_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "confirmed_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "orders",
                newName: "started_at_utc");
        }
    }
}
