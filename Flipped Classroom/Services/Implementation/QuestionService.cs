using System;
using System.Linq;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Services.Implementation
{
    public class QuestionService : IQuestionService
    {
        private readonly Swp391NihongoContext _context;

        public QuestionService(Swp391NihongoContext context)
        {
            _context = context;
        }

        //Add a question to the question bank, and if it's a multiple-choice question, also add the options to the QuestionOptions table. 
        public async Task<bool> CreateQuestionAsync(Question question, List<QuestionOption> questionOption)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                question.IsQuestionBank = true;
                _context.Questions.Add(question);

                await _context.SaveChangesAsync();

                if (questionOption != null && questionOption.Any())
                {
                    foreach (var option in questionOption)
                    {
                        option.QuestionId = question.Id;
                        _context.QuestionOptions.Add(option);
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error creating question: {ex.Message}");
                return false;
            }
        }


        // Using naming tuple to return both the list of questions and the total number of pages for pagination
        public async Task<(List<Question> questions, int totalPages)> getQuestionAsync(
            string searchKeyword, string questionType, string category, int pageIndex, int pageSize)
        {
            var query = _context.Questions
                .Include(q => q.QuestionOptions)
                .Where(q => q.IsQuestionBank == true) // Chỉ lấy các câu trong ngân hàng 
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(q => q.Content.Contains(searchKeyword));
            }
            if (!string.IsNullOrWhiteSpace(questionType))
            {
                query = query.Where(q => q.QuestionType == questionType);
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(q => q.Category == category);
            }

            // Pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(q => q.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalPages);
        }

        // Get a question by its ID, including the related options if it's a multiple-choice question.
        public async Task<Question> GetQuestionByIdAsync(int questionId)
        {
            return await _context.Questions
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        // Delete a question from the question bank, and if it's a multiple-choice question, also delete the related options from the QuestionOptions table.
        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null)
                {
                    return false; // Question not found
                }
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting question: {ex.Message}");
                return false;
            }
        }


        // Update a question in the question bank, and if it's a multiple-choice question, also update the related options in the QuestionOptions table.
        public async Task<bool> UpdateQuestionAsync(Question question, List<QuestionOption> questionOptions)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingQuestion = await _context.Questions
                    .Include(q => q.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == question.Id);
                if (existingQuestion == null)
                {
                    return false;
                }
                // Update question properties
                existingQuestion.Content = question.Content;
                existingQuestion.QuestionType = question.QuestionType;
                existingQuestion.Category = question.Category;
                // Update options
                if (questionOptions != null && questionOptions.Any())
                {
                    // Remove existing options
                    _context.QuestionOptions.RemoveRange(existingQuestion.QuestionOptions);
                    // Add new options
                    foreach (var option in questionOptions)
                    {
                        option.QuestionId = existingQuestion.Id;
                        _context.QuestionOptions.Add(option);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error updating question: {ex.Message}");
                return false;
            }
        }
    }
}
