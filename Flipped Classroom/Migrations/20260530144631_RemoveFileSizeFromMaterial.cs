using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFileSizeFromMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Materials",
                type: "bigint",
                nullable: true);
        }
    }
}
