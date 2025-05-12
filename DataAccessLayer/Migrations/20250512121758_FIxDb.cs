using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FIxDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatRoom_RoomName",
                table: "ChatRoom");

            migrationBuilder.AlterColumn<string>(
                name: "RoomName",
                table: "ChatRoom",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoom_RoomName",
                table: "ChatRoom",
                column: "RoomName",
                unique: true,
                filter: "[RoomName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatRoom_RoomName",
                table: "ChatRoom");

            migrationBuilder.AlterColumn<string>(
                name: "RoomName",
                table: "ChatRoom",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoom_RoomName",
                table: "ChatRoom",
                column: "RoomName",
                unique: true);
        }
    }
}
