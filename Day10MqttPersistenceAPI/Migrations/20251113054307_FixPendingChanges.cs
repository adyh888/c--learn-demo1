using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Day4DatabaseAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "DeviceMessages",
                newName: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "DeviceMessages",
                newName: "Timestamp");
        }
    }
}
