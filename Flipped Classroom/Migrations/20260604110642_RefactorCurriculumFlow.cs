using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCurriculumFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Node_Class",
                table: "Nodes");

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Drop indices before altering columns
            migrationBuilder.DropIndex(
                name: "IX_Nodes_CurriculumId",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_CurriculumId",
                table: "Classes");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Nodes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Classes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ClassId",
                table: "Quizzes",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CurriculumId",
                table: "Classes",
                column: "CurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_Classes_ClassId",
                table: "Nodes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Class",
                table: "Quizzes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_Classes_ClassId",
                table: "Nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_Class",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_ClassId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_CurriculumId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Quizzes");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Classes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Nodes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_CurriculumId",
                table: "Nodes",
                column: "CurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Node_Class",
                table: "Nodes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
