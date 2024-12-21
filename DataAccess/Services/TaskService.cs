using DataAccess.Data;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;


namespace DataAccess.Services
{
    public interface ITaskService<T> where T : CustomTask
    {
        Task AddTaskAsync(T task);
        Task<T> GetTaskAsync(int taskId);
        Task<List<T>> GetAllTasksAsync();
        Task UpdateTaskAsync(T task);
        Task DeleteTaskAsync(int taskId);
        Task<List<string>> GetOptionsAsync(int taskId);

        Task<List<T>> GetRandomTasksAsync(int count);

        Task<List<T>> GetRandomTasksAsync(int count, string topic);
    }

    public class TaskService<T> : ITaskService<T> where T : CustomTask
    {
        private readonly AppDbContext _context;
        private readonly Random _random;

        public TaskService(AppDbContext context)
        {
            _context = context;
            _random = new Random();
        }

        public async Task AddTaskAsync(T task)
        {
            _context.CustomTasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task<T> GetTaskAsync(int taskId)
        {
            return await _context.CustomTasks
                .OfType<T>() // Filters to the specific derived type
                .Include(ct => ct.AnswerOptions)
                .FirstOrDefaultAsync(ct => ct.Id == taskId);
        }

        public async Task<List<T>> GetAllTasksAsync()
        {
            return await _context.CustomTasks
                .OfType<T>()
                .Include(ct => ct.AnswerOptions)
                .ToListAsync();
        }

        public async Task<List<T>> GetRandomTasksAsync(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero.", nameof(count));

            var allTaskIds = await _context.CustomTasks
                                           .OfType<T>()
                                           .Select(ct => ct.Id)
                                           .ToListAsync();

            if (allTaskIds.Count == 0)
                return new List<T>(); 

            int fetchCount = Math.Min(count, allTaskIds.Count);

            var selectedIds = GetRandomSubset(allTaskIds, fetchCount);

            var randomTasks = await _context.CustomTasks
                                            .OfType<T>()
                                            .Where(ct => selectedIds.Contains(ct.Id))
                                            .Include(ct => ct.AnswerOptions) // Include related data if necessary
                                            .AsNoTracking() 
                                            .ToListAsync();

            var orderedRandomTasks = selectedIds.Select(id => randomTasks.FirstOrDefault(t => t.Id == id))
                                                .Where(t => t != null)
                                                .ToList();

            return orderedRandomTasks;
        }

        public async Task<List<T>> GetRandomTasksAsync(int count, string topic)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero.", nameof(count));

            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("Topic must be provided.", nameof(topic));

            // Fetch task IDs filtered by topic
            var filteredTaskIds = await _context.CustomTasks
                                                .OfType<T>()
                                                .Where(ct => ct.Topic == topic)
                                                .Select(ct => ct.Id)
                                                .ToListAsync();

            if (!filteredTaskIds.Any())
                return new List<T>(); 

            int fetchCount = Math.Min(count, filteredTaskIds.Count);

            var selectedIds = GetRandomSubset(filteredTaskIds, fetchCount);

            var randomTasks = await _context.CustomTasks
                                            .OfType<T>()
                                            .Where(ct => selectedIds.Contains(ct.Id))
                                            .Include(ct => ct.AnswerOptions)
                                            .AsNoTracking()
                                            .ToListAsync();

            
            var orderedRandomTasks = selectedIds.Select(id => randomTasks.FirstOrDefault(t => t.Id == id))
                                                .Where(t => t != null)
                                                .ToList();

            return orderedRandomTasks;
        }

        private List<int> GetRandomSubset(List<int> source, int count)
        {
            int n = source.Count;
            for (int i = 0; i < n; i++)
            {
                int j = _random.Next(i, n);
                int temp = source[i];
                source[i] = source[j];
                source[j] = temp;
            }

            return source.Take(count).ToList();
        }



        public async Task UpdateTaskAsync(T task)
        {
            var existingTask = await _context.CustomTasks
                .OfType<T>()
                .Include(ct => ct.AnswerOptions)
                .FirstOrDefaultAsync(ct => ct.Id == task.Id);

            if (existingTask != null)
            {
                existingTask.Sentence = task.Sentence;
                existingTask.UserText = task.UserText;
                existingTask.CorrectAnswer = task.CorrectAnswer;
                existingTask.Explanation = task.Explanation;
                existingTask.TaskStatus = task.TaskStatus;
                existingTask.Topic = task.Topic;

                // Set new options via Options property, which handles AnswerOptions
                existingTask.Options = task.Options;

                if (existingTask is PunctuationTask punctuationTask && task is PunctuationTask updatedPunctuationTask)
                {
                    punctuationTask.Highlights = updatedPunctuationTask.Highlights;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            var task = await _context.CustomTasks
                .OfType<T>()
                .FirstOrDefaultAsync(ct => ct.Id == taskId);

            if (task != null)
            {
                _context.CustomTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetOptionsAsync(int taskId)
        {
            var task = await GetTaskAsync(taskId);
            return task?.Options ?? new List<string>();
        }

    }
}

