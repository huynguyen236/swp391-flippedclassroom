using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAudioAndExampleFromVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "Vocabularies");

            migrationBuilder.DropColumn(
                name: "ExampleMeaning",
                table: "Vocabularies");

            migrationBuilder.DropColumn(
                name: "ExampleSentence",
                table: "Vocabularies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "Vocabularies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExampleMeaning",
                table: "Vocabularies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExampleSentence",
                table: "Vocabularies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
