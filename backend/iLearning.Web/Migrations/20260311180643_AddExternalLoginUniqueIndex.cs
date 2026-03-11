using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalLoginUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProvider_ExternalProviderUserId",
                table: "Users",
                columns: new[] { "ExternalProvider", "ExternalProviderUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalProvider_ExternalProviderUserId",
                table: "Users");
        }
    }
}
