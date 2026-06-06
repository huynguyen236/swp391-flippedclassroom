using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Flipped_Classroom.Services.Implementation
{
    public class QuizService : IQuizService
    {
        private const string PublishedStatus = "Published";
        private const string DraftStatus = "Draft";
        private const string AllCategories = "All";
        private const int DailyReviewQuestionCount = 5;

        private readonly Swp391NihongoContext _context;

        public QuizService(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<List<Node>> GetNodesAsync()
        {
            return await _context.Nodes
                .Include(n => n.Class)
                .Where(n => n.IsActive != false)
                .OrderBy(n => n.Class.ClassName)
                .ThenBy(n => n.NodeOrder)
                .ThenBy(n => n.Title)
                .ToListAsync();
        }

        public async Task<List<Class>> GetClassesAsync()
        {
            return await _context.Classes
                .OrderBy(c => c.ClassName)
                .ToListAsync();
        }

        public async Task<List<Quiz>> GetRecentQuizzesAsync()
        {
            return await _context.Quizzes
                .Include(q => q.Node)
                    .ThenInclude(n => n.Class)
                .Include(q => q.QuizQuestions)
                .OrderByDescending(q => q.CreatedAt)
                .ThenByDescending(q => q.Id)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<Quiz>> GetPublishedQuizzesForStudentAsync(int studentId)
        {
            var classIds = _context.ClassMembers
                .Where(cm => cm.UserId == studentId)
                .Select(cm => cm.ClassId);

            return await _context.Quizzes
                .Include(q => q.Node)
                    .ThenInclude(n => n.Class)
                .Include(q => q.QuizQuestions)
                .Where(q => q.Status == PublishedStatus && classIds.Contains(q.Node.ClassId ?? 0))
                .OrderByDescending(q => q.PublishedAt)
                .ThenByDescending(q => q.Id)
                .ToListAsync();
        }

        public async Task<Quiz?> GetPublishedQuizForStudentAsync(int quizId, int studentId)
        {
            var classIds = _context.ClassMembers
                .Where(cm => cm.UserId == studentId)
                .Select(cm => cm.ClassId);

            return await _context.Quizzes
                .Include(q => q.Node)
                    .ThenInclude(n => n.Class)
                .Include(q => q.QuizQuestions)
                    .ThenInclude(qq => qq.Question)
                        .ThenInclude(question => question.QuestionOptions)
                .FirstOrDefaultAsync(q => q.Id == quizId
                    && q.Status == PublishedStatus
                    && classIds.Contains(q.Node.ClassId ?? 0));
        }

        public async Task<int> CountAvailableQuestionsAsync(int nodeId, string category)
        {
            return await BuildEligibleQuestionQuery(nodeId, category).CountAsync();
        }

        public async Task<CreateRandomQuizResult> CreateRandomQuizAsync(CreateRandomQuizRequest request)
        {
            var normalizedCategory = request.Category?.Trim() ?? string.Empty;
            var normalizedTitle = request.Title?.Trim() ?? string.Empty;

            if (request.NodeId <= 0)
            {
                return FailCreate("Vui lòng chọn bài học hợp lệ.");
            }

            if (request.ClassId <= 0)
            {
                return FailCreate("Vui lòng chọn lớp học hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(normalizedTitle))
            {
                return FailCreate("Vui lòng nhập tên bài test.");
            }

            if (string.IsNullOrWhiteSpace(normalizedCategory))
            {
                return FailCreate("Vui lòng chọn phân loại kiến thức.");
            }

            if (request.QuestionCount <= 0)
            {
                return FailCreate("Số lượng câu hỏi phải lớn hơn 0.");
            }

            if (request.DurationMinutes <= 0)
            {
                return FailCreate("Thời gian làm bài phải lớn hơn 0 phút.");
            }

            var nodeExists = await _context.Nodes.AnyAsync(n => n.Id == request.NodeId && n.IsActive != false);
            if (!nodeExists)
            {
                return FailCreate("Không tìm thấy bài học được chọn.");
            }

            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists)
            {
                return FailCreate("Không tìm thấy lớp học được chọn.");
            }

            var availableQuestionCount = await CountAvailableQuestionsAsync(request.NodeId, normalizedCategory);
            if (availableQuestionCount < request.QuestionCount)
            {
                return new CreateRandomQuizResult
                {
                    Success = false,
                    Message = $"Kho câu hỏi chỉ có {availableQuestionCount} câu phù hợp, không đủ để tạo {request.QuestionCount} câu.",
                    AvailableQuestionCount = availableQuestionCount
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var selectedQuestions = await BuildEligibleQuestionQuery(request.NodeId, normalizedCategory)
                    .OrderBy(q => Guid.NewGuid())
                    .Take(request.QuestionCount)
                    .Select(q => q.Id)
                    .ToListAsync();

                if (selectedQuestions.Count < request.QuestionCount)
                {
                    await transaction.RollbackAsync();
                    return new CreateRandomQuizResult
                    {
                        Success = false,
                        Message = "Không lấy đủ câu hỏi phù hợp. Vui lòng thử lại.",
                        AvailableQuestionCount = selectedQuestions.Count
                    };
                }

                var quiz = new Quiz
                {
                    NodeId = request.NodeId,
                    ClassId = request.ClassId,
                    Title = normalizedTitle,
                    DurationMinutes = request.DurationMinutes,
                    Status = request.PublishNow ? PublishedStatus : DraftStatus,
                    PublishedAt = request.PublishNow ? DateTime.Now : null,
                    CreatedAt = DateTime.Now
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                var quizQuestions = selectedQuestions
                    .Select((questionId, index) => new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        QuestionId = questionId,
                        Point = 1m,
                        DisplayOrder = index + 1
                    })
                    .ToList();

                _context.QuizQuestions.AddRange(quizQuestions);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CreateRandomQuizResult
                {
                    Success = true,
                    Message = request.PublishNow
                        ? "Đã tạo và phát hành bài test thành công."
                        : "Đã tạo bài test ở trạng thái nháp.",
                    QuizId = quiz.Id,
                    AvailableQuestionCount = availableQuestionCount
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return FailCreate($"Đã xảy ra lỗi khi tạo bài test: {ex.Message}");
            }
        }

        public async Task<SubmitQuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int> selectedOptionIds)
        {
            var existingResult = await _context.QuizResults
                .FirstOrDefaultAsync(qr => qr.QuizId == quizId && qr.StudentId == studentId);

            if (existingResult != null)
            {
                return new SubmitQuizResult
                {
                    Success = true,
                    Message = "Bạn đã nộp bài test này trước đó.",
                    QuizResultId = existingResult.Id,
                    Score = existingResult.Score
                };
            }

            var quiz = await GetPublishedQuizForStudentAsync(quizId, studentId);
            if (quiz == null)
            {
                return FailSubmit("Không tìm thấy bài test hoặc bạn không có quyền làm bài này.");
            }

            var quizQuestions = quiz.QuizQuestions
                .OrderBy(qq => qq.DisplayOrder)
                .ToList();

            if (!quizQuestions.Any())
            {
                return FailSubmit("Bài test chưa có câu hỏi.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal score = 0;
                var correctAnswers = 0;

                var quizResult = new QuizResult
                {
                    QuizId = quiz.Id,
                    StudentId = studentId,
                    Score = 0,
                    CompletedAt = DateTime.Now
                };

                _context.QuizResults.Add(quizResult);
                await _context.SaveChangesAsync();

                foreach (var quizQuestion in quizQuestions)
                {
                    var question = quizQuestion.Question;
                    selectedOptionIds.TryGetValue(question.Id, out var selectedOptionId);

                    var selectedOption = question.QuestionOptions.FirstOrDefault(o => o.Id == selectedOptionId);
                    var isCorrect = selectedOption?.IsCorrect == true;
                    var pointEarned = isCorrect ? quizQuestion.Point ?? 1m : 0m;

                    if (isCorrect)
                    {
                        correctAnswers++;
                        score += pointEarned;
                    }
                    else
                    {
                        await UpsertStudentMistakeAsync(studentId, question.Id, question.Category);
                    }

                    _context.QuizAnswers.Add(new QuizAnswer
                    {
                        QuizResultId = quizResult.Id,
                        QuestionId = question.Id,
                        SelectedOptionId = selectedOption?.Id,
                        AnswerText = selectedOption?.OptionContent,
                        IsCorrect = isCorrect,
                        PointEarned = pointEarned,
                        CreatedAt = DateTime.Now
                    });
                }

                quizResult.Score = score;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new SubmitQuizResult
                {
                    Success = true,
                    Message = "Đã nộp bài và chấm điểm thành công.",
                    QuizResultId = quizResult.Id,
                    Score = score,
                    TotalQuestions = quizQuestions.Count,
                    CorrectAnswers = correctAnswers
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return FailSubmit($"Đã xảy ra lỗi khi nộp bài: {ex.Message}");
            }
        }

        public async Task<List<StudentMistake>> GetDailyReviewMistakesAsync(int studentId, int questionCount)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await HasCompletedDailyReviewAsync(studentId, today))
            {
                return new List<StudentMistake>();
            }

            return await _context.StudentMistakes
                .Include(sm => sm.Question)
                    .ThenInclude(q => q.QuestionOptions)
                .Include(sm => sm.Question)
                    .ThenInclude(q => q.Node)
                        .ThenInclude(n => n.Class)
                .Where(sm => sm.StudentId == studentId
                    && (sm.ErrorCount ?? 0) > 0
                    && (sm.NextReviewDate == null || sm.NextReviewDate <= today))
                .OrderByDescending(sm => sm.ErrorCount ?? 0)
                .ThenBy(sm => sm.NextReviewDate ?? DateOnly.MinValue)
                .ThenBy(sm => sm.CreatedAt)
                .Take(DailyReviewQuestionCount)
                .ToListAsync();
        }

        public async Task<bool> IsDailyReviewRequiredAsync(int studentId)
        {
            if (DateTime.Now.Hour < 20)
            {
                return false;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await HasCompletedDailyReviewAsync(studentId, today))
            {
                return false;
            }

            return await _context.StudentMistakes
                .AnyAsync(sm => sm.StudentId == studentId
                    && (sm.ErrorCount ?? 0) > 0
                    && (sm.NextReviewDate == null || sm.NextReviewDate <= today));
        }

        public async Task<DailyReviewSubmitResult> SubmitDailyReviewAsync(int studentId, Dictionary<int, int> selectedOptionIds)
        {
            if (selectedOptionIds.Count == 0)
            {
                return new DailyReviewSubmitResult
                {
                    Success = false,
                    Message = "Chưa có câu trả lời nào để chấm."
                };
            }

            var questionIds = selectedOptionIds.Keys.ToList();
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (await HasCompletedDailyReviewAsync(studentId, today))
            {
                return new DailyReviewSubmitResult
                {
                    Success = true,
                    Message = "Bạn đã hoàn thành Daily Review hôm nay."
                };
            }

            var allowedQuestionIds = await _context.StudentMistakes
                .Where(sm => sm.StudentId == studentId
                    && (sm.ErrorCount ?? 0) > 0
                    && (sm.NextReviewDate == null || sm.NextReviewDate <= today))
                .OrderByDescending(sm => sm.ErrorCount ?? 0)
                .ThenBy(sm => sm.NextReviewDate ?? DateOnly.MinValue)
                .ThenBy(sm => sm.CreatedAt)
                .Take(DailyReviewQuestionCount)
                .Select(sm => sm.QuestionId)
                .ToListAsync();

            questionIds = questionIds
                .Where(allowedQuestionIds.Contains)
                .ToList();

            var mistakes = await _context.StudentMistakes
                .Include(sm => sm.Question)
                    .ThenInclude(q => q.QuestionOptions)
                .Where(sm => sm.StudentId == studentId && questionIds.Contains(sm.QuestionId))
                .ToListAsync();

            if (!mistakes.Any())
            {
                return new DailyReviewSubmitResult
                {
                    Success = false,
                    Message = "Không tìm thấy câu ôn tập phù hợp."
                };
            }

            var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var reviewedCount = 0;
            var correctCount = 0;
            var masteredCount = 0;

            foreach (var mistake in mistakes)
            {
                if (!selectedOptionIds.TryGetValue(mistake.QuestionId, out var selectedOptionId))
                {
                    continue;
                }

                reviewedCount++;
                var selectedOption = mistake.Question.QuestionOptions.FirstOrDefault(o => o.Id == selectedOptionId);
                var isCorrect = selectedOption?.IsCorrect == true;

                if (isCorrect)
                {
                    correctCount++;
                    mistake.ErrorCount = Math.Max((mistake.ErrorCount ?? 1) - 1, 0);

                    if (mistake.ErrorCount <= 0)
                    {
                        masteredCount++;
                        _context.StudentMistakes.Remove(mistake);
                    }
                    else
                    {
                        mistake.NextReviewDate = tomorrow;
                    }
                }
                else
                {
                    mistake.ErrorCount = (mistake.ErrorCount ?? 0) + 1;
                    mistake.NextReviewDate = tomorrow;
                }
            }

            _context.DailyReviewLogs.Add(new DailyReviewLog
            {
                StudentId = studentId,
                ReviewDate = today,
                ReviewedCount = reviewedCount,
                CorrectCount = correctCount,
                MasteredCount = masteredCount,
                CompletedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return new DailyReviewSubmitResult
            {
                Success = true,
                Message = "Đã chấm Daily Review và cập nhật lịch ôn tập.",
                ReviewedCount = reviewedCount,
                CorrectCount = correctCount,
                MasteredCount = masteredCount
            };
        }

        public async Task<List<QuestionMistakeStatistic>> GetMistakeStatisticsAsync(int? classId)
        {
            var query = _context.StudentMistakes
                .Include(sm => sm.Question)
                    .ThenInclude(q => q.Node)
                        .ThenInclude(n => n.Class)
                .AsQueryable();

            if (classId.HasValue)
            {
                query = query.Where(sm => sm.Question.Node.ClassId == classId.Value);
            }

            var rows = await query
                .GroupBy(sm => new
                {
                    sm.QuestionId,
                    sm.Question.Content,
                    sm.Question.Category,
                    NodeTitle = sm.Question.Node.Title,
                    ClassName = sm.Question.Node.Class.ClassName,
                    sm.Question.Node.ClassId
                })
                .Select(g => new
                {
                    g.Key.QuestionId,
                    g.Key.Content,
                    g.Key.Category,
                    g.Key.NodeTitle,
                    g.Key.ClassName,
                    g.Key.ClassId,
                    WrongStudentCount = g.Select(sm => sm.StudentId).Distinct().Count(),
                    TotalMistakeCount = g.Sum(sm => sm.ErrorCount ?? 0)
                })
                .OrderByDescending(x => x.WrongStudentCount)
                .ThenByDescending(x => x.TotalMistakeCount)
                .Take(50)
                .ToListAsync();

            var classStudentCounts = await _context.ClassMembers
                .Where(cm => !classId.HasValue || cm.ClassId == classId.Value)
                .GroupBy(cm => cm.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Select(cm => cm.UserId).Distinct().Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);

            return rows.Select(row =>
            {
                classStudentCounts.TryGetValue(row.ClassId ?? 0, out var classStudentCount);
                var percent = classStudentCount == 0
                    ? 0
                    : Math.Round(row.WrongStudentCount * 100m / classStudentCount, 2);

                return new QuestionMistakeStatistic
                {
                    QuestionId = row.QuestionId,
                    QuestionContent = row.Content,
                    Category = row.Category,
                    NodeTitle = row.NodeTitle,
                    ClassName = row.ClassName,
                    WrongStudentCount = row.WrongStudentCount,
                    TotalMistakeCount = row.TotalMistakeCount,
                    ClassStudentCount = classStudentCount,
                    WrongStudentPercent = percent
                };
            }).ToList();
        }

        public async Task<QuestionMistakeDetail?> GetQuestionMistakeDetailAsync(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.QuestionOptions)
                .Include(q => q.Node)
                    .ThenInclude(n => n.Class)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return null;
            }

            var mistakes = await _context.StudentMistakes
                .Include(sm => sm.Student)
                .Where(sm => sm.QuestionId == questionId)
                .OrderByDescending(sm => sm.ErrorCount)
                .ThenBy(sm => sm.Student.LastName)
                .ThenBy(sm => sm.Student.FirstName)
                .ToListAsync();

            var classStudentCount = await _context.ClassMembers
                .Where(cm => cm.ClassId == question.Node.ClassId)
                .Select(cm => cm.UserId)
                .Distinct()
                .CountAsync();

            var wrongStudentCount = mistakes.Select(sm => sm.StudentId).Distinct().Count();
            var totalMistakeCount = mistakes.Sum(sm => sm.ErrorCount ?? 0);
            var percent = classStudentCount == 0
                ? 0
                : Math.Round(wrongStudentCount * 100m / classStudentCount, 2);

            return new QuestionMistakeDetail
            {
                QuestionId = question.Id,
                QuestionContent = question.Content,
                QuestionType = question.QuestionType,
                Category = question.Category,
                CorrectAnswer = question.CorrectAnswer,
                Explanation = question.Explanation,
                NodeTitle = question.Node.Title,
                ClassName = question.Node.Class.ClassName,
                ClassId = (int)question.Node.ClassId,
                WrongStudentCount = wrongStudentCount,
                TotalMistakeCount = totalMistakeCount,
                ClassStudentCount = classStudentCount,
                WrongStudentPercent = percent,
                Options = question.QuestionOptions
                    .OrderBy(o => o.Id)
                    .Select(o => new QuestionOptionDetail
                    {
                        Id = o.Id,
                        OptionContent = o.OptionContent,
                        IsCorrect = o.IsCorrect
                    })
                    .ToList(),
                Students = mistakes.Select(sm => new StudentMistakeEntry
                {
                    StudentId = sm.StudentId,
                    StudentName = $"{sm.Student.LastName} {sm.Student.FirstName}".Trim(),
                    ErrorCount = sm.ErrorCount ?? 0,
                    MistakeType = sm.MistakeType,
                    NextReviewDate = sm.NextReviewDate
                }).ToList()
            };
        }

        private async Task UpsertStudentMistakeAsync(int studentId, int questionId, string category)
        {
            var existingMistake = await _context.StudentMistakes
                .FirstOrDefaultAsync(sm => sm.StudentId == studentId && sm.QuestionId == questionId);

            if (existingMistake == null)
            {
                _context.StudentMistakes.Add(new StudentMistake
                {
                    StudentId = studentId,
                    QuestionId = questionId,
                    ErrorCount = 1,
                    MistakeType = category,
                    NextReviewDate = DateOnly.FromDateTime(DateTime.Today),
                    CreatedAt = DateTime.Now
                });
                return;
            }

            existingMistake.ErrorCount = (existingMistake.ErrorCount ?? 0) + 1;
            existingMistake.MistakeType = category;
            existingMistake.NextReviewDate = DateOnly.FromDateTime(DateTime.Today);
        }

        private async Task<bool> HasCompletedDailyReviewAsync(int studentId, DateOnly reviewDate)
        {
            return await _context.DailyReviewLogs
                .AnyAsync(log => log.StudentId == studentId && log.ReviewDate == reviewDate);
        }

        private IQueryable<Question> BuildEligibleQuestionQuery(int nodeId, string category)
        {
            var normalizedCategory = category.Trim();

            var query = _context.Questions
                .Where(q => q.NodeId == nodeId
                    && q.IsQuestionBank == true
                    && q.IsDeleted == false);

            if (!IsAllCategory(normalizedCategory))
            {
                query = query.Where(q => q.Category == normalizedCategory);
            }

            return query;
        }

        private static bool IsAllCategory(string category)
        {
            return string.Equals(category, AllCategories, StringComparison.OrdinalIgnoreCase);
        }

        private static CreateRandomQuizResult FailCreate(string message)
        {
            return new CreateRandomQuizResult
            {
                Success = false,
                Message = message
            };
        }

        private static SubmitQuizResult FailSubmit(string message)
        {
            return new SubmitQuizResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
