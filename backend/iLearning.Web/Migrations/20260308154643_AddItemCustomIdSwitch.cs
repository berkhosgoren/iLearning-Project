using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCustomIdSwitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ItemCustomIdEnabled",
                table: "Inventories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemCustomIdEnabled",
                table: "Inventories");
        }
    }
}
