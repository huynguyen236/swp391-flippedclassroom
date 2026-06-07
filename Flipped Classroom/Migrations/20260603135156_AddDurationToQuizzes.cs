using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class AddDurationToQuizzes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Quizzes",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Quizzes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "DailyReviewLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewedCount = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    MasteredCount = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReviewLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReviewLog_Student",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizResultId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    PointEarned = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAnswer_Question",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuizAnswer_Result",
                        column: x => x.QuizResultId,
                        principalTable: "QuizResults",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuizAnswer_SelectedOption",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_DailyReviewLog_Student_Date",
                table: "DailyReviewLogs",
                columns: new[] { "StudentId", "ReviewDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuestionId",
                table: "QuizAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuizResultId",
                table: "QuizAnswers",
                column: "QuizResultId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_SelectedOptionId",
                table: "QuizAnswers",
                column: "SelectedOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyReviewLogs");

            migrationBuilder.DropTable(
                name: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Quizzes");
        }
    }
}
