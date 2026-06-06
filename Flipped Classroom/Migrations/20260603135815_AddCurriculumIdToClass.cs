using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumIdToClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add column if it doesn't already exist - this handles the duplicate issue
            migrationBuilder.Sql(
                @"IF NOT EXISTS(
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME='Classes' AND COLUMN_NAME='CurriculumId'
                  )
                  BEGIN
                    ALTER TABLE [Classes] ADD [CurriculumId] int NULL;
                  END");

            migrationBuilder.Sql(
                @"IF NOT EXISTS(
                    SELECT 1 FROM sys.indexes 
                    WHERE object_id = OBJECT_ID('Classes') 
                    AND name = 'IX_Classes_CurriculumId'
                  )
                  BEGIN
                    CREATE INDEX [IX_Classes_CurriculumId] ON [Classes] ([CurriculumId]);
                  END");

            migrationBuilder.Sql(
                @"IF NOT EXISTS(
                    SELECT 1 FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
                    WHERE CONSTRAINT_NAME='FK_Class_Curriculum'
                  )
                  BEGIN
                    ALTER TABLE [Classes] ADD CONSTRAINT [FK_Class_Curriculum] FOREIGN KEY ([CurriculumId]) 
                    REFERENCES [Curriculums]([Id]) ON DELETE SET NULL;
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF EXISTS(
                    SELECT 1 FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
                    WHERE CONSTRAINT_NAME='FK_Class_Curriculum'
                  )
                  BEGIN
                    ALTER TABLE [Classes] DROP CONSTRAINT [FK_Class_Curriculum];
                  END");

            migrationBuilder.Sql(
                @"IF EXISTS(
                    SELECT 1 FROM sys.indexes 
                    WHERE object_id = OBJECT_ID('Classes') 
                    AND name = 'IX_Classes_CurriculumId'
                  )
                  BEGIN
                    DROP INDEX [IX_Classes_CurriculumId] ON [Classes];
                  END");

            migrationBuilder.Sql(
                @"IF EXISTS(
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME='Classes' AND COLUMN_NAME='CurriculumId'
                  )
                  BEGIN
                    ALTER TABLE [Classes] DROP COLUMN [CurriculumId];
                  END");
        }
    }
}
