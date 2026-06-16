using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at_utc",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at_utc",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "price_lists",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<Guid>(
                name: "series_id",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "valid_from_utc",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "valid_until_utc",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE price_lists AS price_list
                SET
                    series_id = price_list.id,
                    created_by_user_id = (
                        SELECT "user".id
                        FROM users AS "user"
                        WHERE "user".distributor_id = price_list.distributor_id
                        ORDER BY "user".id
                        LIMIT 1
                    ),
                    updated_by_user_id = (
                        SELECT "user".id
                        FROM users AS "user"
                        WHERE "user".distributor_id = price_list.distributor_id
                        ORDER BY "user".id
                        LIMIT 1
                    ),
                    activated_at_utc = CASE
                        WHEN price_list.status = 'Active' THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END,
                    valid_from_utc = CASE
                        WHEN price_list.status = 'Active' THEN '-infinity'::timestamptz
                        ELSE NULL
                    END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "series_id",
                table: "price_lists",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_distributor_id_series_id_version",
                table: "price_lists",
                columns: new[] { "distributor_id", "series_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_effective_version",
                table: "price_lists",
                columns: new[] { "distributor_id", "series_id", "status", "valid_from_utc", "valid_until_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_price_lists_distributor_id_series_id_version",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "ix_price_lists_effective_version",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "activated_at_utc",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "deactivated_at_utc",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "series_id",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "valid_from_utc",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "valid_until_utc",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "version",
                table: "price_lists");
        }
    }
}
