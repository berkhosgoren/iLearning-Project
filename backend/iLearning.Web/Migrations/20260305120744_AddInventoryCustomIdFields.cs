using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCustomIdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemCustomIdDigits",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "ItemCustomIdNextNumber",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ItemCustomIdPrefix",
                table: "Inventories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemCustomIdDigits",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ItemCustomIdNextNumber",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ItemCustomIdPrefix",
                table: "Inventories");
        }
    }
}
