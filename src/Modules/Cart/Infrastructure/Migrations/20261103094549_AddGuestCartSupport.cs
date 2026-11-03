using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestCartSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_shopping_carts_CartId",
                schema: "cart",
                table: "cart_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_shopping_carts",
                schema: "cart",
                table: "shopping_carts");

            migrationBuilder.DropIndex(
                name: "IX_shopping_carts_UserId",
                schema: "cart",
                table: "shopping_carts");

            migrationBuilder.RenameTable(
                name: "shopping_carts",
                schema: "cart",
                newName: "Carts",
                newSchema: "cart");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "cart",
                table: "Carts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "cart",
                table: "Carts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GuestTokenHash",
                schema: "cart",
                table: "Carts",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Carts",
                schema: "cart",
                table: "Carts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_GuestTokenHash",
                schema: "cart",
                table: "Carts",
                column: "GuestTokenHash",
                unique: true,
                filter: "\"GuestTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                schema: "cart",
                table: "Carts",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cart_Identity",
                schema: "cart",
                table: "Carts",
                sql: "(\n    \"UserId\" IS NOT NULL\n    AND \"GuestTokenHash\" IS NULL\n)\nOR\n(\n    \"UserId\" IS NULL\n    AND \"GuestTokenHash\" IS NOT NULL\n)");

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_Carts_CartId",
                schema: "cart",
                table: "cart_items",
                column: "CartId",
                principalSchema: "cart",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_Carts_CartId",
                schema: "cart",
                table: "cart_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Carts",
                schema: "cart",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_GuestTokenHash",
                schema: "cart",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                schema: "cart",
                table: "Carts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cart_Identity",
                schema: "cart",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "cart",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "GuestTokenHash",
                schema: "cart",
                table: "Carts");

            migrationBuilder.RenameTable(
                name: "Carts",
                schema: "cart",
                newName: "shopping_carts",
                newSchema: "cart");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "cart",
                table: "shopping_carts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_shopping_carts",
                schema: "cart",
                table: "shopping_carts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_carts_UserId",
                schema: "cart",
                table: "shopping_carts",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_shopping_carts_CartId",
                schema: "cart",
                table: "cart_items",
                column: "CartId",
                principalSchema: "cart",
                principalTable: "shopping_carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
