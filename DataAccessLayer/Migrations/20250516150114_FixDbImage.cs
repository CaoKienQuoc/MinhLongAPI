using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixDbImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequestDetail_ReturnRequestDetailId",
                table: "ReturnRequestImage");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReturnRequestDetailId",
                table: "ReturnRequestImage",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsProof",
                table: "ReturnRequestImage",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnRequestId",
                table: "ReturnRequestImage",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequestImage_ReturnRequestId",
                table: "ReturnRequestImage",
                column: "ReturnRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequestDetail_ReturnRequestDetailId",
                table: "ReturnRequestImage",
                column: "ReturnRequestDetailId",
                principalTable: "ReturnRequestDetail",
                principalColumn: "ReturnRequestDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequest_ReturnRequestId",
                table: "ReturnRequestImage",
                column: "ReturnRequestId",
                principalTable: "ReturnRequest",
                principalColumn: "ReturnRequestId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequestDetail_ReturnRequestDetailId",
                table: "ReturnRequestImage");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequest_ReturnRequestId",
                table: "ReturnRequestImage");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequestImage_ReturnRequestId",
                table: "ReturnRequestImage");

            migrationBuilder.DropColumn(
                name: "IsProof",
                table: "ReturnRequestImage");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "ReturnRequestImage");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReturnRequestDetailId",
                table: "ReturnRequestImage",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequestImage_ReturnRequestDetail_ReturnRequestDetailId",
                table: "ReturnRequestImage",
                column: "ReturnRequestDetailId",
                principalTable: "ReturnRequestDetail",
                principalColumn: "ReturnRequestDetailId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
