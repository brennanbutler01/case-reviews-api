using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace case_reviews_server.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    ORNumber = table.Column<string>(type: "text", nullable: false),
                    Office = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: false),
                    Program = table.Column<int>(type: "integer", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsTargeted = table.Column<bool>(type: "boolean", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CaseNumber = table.Column<int>(type: "integer", nullable: false),
                    OtherComments = table.Column<string>(type: "text", nullable: true),
                    ReportingSystem = table.Column<int>(type: "integer", nullable: true),
                    MagiSubProgram = table.Column<int>(type: "integer", nullable: true),
                    NonMagiSubProgram = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MagiEligibles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdultsEligibleActual = table.Column<int>(type: "integer", nullable: false),
                    AdultsEligibleCoded = table.Column<int>(type: "integer", nullable: false),
                    AdultsNotEligibleActual = table.Column<int>(type: "integer", nullable: false),
                    AdultsNotEligibleCoded = table.Column<int>(type: "integer", nullable: false),
                    ChildrenEligibleActual = table.Column<int>(type: "integer", nullable: false),
                    ChildrenEligibleCoded = table.Column<int>(type: "integer", nullable: false),
                    ChildrenNotEligibleActual = table.Column<int>(type: "integer", nullable: false),
                    ChildrenNotEligibleCoded = table.Column<int>(type: "integer", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagiEligibles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagiEligibles_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedElement = table.Column<int>(type: "integer", nullable: false),
                    Program = table.Column<int>(type: "integer", nullable: false),
                    IsReviewed = table.Column<bool>(type: "boolean", nullable: false),
                    IsError = table.Column<bool>(type: "boolean", nullable: false),
                    HasAction = table.Column<bool>(type: "boolean", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewElements_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MagiEligibles_ReviewId",
                table: "MagiEligibles",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewElements_ReviewId",
                table: "ReviewElements",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_StaffId",
                table: "Reviews",
                column: "StaffId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagiEligibles");

            migrationBuilder.DropTable(
                name: "ReviewElements");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Staff");
        }
    }
}
