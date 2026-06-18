using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFlashcardsToCurriculumLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashcardSets_Nodes_NodeId",
                table: "FlashcardSets");

            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "FlashcardSets",
                newName: "CurriculumId");

            migrationBuilder.RenameIndex(
                name: "IX_FlashcardSets_NodeId",
                table: "FlashcardSets",
                newName: "IX_FlashcardSets_CurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashcardSets_Curriculums_CurriculumId",
                table: "FlashcardSets",
                column: "CurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashcardSets_Curriculums_CurriculumId",
                table: "FlashcardSets");

            migrationBuilder.RenameColumn(
                name: "CurriculumId",
                table: "FlashcardSets",
                newName: "NodeId");

            migrationBuilder.RenameIndex(
                name: "IX_FlashcardSets_CurriculumId",
                table: "FlashcardSets",
                newName: "IX_FlashcardSets_NodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashcardSets_Nodes_NodeId",
                table: "FlashcardSets",
                column: "NodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
