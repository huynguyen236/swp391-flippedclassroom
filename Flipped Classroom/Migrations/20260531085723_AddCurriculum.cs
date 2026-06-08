using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Node_Class",
                table: "Nodes");

            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "Nodes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CurriculumId",
                table: "Nodes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Curriculums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurriculumName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Curriculum_Manager",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_CurriculumId",
                table: "Nodes",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_ManagerId",
                table: "Curriculums",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Node_Class",
                table: "Nodes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Node_Curriculum",
                table: "Nodes",
                column: "CurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Node_Class",
                table: "Nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Node_Curriculum",
                table: "Nodes");

            migrationBuilder.DropTable(
                name: "Curriculums");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_CurriculumId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "Nodes");

            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "Nodes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Node_Class",
                table: "Nodes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");
        }
    }
}
