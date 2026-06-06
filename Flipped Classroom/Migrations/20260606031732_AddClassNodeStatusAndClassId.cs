using Flipped_Classroom.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flipped_Classroom.Migrations
{
    [DbContext(typeof(Swp391NihongoContext))]
    [Migration("20260606031732_AddClassNodeStatusAndClassId")]
    public partial class AddClassNodeStatusAndClassId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Student_Node' AND parent_object_id = OBJECT_ID(N'[StudentProgress]'))
                    ALTER TABLE [StudentProgress] DROP CONSTRAINT [UQ_Student_Node];
                ELSE IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Student_Node' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    DROP INDEX [UQ_Student_Node] ON [StudentProgress];

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StudentProgress' AND COLUMN_NAME = 'ClassId'
                )
                    ALTER TABLE [StudentProgress] ADD [ClassId] int NOT NULL CONSTRAINT [DF_StudentProgress_ClassId] DEFAULT 0;

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'QuizResults' AND COLUMN_NAME = 'ClassId'
                )
                    ALTER TABLE [QuizResults] ADD [ClassId] int NULL;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Nodes' AND COLUMN_NAME = 'ClassId'
                )
                BEGIN
                    UPDATE sp
                    SET [ClassId] = n.[ClassId]
                    FROM [StudentProgress] sp
                    INNER JOIN [Nodes] n ON n.[Id] = sp.[NodeId]
                    WHERE sp.[ClassId] = 0 AND n.[ClassId] IS NOT NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[ClassNodeStatuses]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ClassNodeStatuses] (
                        [ClassId] int NOT NULL,
                        [NodeId] int NOT NULL,
                        [IsUnlocked] bit NOT NULL CONSTRAINT [DF_ClassNodeStatuses_IsUnlocked] DEFAULT CAST(0 AS bit),
                        [UnlockedAt] datetime NULL,
                        CONSTRAINT [PK_ClassNodeStatus] PRIMARY KEY ([ClassId], [NodeId])
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProgress_ClassId' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    CREATE INDEX [IX_StudentProgress_ClassId] ON [StudentProgress] ([ClassId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Student_Node_Class' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    CREATE UNIQUE INDEX [UQ_Student_Node_Class] ON [StudentProgress] ([StudentId], [NodeId], [ClassId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuizResults_ClassId' AND object_id = OBJECT_ID(N'[QuizResults]'))
                    CREATE INDEX [IX_QuizResults_ClassId] ON [QuizResults] ([ClassId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClassNodeStatuses_NodeId' AND object_id = OBJECT_ID(N'[ClassNodeStatuses]'))
                    CREATE INDEX [IX_ClassNodeStatuses_NodeId] ON [ClassNodeStatuses] ([NodeId]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassNodeStatus_Class')
                    ALTER TABLE [ClassNodeStatuses] ADD CONSTRAINT [FK_ClassNodeStatus_Class]
                    FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassNodeStatus_Node')
                    ALTER TABLE [ClassNodeStatuses] ADD CONSTRAINT [FK_ClassNodeStatus_Node]
                    FOREIGN KEY ([NodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Result_Class')
                    ALTER TABLE [QuizResults] ADD CONSTRAINT [FK_Result_Class]
                    FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE SET NULL;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Progress_Class')
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [StudentProgress] sp
                        LEFT JOIN [Classes] c ON c.[Id] = sp.[ClassId]
                        WHERE c.[Id] IS NULL
                    )
                    ALTER TABLE [StudentProgress] ADD CONSTRAINT [FK_Progress_Class]
                    FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Result_Class')
                    ALTER TABLE [QuizResults] DROP CONSTRAINT [FK_Result_Class];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Progress_Class')
                    ALTER TABLE [StudentProgress] DROP CONSTRAINT [FK_Progress_Class];

                IF OBJECT_ID(N'[ClassNodeStatuses]', N'U') IS NOT NULL
                    DROP TABLE [ClassNodeStatuses];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProgress_ClassId' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    DROP INDEX [IX_StudentProgress_ClassId] ON [StudentProgress];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Student_Node_Class' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    DROP INDEX [UQ_Student_Node_Class] ON [StudentProgress];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuizResults_ClassId' AND object_id = OBJECT_ID(N'[QuizResults]'))
                    DROP INDEX [IX_QuizResults_ClassId] ON [QuizResults];

                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StudentProgress' AND COLUMN_NAME = 'ClassId'
                )
                BEGIN
                    DECLARE @StudentProgressDefaultConstraint nvarchar(128);
                    SELECT @StudentProgressDefaultConstraint = dc.[name]
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'[StudentProgress]')
                      AND c.[name] = 'ClassId';

                    IF @StudentProgressDefaultConstraint IS NOT NULL
                        EXEC(N'ALTER TABLE [StudentProgress] DROP CONSTRAINT [' + @StudentProgressDefaultConstraint + N']');

                    ALTER TABLE [StudentProgress] DROP COLUMN [ClassId];
                END

                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'QuizResults' AND COLUMN_NAME = 'ClassId'
                )
                    ALTER TABLE [QuizResults] DROP COLUMN [ClassId];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Student_Node' AND object_id = OBJECT_ID(N'[StudentProgress]'))
                    CREATE UNIQUE INDEX [UQ_Student_Node] ON [StudentProgress] ([StudentId], [NodeId]);
                """);
        }
    }
}
