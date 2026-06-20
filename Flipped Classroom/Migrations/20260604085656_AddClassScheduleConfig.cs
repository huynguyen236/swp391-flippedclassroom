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
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[ClassSchedule]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassSchedule_Classes_ClassId')
                        ALTER TABLE [ClassSchedule] DROP CONSTRAINT [FK_ClassSchedule_Classes_ClassId];

                    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ClassSchedule' AND parent_object_id = OBJECT_ID(N'[ClassSchedule]'))
                        ALTER TABLE [ClassSchedule] DROP CONSTRAINT [PK_ClassSchedule];

                    EXEC sp_rename 'ClassSchedule', 'ClassSchedules';
                END

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClassSchedule_ClassId' AND object_id = OBJECT_ID(N'[ClassSchedules]'))
                BEGIN
                    EXEC sp_rename 'ClassSchedules.IX_ClassSchedule_ClassId', 'IX_ClassSchedules_ClassId', 'INDEX';
                END

                ALTER TABLE [ClassSchedules] ALTER COLUMN [Room] nvarchar(50) NULL;

                DECLARE @pkName nvarchar(255);
                SELECT @pkName = name 
                FROM sys.key_constraints 
                WHERE parent_object_id = OBJECT_ID(N'[ClassSchedules]') AND type = 'PK';

                IF @pkName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [ClassSchedules] DROP CONSTRAINT [' + @pkName + ']');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ClassSchedules' AND parent_object_id = OBJECT_ID(N'[ClassSchedules]'))
                BEGIN
                    ALTER TABLE [ClassSchedules] ADD CONSTRAINT [PK_ClassSchedules] PRIMARY KEY ([Id]);
                END

                DECLARE @fkName nvarchar(255);
                SELECT @fkName = name 
                FROM sys.foreign_keys 
                WHERE parent_object_id = OBJECT_ID(N'[ClassSchedules]') AND referenced_object_id = OBJECT_ID(N'[Classes]');

                IF @fkName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [ClassSchedules] DROP CONSTRAINT [' + @fkName + ']');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassSchedule_Class')
                BEGIN
                    ALTER TABLE [ClassSchedules] ADD CONSTRAINT [FK_ClassSchedule_Class] 
                        FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[ClassSchedules]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassSchedule_Class')
                        ALTER TABLE [ClassSchedules] DROP CONSTRAINT [FK_ClassSchedule_Class];

                    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ClassSchedules' AND parent_object_id = OBJECT_ID(N'[ClassSchedules]'))
                        ALTER TABLE [ClassSchedules] DROP CONSTRAINT [PK_ClassSchedules];

                    EXEC sp_rename 'ClassSchedules', 'ClassSchedule';
                END

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClassSchedules_ClassId' AND object_id = OBJECT_ID(N'[ClassSchedule]'))
                BEGIN
                    EXEC sp_rename 'ClassSchedule.IX_ClassSchedules_ClassId', 'IX_ClassSchedule_ClassId', 'INDEX';
                END

                ALTER TABLE [ClassSchedule] ALTER COLUMN [Room] nvarchar(max) NULL;

                DECLARE @pkName nvarchar(255);
                SELECT @pkName = name 
                FROM sys.key_constraints 
                WHERE parent_object_id = OBJECT_ID(N'[ClassSchedule]') AND type = 'PK';

                IF @pkName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [ClassSchedule] DROP CONSTRAINT [' + @pkName + ']');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ClassSchedule' AND parent_object_id = OBJECT_ID(N'[ClassSchedule]'))
                BEGIN
                    ALTER TABLE [ClassSchedule] ADD CONSTRAINT [PK_ClassSchedule] PRIMARY KEY ([Id]);
                END

                DECLARE @fkName nvarchar(255);
                SELECT @fkName = name 
                FROM sys.foreign_keys 
                WHERE parent_object_id = OBJECT_ID(N'[ClassSchedule]') AND referenced_object_id = OBJECT_ID(N'[Classes]');

                IF @fkName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [ClassSchedule] DROP CONSTRAINT [' + @fkName + ']');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClassSchedule_Classes_ClassId')
                BEGIN
                    ALTER TABLE [ClassSchedule] ADD CONSTRAINT [FK_ClassSchedule_Classes_ClassId] 
                        FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE;
                END
                """);
        }
    }
}
