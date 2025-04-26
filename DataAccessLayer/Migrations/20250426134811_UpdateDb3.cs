using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDb3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchCode",
                table: "DamagedStock");

            migrationBuilder.AddColumn<long>(
                name: "BatchId",
                table: "DamagedStock",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStock_BatchId",
                table: "DamagedStock",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_DamagedStock_Batch_BatchId",
                table: "DamagedStock",
                column: "BatchId",
                principalTable: "Batch",
                principalColumn: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DamagedStock_Batch_BatchId",
                table: "DamagedStock");

            migrationBuilder.DropIndex(
                name: "IX_DamagedStock_BatchId",
                table: "DamagedStock");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "DamagedStock");

            migrationBuilder.AddColumn<string>(
                name: "BatchCode",
                table: "DamagedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
