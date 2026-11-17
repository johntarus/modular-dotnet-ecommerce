using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Added_User_Preference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentAtUtc",
                schema: "notifications",
                table: "NotificationLogs",
                newName: "SentAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "notifications",
                table: "NotificationLogs",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "notifications",
                table: "NotificationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerNotificationPreferences",
                schema: "notifications",
                columns: table => new
                {
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    ReceiveMarketingEmails = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveOrderUpdates = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotificationPreferences", x => x.CustomerId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNotificationPreferences",
                schema: "notifications");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "notifications",
                table: "NotificationLogs");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                schema: "notifications",
                table: "NotificationLogs",
                newName: "SentAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "notifications",
                table: "NotificationLogs",
                newName: "CreatedAtUtc");
        }
    }
}
