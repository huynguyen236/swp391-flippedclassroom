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

                if (!await EnsureQuestionNodeIdAsync(question))
                {
                    return false;
                }

                if (string.Equals(question.QuestionType, "MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    question.CorrectAnswer = null;
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

                    if (questionOption != null)
                    {
                        foreach (var option in questionOption)
                        {
                            option.QuestionId = question.Id;
                            _context.QuestionOptions.Add(option);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Questions.Add(question);
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

      
        // Question bank items require FK_Question_Node. Form does not set NodeId, so resolve a valid node.
        private async Task<bool> EnsureQuestionNodeIdAsync(Question question)
        {
            // If NodeId is already set and valid, use it
            if (question.NodeId > 0 && await _context.Nodes.AnyAsync(n => n.Id == question.NodeId))
                return true;

            // If no valid NodeId is set, this method will return false
            // The form should handle setting NodeId before calling CreateQuestion
            return false;
        }


        // Using naming tuple to return both the list of questions and the total number of pages for pagination
        public async Task<(List<Question> questions, int totalPages)> getQuestionAsync(
            string searchKeyword, string questionType, string category, int pageIndex, int pageSize)
        {
            var query = _context.Questions
                .Include(q => q.QuestionOptions)
                .Where(q => q.IsQuestionBank == true && q.IsDeleted == false) // Only include questions that are in the question bank and not deleted
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

        
        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null)
                {
                    Console.WriteLine($"Question with ID {questionId} not found.");
                    return false;
                }
                // Soft delete: Mark the question as deleted instead of removing it from the database
                question.IsDeleted = true;
                _context.Questions.Update(question);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
                existingQuestion.Content = question.Content;
                existingQuestion.QuestionType = question.QuestionType;
                existingQuestion.Category = question.Category;

                if (string.Equals(question.QuestionType, "MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    existingQuestion.CorrectAnswer = null;
                    _context.QuestionOptions.RemoveRange(existingQuestion.QuestionOptions);
                    if (questionOptions != null)
                    {
                        foreach (var option in questionOptions)
                        {
                            option.QuestionId = existingQuestion.Id;
                            _context.QuestionOptions.Add(option);
                        }
                    }
                }
                else
                {
                    existingQuestion.CorrectAnswer = question.CorrectAnswer;
                    if (existingQuestion.QuestionOptions.Count > 0)
                    {
                        _context.QuestionOptions.RemoveRange(existingQuestion.QuestionOptions);
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
