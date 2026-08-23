-- =========================================================================================
-- SCRIPT TẠO DỮ LIỆU MẪU (MOCK DATA) CHO HỆ THỐNG FLIPPED CLASSROOM TIẾNG NHẬT
-- Database: SWP391_Nihongo
-- Lưu ý: Mật khẩu mặc định cho tất cả tài khoản mẫu là: 123456
-- =========================================================================================

USE SWP391_Nihongo;
GO

-- 1. XÓA DỮ LIỆU CŨ NẾU CẦN RESET (Bỏ comment các dòng bên dưới nếu muốn xóa sạch DB làm lại từ đầu)
/*
DELETE FROM StudentProgress;
DELETE FROM StudentMistakes;
DELETE FROM DailyReviewLogs;
DELETE FROM QA_Replies;
DELETE FROM QA_Threads;
DELETE FROM Submissions;
DELETE FROM Assignments;
DELETE FROM QuizAnswers;
DELETE FROM QuizResults;
DELETE FROM QuizQuestions;
DELETE FROM Quizzes;
DELETE FROM QuestionOptions;
DELETE FROM Questions;
DELETE FROM Materials;
DELETE FROM Vocabularies;
DELETE FROM ClassSchedules;
DELETE FROM ClassNodeStatuses;
DELETE FROM ClassMembers;
DELETE FROM GroupMembers;
DELETE FROM Groups;
DELETE FROM Classes;
DELETE FROM Nodes;
DELETE FROM Curriculums;
DELETE FROM Users;
*/

-- 2. THÊM TÀI KHOẢN NGƯỜI DÙNG (Users)
-- Password hash tương ứng với mật khẩu '123456': jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Email, FirstName, LastName, Gender, Role, IsActive, CreatedAt, PhoneNumber, Address)
    VALUES 
    ('admin', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'admin@nihongo.edu.vn', N'Hệ thống', N'Admin', N'Nam', 'Admin', 1, GETDATE(), '0901000001', N'Hà Nội'),
    ('manager', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'manager@nihongo.edu.vn', N'Trần', N'Quản Lý', N'Nữ', 'Manager', 1, GETDATE(), '0901000002', N'Hà Nội'),
    ('teacher1', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'tanaka.sensei@nihongo.edu.vn', N'Tanaka', N'Kenji', N'Nam', 'Teacher', 1, GETDATE(), '0901000003', N'Tokyo / Hà Nội'),
    ('teacher2', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'yamada.sensei@nihongo.edu.vn', N'Yamada', N'Sakura', N'Nữ', 'Teacher', 1, GETDATE(), '0901000004', N'Osaka / Hà Nội'),
    ('student1', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'nguyenvana@gmail.com', N'Nguyễn', N'Văn A', N'Nam', 'Student', 1, GETDATE(), '0902000001', N'Cầu Giấy, Hà Nội'),
    ('student2', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'tranthib@gmail.com', N'Trần', N'Thị B', N'Nữ', 'Student', 1, GETDATE(), '0902000002', N'Đống Đa, Hà Nội'),
    ('student3', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'levanc@gmail.com', N'Lê', N'Văn C', N'Nam', 'Student', 1, GETDATE(), '0902000003', N'Thanh Xuân, Hà Nội'),
    ('student4', 'jZae727K08KaOmKSFbYFYGJRinnB/TOdVVA5NOgTlRN=', 'phamthid@gmail.com', N'Phạm', N'Thị D', N'Nữ', 'Student', 1, GETDATE(), '0902000004', N'Ba Đình, Hà Nội');
END
GO

PRINT N'Đã hoàn thành nạp Mock Data!';
