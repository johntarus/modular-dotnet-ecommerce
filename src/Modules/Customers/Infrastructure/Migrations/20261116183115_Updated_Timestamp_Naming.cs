using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Timestamp_Naming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "customers",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "customers",
                table: "Customers",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "customers",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "customers",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "customers",
                table: "Customers",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                schema: "customers",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
