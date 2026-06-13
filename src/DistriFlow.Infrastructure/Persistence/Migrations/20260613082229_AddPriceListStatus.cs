using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistriFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "price_lists",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "price_lists");
        }
    }
}
