using System;
using System.Collections.Generic;
using Flipped_Classroom.Models;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Data;

public partial class Swp391NihongoContext : DbContext
{
    public Swp391NihongoContext()
    {
    }

    public Swp391NihongoContext(DbContextOptions<Swp391NihongoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assignment> Assignments { get; set; }

    public virtual DbSet<Curriculum> Curriculums { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassMember> ClassMembers { get; set; }

    public virtual DbSet<ClassNodeStatus> ClassNodeStatuses { get; set; }

    public virtual DbSet<DailyReviewLog> DailyReviewLogs { get; set; }

    public virtual DbSet<FeedbackComment> FeedbackComments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeCategory> GradeCategories { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Milestone> Milestones { get; set; }

    public virtual DbSet<Node> Nodes { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<QaReply> QaReplies { get; set; }

    public virtual DbSet<QaThread> QaThreads { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<QuizAnswer> QuizAnswers { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<QuizQuestion> QuizQuestions { get; set; }

    public virtual DbSet<QuizResult> QuizResults { get; set; }

    public virtual DbSet<StudentMistake> StudentMistakes { get; set; }

    public virtual DbSet<StudentProgress> StudentProgresses { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SWP391_Nihongo;Trusted_Connection=SSPI;Encrypt=false");


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assignme__3214EC07909120E8");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Class).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignment_Class");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignment_Teacher");

            entity.HasOne(d => d.Node).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.NodeId)
                .HasConstraintName("FK_Assignment_Node");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Classes__3214EC071D7703CC");

            entity.HasIndex(e => e.InviteCode, "UQ__Classes__B8659E393A96C081").IsUnique();

            entity.Property(e => e.ClassName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InviteCode).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Manager).WithMany(p => p.Classes)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Manager");

            entity.HasOne(d => d.Curriculum).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CurriculumId)
                .OnDelete(DeleteBehavior.ClientSetNull) // Dùng ClientSetNull để tránh lỗi Cascade loop
                .HasConstraintName("FK_Class_Curriculum");
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClassMem__3214EC07EB971E15");

            entity.HasIndex(e => new { e.ClassId, e.UserId }, "UQ_Class_User").IsUnique();

            entity.Property(e => e.IsSupportTeam).HasDefaultValue(false);
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Member_Class");

            entity.HasOne(d => d.User).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Member_User");
        });
        modelBuilder.Entity<ClassNodeStatus>(entity =>
        {
            entity.HasKey(e => new { e.ClassId, e.NodeId }).HasName("PK_ClassNodeStatus");

            entity.Property(e => e.IsUnlocked).HasDefaultValue(false);
            entity.Property(e => e.UnlockedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Class).WithMany()
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClassNodeStatus_Class");

            entity.HasOne(d => d.Node).WithMany()
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClassNodeStatus_Node");
        });

        modelBuilder.Entity<Curriculum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Curriculums");

            entity.Property(e => e.CurriculumName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Manager).WithMany(p => p.Curriculums)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Curriculum_Manager");
        });

        modelBuilder.Entity<DailyReviewLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_DailyReviewLogs");

            entity.HasIndex(e => new { e.StudentId, e.ReviewDate }, "UQ_DailyReviewLog_Student_Date")
                .IsUnique();

            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Student).WithMany(p => p.DailyReviewLogs)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DailyReviewLog_Student");
        });

        modelBuilder.Entity<FeedbackComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3214EC07504212DA");

            entity.ToTable("Feedback_Comments");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TimelineStamp)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Reviewer).WithMany(p => p.FeedbackComments)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Reviewer");

            entity.HasOne(d => d.Submission).WithMany(p => p.FeedbackComments)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Submission");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Grades__3214EC0732B994D3");

            entity.Property(e => e.GradedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Grades)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grade_Cat");

            entity.HasOne(d => d.Student).WithMany(p => p.Grades)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grade_Student");
        });

        modelBuilder.Entity<GradeCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GradeCat__3214EC076B3DA5A8");

            entity.Property(e => e.CategoryName).HasMaxLength(100);

            entity.HasOne(d => d.Class).WithMany(p => p.GradeCategories)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GradeCat_Class");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Groups__3214EC07F0DFEE88");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GroupName).HasMaxLength(100);

            entity.HasOne(d => d.Class).WithMany(p => p.Groups)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Group_Class");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GroupMem__3214EC0739F6DEBA");

            entity.HasIndex(e => new { e.GroupId, e.StudentId }, "UQ_Group_Student").IsUnique();

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMember_Group");

            entity.HasOne(d => d.Student).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMember_Student");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Material__3214EC074BD3A5D7");

            entity.Property(e => e.MaterialType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Node).WithMany(p => p.Materials)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Material_Node");
        });

        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mileston__3214EC076EA52477");

            entity.Property(e => e.Deadline).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Project).WithMany(p => p.Milestones)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Milestone_Project");
        });

        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Nodes__3214EC07510306F0");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsReviewNode).HasDefaultValue(false);
            entity.Property(e => e.NodeOrder).HasDefaultValue(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Curriculum).WithMany(p => p.Nodes)
                .HasForeignKey(d => d.CurriculumId)
                .OnDelete(DeleteBehavior.Cascade) // Nếu xóa Curriculum thì xóa hết Node bên trong
                .HasConstraintName("FK_Node_Curriculum");

            entity.HasOne(d => d.ParentNode).WithMany(p => p.InverseParentNode)
                .HasForeignKey(d => d.ParentNodeId)
                .HasConstraintName("FK_Node_Parent");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Projects__3214EC07187C8DAE");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProjectName).HasMaxLength(200);

            entity.HasOne(d => d.Class).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Class");
        });

        modelBuilder.Entity<QaReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QA_Repli__3214EC07961B157A");

            entity.ToTable("QA_Replies");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.QaThreadId).HasColumnName("QA_ThreadId");

            entity.HasOne(d => d.QaThread).WithMany(p => p.QaReplies)
                .HasForeignKey(d => d.QaThreadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reply_QA");

            entity.HasOne(d => d.User).WithMany(p => p.QaReplies)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reply_User");
        });

        modelBuilder.Entity<QaThread>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QA_Threa__3214EC0771C493A0");

            entity.ToTable("QA_Threads");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpvoteCount).HasDefaultValue(0);

            entity.HasOne(d => d.Class).WithMany(p => p.QaThreads)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_QA_Class");

            entity.HasOne(d => d.Node).WithMany(p => p.QaThreads)
                .HasForeignKey(d => d.NodeId)
                .HasConstraintName("FK_QA_Node");

            entity.HasOne(d => d.Student).WithMany(p => p.QaThreads)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_QA_Student");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Question__3214EC07A73FBC5A");

            entity.Property(e => e.DifficultyLevel).HasDefaultValue(1);
            entity.Property(e => e.IsQuestionBank).HasDefaultValue(false);
            entity.Property(e => e.QuestionType).HasMaxLength(50);
            entity.Property(e => e.Visibility)
                .HasMaxLength(50)
                .HasDefaultValue("Always");

            entity.HasOne(d => d.Node).WithMany(p => p.Questions)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Question_Node");
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Question__3214EC07D659E11E");

            entity.Property(e => e.IsCorrect).HasDefaultValue(false);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionOptions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Option_Question");
        });

        modelBuilder.Entity<QuizAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_QuizAnswers");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PointEarned)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Question).WithMany(p => p.QuizAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAnswer_Question");

            entity.HasOne(d => d.QuizResult).WithMany(p => p.QuizAnswers)
                .HasForeignKey(d => d.QuizResultId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAnswer_Result");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.QuizAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .HasConstraintName("FK_QuizAnswer_SelectedOption");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Quizzes__3214EC078FCFA5F4");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PublishedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Node).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_Node");

            entity.HasOne(d => d.Class).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_Class");
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QuizQues__3214EC0744CD312F");

            entity.Property(e => e.DisplayOrder).HasDefaultValue(1);
            entity.Property(e => e.Point)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Question).WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QQ_Question");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QQ_Quiz");
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QuizResu__3214EC07E4777785");

            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Class).WithMany()
                .HasForeignKey(d => d.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Result_Class");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizResults)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Result_Quiz");

            entity.HasOne(d => d.Student).WithMany(p => p.QuizResults)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Result_Student");
        });

        modelBuilder.Entity<StudentMistake>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentM__3214EC0743102BE5");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ErrorCount).HasDefaultValue(1);
            entity.Property(e => e.MistakeType).HasMaxLength(50);

            entity.HasOne(d => d.Question).WithMany(p => p.StudentMistakes)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mistake_Question");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentMistakes)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mistake_Student");
        });

        modelBuilder.Entity<StudentProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentP__3214EC072617F85C");

            entity.ToTable("StudentProgress");

            entity.HasIndex(e => new { e.StudentId, e.NodeId, e.ClassId }, "UQ_Student_Node_Class").IsUnique();

            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);

            entity.HasOne(d => d.Class).WithMany()
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Progress_Class");

            entity.HasOne(d => d.Node).WithMany(p => p.StudentProgresses)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Progress_Node");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentProgresses)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Progress_Student");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3214EC077AE01F90");

            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Submitted");
            entity.Property(e => e.SubmitAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Assignment).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_Assignment");

            entity.HasOne(d => d.Group).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Submission_Group");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_Student");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07D70BBCD3");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E47AD89E9C").IsUnique();

            entity.HasIndex(e => e.Email, "UX_Users_Email")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.HasIndex(e => e.GoogleId, "UX_Users_GoogleId")
                .IsUnique()
                .HasFilter("([GoogleId] IS NOT NULL)");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.GoogleId).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
            entity.Property(e => e.PasswordResetTokenExpiry).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ClassSchedules");

            entity.Property(e => e.Room).HasMaxLength(50);

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSchedules)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClassSchedule_Class");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
