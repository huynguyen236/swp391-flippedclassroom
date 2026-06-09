using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class AddClassScheduleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedule_Classes_ClassId",
                table: "ClassSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSchedule",
                table: "ClassSchedule");

            migrationBuilder.RenameTable(
                name: "ClassSchedule",
                newName: "ClassSchedules");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSchedule_ClassId",
                table: "ClassSchedules",
                newName: "IX_ClassSchedules_ClassId");

            migrationBuilder.AlterColumn<string>(
                name: "Room",
                table: "ClassSchedules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSchedules",
                table: "ClassSchedules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedule_Class",
                table: "ClassSchedules",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedule_Class",
                table: "ClassSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSchedules",
                table: "ClassSchedules");

            migrationBuilder.RenameTable(
                name: "ClassSchedules",
                newName: "ClassSchedule");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSchedules_ClassId",
                table: "ClassSchedule",
                newName: "IX_ClassSchedule_ClassId");

            migrationBuilder.AlterColumn<string>(
                name: "Room",
                table: "ClassSchedule",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSchedule",
                table: "ClassSchedule",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedule_Classes_ClassId",
                table: "ClassSchedule",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
