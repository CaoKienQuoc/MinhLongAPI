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
            migrationBuilder.AlterColumn<long>(
                name: "AgencyId",
                table: "Contract",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "EmployeeId",
                table: "Contract",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_EmployeeId",
                table: "Contract",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contract_Employee_EmployeeId",
                table: "Contract",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contract_Employee_EmployeeId",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_Contract_EmployeeId",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Contract");

            migrationBuilder.AlterColumn<long>(
                name: "AgencyId",
                table: "Contract",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
