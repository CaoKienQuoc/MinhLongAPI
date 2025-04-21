using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ManagedByEmployeeId",
                table: "AgencyAccount",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAccount_ManagedByEmployeeId",
                table: "AgencyAccount",
                column: "ManagedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgencyAccount_Employee_ManagedByEmployeeId",
                table: "AgencyAccount",
                column: "ManagedByEmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgencyAccount_Employee_ManagedByEmployeeId",
                table: "AgencyAccount");

            migrationBuilder.DropIndex(
                name: "IX_AgencyAccount_ManagedByEmployeeId",
                table: "AgencyAccount");

            migrationBuilder.DropColumn(
                name: "ManagedByEmployeeId",
                table: "AgencyAccount");
        }
    }
}
