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

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Nodes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // CurriculumId on Classes (column, index, FK) is already created by
            // 20260603135815_AddCurriculumIdToClass, which was merged in after this
            // migration was authored. Skip re-creating it here to avoid duplicate column/object errors.

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ClassId",
                table: "Quizzes",
                column: "ClassId");

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

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Quizzes");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                table: "Nodes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

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
