using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Services.Implementation
{
    public class QuizTestRunner
    {
        private readonly IServiceProvider _services;

        public QuizTestRunner(IServiceProvider services)
        {
            _services = services;
        }

        public async Task RunTestsAsync()
        {
            Console.WriteLine("\n==================================================");
            Console.WriteLine("  BẮT ĐẦU CHẠY KIỂM THỬ TOÀN DIỆN END-TO-END E2E  ");
            Console.WriteLine("==================================================");

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Swp391NihongoContext>();
            var quizService = scope.ServiceProvider.GetRequiredService<IQuizService>();

            const int studentId = 2; // student01
            const int classId = 1;   // Class 1
            const int nodeId = 2;    // Node 2 (có sẵn 9 câu hỏi)

            // Dọn dẹp trước khi test
            await CleanupTestRunnerDataAsync(context, studentId);

            try
            {
                // STEP 1: Tạo Quiz Strict Mode cho lớp
                Console.WriteLine("\n[Bước 1] Tạo bài test Strict Mode cho lớp 1...");
                var createRequest = new CreateRandomQuizRequest
                {
                    NodeId = nodeId,
                    ClassId = classId,
                    Title = "Test Integration E2E",
                    QuestionCount = 2,
                    DurationMinutes = 15,
                    PublishNow = true,
                    IsAlwaysOpen = false, // Strict Mode
                    Category = "All"
                };

                var createResult = await quizService.CreateRandomQuizAsync(createRequest);
                if (!createResult.Success || !createResult.QuizId.HasValue)
                {
                    throw new Exception($"Không thể tạo bài test: {createResult.Message}");
                }
                int quizId = createResult.QuizId.Value;
                Console.WriteLine($"-> Tạo thành công bài test ID: #{quizId}");

                // STEP 2: Đảm bảo Node bài học được mở khóa (để học sinh làm được bài test Strict Mode)
                Console.WriteLine("\n[Bước 2] Đảm bảo Node bài học được mở khóa...");
                var nodeStatus = await context.ClassNodeStatuses
                    .FirstOrDefaultAsync(cns => cns.ClassId == classId && cns.NodeId == nodeId);
                if (nodeStatus == null)
                {
                    nodeStatus = new ClassNodeStatus { ClassId = classId, NodeId = nodeId, IsUnlocked = true };
                    context.ClassNodeStatuses.Add(nodeStatus);
                }
                else
                {
                    nodeStatus.IsUnlocked = true;
                }
                await context.SaveChangesAsync();
                Console.WriteLine("-> Node 2 đã được mở khóa cho Lớp 1.");

                // STEP 3: Học sinh làm bài kiểm tra và trả lời sai toàn bộ câu hỏi
                Console.WriteLine("\n[Bước 3] Học sinh làm bài và trả lời SAI toàn bộ...");
                var quizWithQuestions = await context.Quizzes
                    .Include(q => q.QuizQuestions)
                        .ThenInclude(qq => qq.Question)
                            .ThenInclude(q => q.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quizWithQuestions == null || !quizWithQuestions.QuizQuestions.Any())
                {
                    throw new Exception("Lỗi: Bài test mới tạo không có câu hỏi.");
                }

                // Chọn phương án SAI cho các câu hỏi
                var wrongAnswers = new Dictionary<int, int>();
                foreach (var qq in quizWithQuestions.QuizQuestions)
                {
                    var question = qq.Question;
                    var wrongOption = question.QuestionOptions.FirstOrDefault(o => o.IsCorrect == false);
                    if (wrongOption != null)
                    {
                        wrongAnswers[question.Id] = wrongOption.Id;
                    }
                }

                var submitResult = await quizService.SubmitQuizAsync(quizId, studentId, wrongAnswers);
                if (!submitResult.Success)
                {
                    throw new Exception($"Không thể nộp bài test: {submitResult.Message}");
                }
                Console.WriteLine($"-> Nộp bài thành công. Số câu đúng: {submitResult.CorrectAnswers}/{submitResult.TotalQuestions}. Điểm: {submitResult.Score}");

                // Kiểm tra xem lỗi sai đã được lưu vào bảng StudentMistakes chưa
                var mistakesInDb = await context.StudentMistakes
                    .Where(m => m.StudentId == studentId)
                    .ToListAsync();
                if (mistakesInDb.Count < 2)
                {
                    throw new Exception($"Lỗi: Bảng StudentMistakes chỉ ghi nhận {mistakesInDb.Count} lỗi (Kỳ vọng: 2 lỗi).");
                }
                Console.WriteLine($"-> Ghi nhận thành công {mistakesInDb.Count} câu hỏi sai vào bảng StudentMistakes.");

                // STEP 4: Kiểm tra yêu cầu Daily Review và tải câu hỏi ôn tập
                Console.WriteLine("\n[Bước 4] Kiểm tra Daily Review và tải câu hỏi ôn tập...");
                bool isReviewRequired = await quizService.IsDailyReviewRequiredAsync(studentId);
                bool expectedRequiredState = DateTime.Now.Hour >= 20;
                
                Console.WriteLine($"-> Trạng thái bắt buộc Daily Review hiện tại: {isReviewRequired} (Kỳ vọng: {expectedRequiredState} vì giờ hiện tại là {DateTime.Now.Hour}h).");

                var dailyReviewMistakes = await quizService.GetDailyReviewMistakesAsync(studentId, 5);
                if (!dailyReviewMistakes.Any())
                {
                    throw new Exception("Lỗi: Không tải được câu hỏi ôn luyện từ các câu làm sai.");
                }
                Console.WriteLine($"-> Tải thành công {dailyReviewMistakes.Count} câu hỏi từ ngân hàng lỗi sai để ôn tập.");

                // STEP 5: Làm bài Daily Review và trả lời ĐÚNG
                Console.WriteLine("\n[Bước 5] Học sinh hoàn thành Daily Review (trả lời ĐÚNG)...");
                var correctAnswers = new Dictionary<int, int>();
                foreach (var mistake in dailyReviewMistakes)
                {
                    var questionWithOptions = await context.Questions
                        .Include(q => q.QuestionOptions)
                        .FirstOrDefaultAsync(q => q.Id == mistake.QuestionId);

                    if (questionWithOptions != null)
                    {
                        var correctOption = questionWithOptions.QuestionOptions.FirstOrDefault(o => o.IsCorrect == true);
                        if (correctOption != null)
                        {
                            correctAnswers[mistake.QuestionId] = correctOption.Id;
                        }
                    }
                }

                var reviewResult = await quizService.SubmitDailyReviewAsync(studentId, correctAnswers);
                if (!reviewResult.Success)
                {
                    throw new Exception($"Không thể nộp bài Daily Review: {reviewResult.Message}");
                }
                Console.WriteLine($"-> Nộp bài Daily Review thành công. Số câu đúng: {reviewResult.CorrectCount}/{reviewResult.ReviewedCount}.");

                // Kiểm tra xem trạng thái Daily Review đã chuyển về không bắt buộc chưa
                isReviewRequired = await quizService.IsDailyReviewRequiredAsync(studentId);
                if (isReviewRequired)
                {
                    throw new Exception("Lỗi: Daily Review vẫn bắt buộc sau khi học sinh đã hoàn thành.");
                }
                Console.WriteLine("-> Xác nhận: Trạng thái Daily Review đã chuyển sang hoàn thành (Required = false).");
                Console.WriteLine(">>> END-TO-END TEST: PASSED!");

            }
            finally
            {
                // Dọn dẹp dữ liệu kiểm thử
                Console.WriteLine("\nĐang dọn dẹp toàn bộ dữ liệu kiểm thử E2E...");
                await CleanupTestRunnerDataAsync(context, studentId);
                Console.WriteLine("Dọn dẹp hoàn tất.");
            }

            Console.WriteLine("\n==================================================");
            Console.WriteLine("    TẤT CẢ CÁC BƯỚC THỬ NGHIỆM ĐÃ THÀNH CÔNG!     ");
            Console.WriteLine("==================================================");
        }

        private async Task CleanupTestRunnerDataAsync(Swp391NihongoContext context, int studentId)
        {
            // Xóa log ôn tập hàng ngày
            var today = DateOnly.FromDateTime(DateTime.Today);
            var logs = await context.DailyReviewLogs
                .Where(l => l.StudentId == studentId && l.ReviewDate == today)
                .ToListAsync();
            if (logs.Any())
            {
                context.DailyReviewLogs.RemoveRange(logs);
            }

            // Xóa câu hỏi sai
            var mistakes = await context.StudentMistakes
                .Where(m => m.StudentId == studentId)
                .ToListAsync();
            if (mistakes.Any())
            {
                context.StudentMistakes.RemoveRange(mistakes);
            }

            // Xóa bài test E2E và các câu trả lời
            var testQuizzes = await context.Quizzes
                .Where(q => q.Title == "Test Integration E2E")
                .ToListAsync();

            if (testQuizzes.Any())
            {
                var quizIds = testQuizzes.Select(t => t.Id).ToList();

                var answers = await context.QuizAnswers
                    .Where(a => context.QuizResults.Any(r => r.StudentId == studentId && r.Id == a.QuizResultId))
                    .ToListAsync();
                if (answers.Any())
                {
                    context.QuizAnswers.RemoveRange(answers);
                }

                var results = await context.QuizResults
                    .Where(r => r.StudentId == studentId && quizIds.Contains(r.QuizId))
                    .ToListAsync();
                if (results.Any())
                {
                    context.QuizResults.RemoveRange(results);
                }

                foreach (var quiz in testQuizzes)
                {
                    var quizQuestions = await context.QuizQuestions.Where(qq => qq.QuizId == quiz.Id).ToListAsync();
                    context.QuizQuestions.RemoveRange(quizQuestions);
                }

                context.Quizzes.RemoveRange(testQuizzes);
            }

            await context.SaveChangesAsync();
        }
    }
}
