using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class AddClassNodeStatusAndClassId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Student_Node",
                table: "StudentProgress");

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "StudentProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "QuizResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassNodeStatuses",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassNodeStatus", x => new { x.ClassId, x.NodeId });
                    table.ForeignKey(
                        name: "FK_ClassNodeStatus_Class",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassNodeStatus_Node",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_ClassId",
                table: "StudentProgress",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "UQ_Student_Node_Class",
                table: "StudentProgress",
                columns: new[] { "StudentId", "NodeId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_ClassId",
                table: "QuizResults",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassNodeStatuses_NodeId",
                table: "ClassNodeStatuses",
                column: "NodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Result_Class",
                table: "QuizResults",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Progress_Class",
                table: "StudentProgress",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Result_Class",
                table: "QuizResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Progress_Class",
                table: "StudentProgress");

            migrationBuilder.DropTable(
                name: "ClassNodeStatuses");

            migrationBuilder.DropIndex(
                name: "IX_StudentProgress_ClassId",
                table: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "UQ_Student_Node_Class",
                table: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "IX_QuizResults_ClassId",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "StudentProgress");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "QuizResults");

            migrationBuilder.CreateIndex(
                name: "UQ_Student_Node",
                table: "StudentProgress",
                columns: new[] { "StudentId", "NodeId" },
                unique: true);
        }
    }
}
