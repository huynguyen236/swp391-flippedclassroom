using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedGradeMilestoneProjectTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "GradeCategories");

            migrationBuilder.DropTable(
                name: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GradeCat__3214EC076B3DA5A8", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeCat_Class",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Projects__3214EC07187C8DAE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_Class",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    GradedBy = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Grades__3214EC0732B994D3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grade_Cat",
                        column: x => x.CategoryId,
                        principalTable: "GradeCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Grade_Student",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Mileston__3214EC076EA52477", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestone_Project",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeCategories_ClassId",
                table: "GradeCategories",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_CategoryId",
                table: "Grades",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId",
                table: "Grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ClassId",
                table: "Projects",
                column: "ClassId");
        }
    }
}
