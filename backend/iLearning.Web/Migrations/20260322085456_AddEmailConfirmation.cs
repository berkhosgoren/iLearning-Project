using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAtUtc",
                table: "Users");
        }
    }
}
