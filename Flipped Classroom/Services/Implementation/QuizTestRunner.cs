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

                var submitResult = await quizService.SubmitQuizAsync(quizId, studentId, wrongAnswers, new Dictionary<int, string>());
                if (!submitResult.Success)
                {
                    throw new Exception($"Không thể nộp bài test: {submitResult.Message}");
                }
                Console.WriteLine($"-> Nộp bài thành công. Số câu đúng: {submitResult.CorrectAnswers}/{submitResult.TotalQuestions}. Điểm: {submitResult.Score}");

                // Kiểm tra xem lỗi sai đã được lưu vào bảng StudentMistakes chưa
                var mistakesInDb = await context.StudentMistakes
                    .Where(m => m.StudentId == studentId && m.IsResolved != true)
                    .ToListAsync();
                if (mistakesInDb.Count < 2)
                {
                    throw new Exception($"Lỗi: Bảng StudentMistakes chỉ ghi nhận {mistakesInDb.Count} lỗi hoạt động (Kỳ vọng: 2 lỗi).");
                }
                Console.WriteLine($"-> Ghi nhận thành công {mistakesInDb.Count} câu hỏi sai hoạt động vào bảng StudentMistakes.");

                // STEP 4: Kiểm tra thống kê lỗi sai của Giáo viên (Thấy câu hỏi bị làm sai)
                Console.WriteLine("\n[Bước 4] Kiểm tra Dashboard thống kê lỗi sai của Giáo viên...");
                var statsBefore = await quizService.GetMistakeStatisticsAsync(classId);
                var targetQuestionId = mistakesInDb.First().QuestionId;
                var hasQuestionInStats = statsBefore.Any(s => s.QuestionId == targetQuestionId);
                if (!hasQuestionInStats)
                {
                    throw new Exception($"Lỗi: Không tìm thấy câu hỏi #{targetQuestionId} trong bảng thống kê của lớp {classId}.");
                }
                Console.WriteLine($"-> Xác nhận: Câu hỏi #{targetQuestionId} xuất hiện trong bảng thống kê lỗi sai.");

                // STEP 5: Giáo viên đánh dấu đã chữa câu hỏi đó
                Console.WriteLine($"\n[Bước 5] Giáo viên đánh dấu đã chữa câu hỏi #{targetQuestionId}...");
                await quizService.ResolveQuestionMistakesForClassAsync(targetQuestionId, classId);

                // Kiểm tra lại bảng thống kê xem câu hỏi đó đã biến mất chưa
                var statsAfter = await quizService.GetMistakeStatisticsAsync(classId);
                var isResolvedInStats = !statsAfter.Any(s => s.QuestionId == targetQuestionId);
                if (!isResolvedInStats)
                {
                    throw new Exception($"Lỗi: Câu hỏi #{targetQuestionId} vẫn xuất hiện trên Dashboard sau khi đã chữa.");
                }
                Console.WriteLine("-> Xác nhận: Câu hỏi đã biến mất khỏi bảng thống kê lỗi sai của lớp.");

                // STEP 6: Học sinh vẫn phải học câu đó trong Daily Review (dù giáo viên đã chữa trên lớp)
                Console.WriteLine("\n[Bước 6] Học sinh kiểm tra Daily Review (phải có cả câu đã chữa)...");
                var dailyReviewMistakes = await quizService.GetDailyReviewMistakesAsync(studentId, 5);
                var containsResolvedQuestion = dailyReviewMistakes.Any(m => m.QuestionId == targetQuestionId);
                if (!containsResolvedQuestion)
                {
                    throw new Exception($"Lỗi: Học sinh không thấy câu hỏi đã chữa #{targetQuestionId} trong Daily Review cá nhân.");
                }
                Console.WriteLine("-> Xác nhận: Học sinh vẫn phải ôn tập câu hỏi đã chữa như cũ.");

                // STEP 7: Học sinh làm Daily Review câu hỏi kia đúng và câu này sai tiếp để kiểm tra tự động mở lại
                Console.WriteLine("\n[Bước 7] Học sinh làm Daily Review (đúng 1 câu, sai tiếp câu đã chữa)...");
                var correctAnswers = new Dictionary<int, int>();
                var wrongReviewAnswers = new Dictionary<int, int>();
                
                foreach (var mistake in dailyReviewMistakes)
                {
                    var questionWithOptions = await context.Questions
                        .Include(q => q.QuestionOptions)
                        .FirstOrDefaultAsync(q => q.Id == mistake.QuestionId);

                    if (questionWithOptions != null)
                    {
                        if (mistake.QuestionId == targetQuestionId)
                        {
                            // Trả lời sai tiếp câu đã chữa
                            var wrongOption = questionWithOptions.QuestionOptions.FirstOrDefault(o => o.IsCorrect == false);
                            if (wrongOption != null)
                            {
                                wrongReviewAnswers[mistake.QuestionId] = wrongOption.Id;
                            }
                        }
                        else
                        {
                            // Trả lời đúng các câu khác
                            var correctOption = questionWithOptions.QuestionOptions.FirstOrDefault(o => o.IsCorrect == true);
                            if (correctOption != null)
                            {
                                correctAnswers[mistake.QuestionId] = correctOption.Id;
                            }
                        }
                    }
                }

                // Gộp chung câu trả lời để nộp
                var allReviewAnswers = new Dictionary<int, int>(correctAnswers);
                foreach (var kvp in wrongReviewAnswers)
                {
                    allReviewAnswers[kvp.Key] = kvp.Value;
                }

                var reviewResult = await quizService.SubmitDailyReviewAsync(studentId, allReviewAnswers, new Dictionary<int, string>());
                if (!reviewResult.Success)
                {
                    throw new Exception($"Không thể nộp bài Daily Review: {reviewResult.Message}");
                }
                Console.WriteLine($"-> Nộp bài Daily Review thành công.");

                // STEP 8: Kiểm tra xem câu hỏi trả lời sai tiếp đã xuất hiện trở lại Dashboard của Giáo viên chưa
                Console.WriteLine("\n[Bước 8] Kiểm tra xem câu làm sai lại có tái xuất hiện trên Dashboard...");
                var statsFinal = await quizService.GetMistakeStatisticsAsync(classId);
                var reappearedInStats = statsFinal.Any(s => s.QuestionId == targetQuestionId);
                if (!reappearedInStats)
                {
                    throw new Exception($"Lỗi: Câu hỏi #{targetQuestionId} không xuất hiện trở lại Dashboard dù học sinh tiếp tục làm sai.");
                }
                Console.WriteLine("-> Xác nhận: Câu hỏi đã tự động xuất hiện lại trên Dashboard do học sinh làm sai lại.");
                Console.WriteLine("\n>>> TẤT CẢ CÁC BƯỚC INTEGRATION TEST E2E: THÀNH CÔNG RỰC RỠ (PASSED)!");

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
