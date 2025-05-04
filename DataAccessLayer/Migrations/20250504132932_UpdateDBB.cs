using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgencyPromotionRequest",
                columns: table => new
                {
                    AgencyPromotionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AgencyId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentLevelId = table.Column<int>(type: "int", nullable: false),
                    SuggestedLevelId = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyPromotionRequest", x => x.AgencyPromotionRequestId);
                    table.ForeignKey(
                        name: "FK_AgencyPromotionRequest_AgencyAccount_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "AgencyAccount",
                        principalColumn: "AgencyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgencyScoreHistory",
                columns: table => new
                {
                    AgencyScoreHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AgencyId = table.Column<long>(type: "bigint", nullable: false),
                    ScoreChange = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyScoreHistory", x => x.AgencyScoreHistoryId);
                    table.ForeignKey(
                        name: "FK_AgencyScoreHistory_AgencyAccount_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "AgencyAccount",
                        principalColumn: "AgencyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyPromotionRequest_AgencyId",
                table: "AgencyPromotionRequest",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyScoreHistory_AgencyId",
                table: "AgencyScoreHistory",
                column: "AgencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgencyPromotionRequest");

            migrationBuilder.DropTable(
                name: "AgencyScoreHistory");
        }
    }
}
