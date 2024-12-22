// File: PunctuationTaskServiceTests.cs
using DataAccess.Models;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestProject.Database;
using Xunit;

namespace TestProject.Services
{
    public class PunctuationTaskServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly ITaskService<PunctuationTask> _taskService;
        private readonly DatabaseFixture _fixture;

        public PunctuationTaskServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _taskService = _fixture.ServiceProvider.GetRequiredService<ITaskService<PunctuationTask>>();
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // Helper method to create AnswerOptions
        private List<string> CreateAnswerOptions(params string[] options)
        {
            return options.ToList();
        }

        [Fact]
        public async Task AddAndRetrievePunctuationTask()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Sveiki kaip sekasi",
                UserText = "Sveiki kaip sekasi",
                CorrectAnswer = "Sveiki, kaip sekasi?",
                Explanation = "A comma is needed after 'Sveiki' and a question mark at the end.",
                TaskStatus = false,
                Topic = "Basic Greetings",
                Options = CreateAnswerOptions(",", ".", "?") // Initialize options
            };

            // Act
            await _taskService.AddTaskAsync(task);
            var retrievedTask = await _taskService.GetTaskAsync(task.Id);

            // Assert
            Assert.NotNull(retrievedTask);
            Assert.Equal("Sveiki kaip sekasi", retrievedTask.UserText);
            Assert.Equal("Sveiki, kaip sekasi?", retrievedTask.CorrectAnswer);
            Assert.Equal(3, retrievedTask.AnswerOptions.Count);
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == ",");
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == ".");
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == "?");
        }

        [Fact]
        public async Task UpdatePunctuationTaskOptions()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Kaip tau sekasi",
                UserText = "Kaip tau sekasi",
                CorrectAnswer = "Kaip tau sekasi?",
                Explanation = "A question mark is needed at the end of the sentence.",
                TaskStatus = false,
                Topic = "Basic Questions",
                Options = CreateAnswerOptions("!", ".", "?") // Initialize options
            };
            await _taskService.AddTaskAsync(task);

            // Act
            var newOptions = CreateAnswerOptions("!!", "?", ":");
            task.CorrectAnswer = "Kaip tau sekasi!";
            task.Options = newOptions; // Update options
            await _taskService.UpdateTaskAsync(task);
            var updatedTask = await _taskService.GetTaskAsync(task.Id);

            // Assert
            Assert.NotNull(updatedTask);
            Assert.Equal("Kaip tau sekasi!", updatedTask.CorrectAnswer);
            Assert.Equal(3, updatedTask.AnswerOptions.Count);
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == "!!");
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == "?");
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == ":");
            Assert.DoesNotContain(updatedTask.AnswerOptions, o => o.OptionText == "!");
            Assert.DoesNotContain(updatedTask.AnswerOptions, o => o.OptionText == ".");
        }

        [Fact]
        public async Task DeletePunctuationTask()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Labas rytas",
                UserText = "Labas rytas",
                CorrectAnswer = "Labas rytas.",
                Explanation = "A period is needed at the end of the sentence.",
                TaskStatus = false,
                Topic = "Basic Greetings",
                Options = CreateAnswerOptions(",", ".", "!") // Initialize options
            };
            await _taskService.AddTaskAsync(task);
            var taskId = task.Id;

            // Act
            await _taskService.DeleteTaskAsync(taskId);
            var deletedTask = await _taskService.GetTaskAsync(taskId);

            // Assert
            Assert.Null(deletedTask);
        }

        [Fact]
        public async Task GetAllPunctuationTasks_ReturnsAllTasks()
        {
            // Arrange
            var tasks = new List<PunctuationTask>
            {
                new PunctuationTask
                {
                    Sentence = "Kaip sekasi",
                    UserText = "Kaip sekasi",
                    CorrectAnswer = "Kaip sekasi?",
                    Explanation = "Question mark needed.",
                    TaskStatus = false,
                    Topic = "Casual Conversations",
                    Options = CreateAnswerOptions("?", "!", ".")
                },
                new PunctuationTask
                {
                    Sentence = "Labas vakaras",
                    UserText = "Labas vakaras",
                    CorrectAnswer = "Labas vakaras.",
                    Explanation = "Period needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions(".", "!", "?")
                }
            };

            foreach (var t in tasks)
            {
                await _taskService.AddTaskAsync(t);
            }

            // Act
            var allTasks = await _taskService.GetAllTasksAsync();

            // Assert
            Assert.Equal(2, allTasks.Count);
            Assert.Contains(allTasks, t => t.Sentence == "Kaip sekasi");
            Assert.Contains(allTasks, t => t.Sentence == "Labas vakaras");
        }

        [Fact]
        public async Task GetOptionsAsync_ReturnsCorrectOptions()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Kaip sekasi",
                UserText = "Kaip sekasi",
                CorrectAnswer = "Kaip sekasi?",
                Explanation = "Question mark needed.",
                TaskStatus = false,
                Topic = "Casual Conversations",
                Options = CreateAnswerOptions("?", "!", ".")
            };
            await _taskService.AddTaskAsync(task);

            // Act
            var options = await _taskService.GetOptionsAsync(task.Id);

            // Assert
            Assert.NotNull(options);
            Assert.Equal(3, options.Count);
            Assert.Contains("?", options);
            Assert.Contains("!", options);
            Assert.Contains(".", options);
        }

        [Fact]
        public async Task GetRandomTasksAsync_ReturnsRandomTasks()
        {
            // Arrange
            var tasks = new List<PunctuationTask>
            {
                new PunctuationTask
                {
                    Sentence = "Kaip sekasi",
                    UserText = "Kaip sekasi",
                    CorrectAnswer = "Kaip sekasi?",
                    Explanation = "Question mark needed.",
                    TaskStatus = false,
                    Topic = "Casual Conversations",
                    Options = CreateAnswerOptions("?", "!", ".")
                },
                new PunctuationTask
                {
                    Sentence = "Labas rytas",
                    UserText = "Labas rytas",
                    CorrectAnswer = "Labas rytas.",
                    Explanation = "Period needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new PunctuationTask
                {
                    Sentence = "Sveiki",
                    UserText = "Sveiki",
                    CorrectAnswer = "Sveiki!",
                    Explanation = "Exclamation mark needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions("!", ".", "?")
                }
            };

            foreach (var t in tasks)
            {
                await _taskService.AddTaskAsync(t);
            }

            // Act
            var randomTasks = await _taskService.GetRandomTasksAsync(2);

            // Assert
            Assert.Equal(2, randomTasks.Count);

            // Verify that the random tasks are part of the original tasks based on Id
            Assert.All(randomTasks, rt =>
            {
                Assert.Contains(tasks, original => original.Id == rt.Id);
            });
        }

        [Fact]
        public async Task GetRandomTasksAsync_WithTopic_ReturnsFilteredRandomTasks()
        {
            // Arrange
            var tasks = new List<PunctuationTask>
            {
                new PunctuationTask
                {
                    Sentence = "Kaip sekasi",
                    UserText = "Kaip sekasi",
                    CorrectAnswer = "Kaip sekasi?",
                    Explanation = "Question mark needed.",
                    TaskStatus = false,
                    Topic = "Casual Conversations",
                    Options = CreateAnswerOptions("?", "!", ".")
                },
                new PunctuationTask
                {
                    Sentence = "Labas rytas",
                    UserText = "Labas rytas",
                    CorrectAnswer = "Labas rytas.",
                    Explanation = "Period needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new PunctuationTask
                {
                    Sentence = "Sveiki",
                    UserText = "Sveiki",
                    CorrectAnswer = "Sveiki!",
                    Explanation = "Exclamation mark needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions("!", ".", "?")
                }
            };

            foreach (var t in tasks)
            {
                await _taskService.AddTaskAsync(t);
            }

            // Act
            var randomTasks = await _taskService.GetRandomTasksAsync(2, "Basic Greetings");

            // Assert
            Assert.Equal(2, randomTasks.Count);
            Assert.All(randomTasks, t => Assert.Equal("Basic Greetings", t.Topic));
        }

        [Fact]
        public async Task GetRandomTasksAsync_WithInvalidCount_ReturnsAllAvailableTasks()
        {
            // Arrange
            var tasks = new List<PunctuationTask>
            {
                new PunctuationTask
                {
                    Sentence = "Kaip sekasi",
                    UserText = "Kaip sekasi",
                    CorrectAnswer = "Kaip sekasi?",
                    Explanation = "Question mark needed.",
                    TaskStatus = false,
                    Topic = "Casual Conversations",
                    Options = CreateAnswerOptions("?", "!", ".")
                },
                new PunctuationTask
                {
                    Sentence = "Labas rytas",
                    UserText = "Labas rytas",
                    CorrectAnswer = "Labas rytas.",
                    Explanation = "Period needed.",
                    TaskStatus = false,
                    Topic = "Basic Greetings",
                    Options = CreateAnswerOptions(".", "!", "?")
                }
            };

            foreach (var t in tasks)
            {
                await _taskService.AddTaskAsync(t);
            }

            // Act
            var randomTasks = await _taskService.GetRandomTasksAsync(5); // Requesting more tasks than available

            // Assert
            Assert.Equal(2, randomTasks.Count);
            Assert.Contains(randomTasks, t => t.Sentence == "Kaip sekasi");
            Assert.Contains(randomTasks, t => t.Sentence == "Labas rytas");
        }

        [Fact]
        public async Task GetRandomTasksAsync_WithInvalidTopic_ReturnsEmptyList()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Kaip sekasi",
                UserText = "Kaip sekasi",
                CorrectAnswer = "Kaip sekasi?",
                Explanation = "Question mark needed.",
                TaskStatus = false,
                Topic = "Casual Conversations",
                Options = CreateAnswerOptions("?", "!", ".")
            };
            await _taskService.AddTaskAsync(task);

            // Act
            var randomTasks = await _taskService.GetRandomTasksAsync(1, "Non-Existent Topic");

            // Assert
            Assert.Empty(randomTasks);
        }

        [Fact]
        public async Task GetTaskAsync_NonExistentTask_ReturnsNull()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            // Act
            var task = await _taskService.GetTaskAsync(999); // Assuming ID 999 does not exist

            // Assert
            Assert.Null(task);
        }

        [Fact]
        public async Task UpdatePunctuationTask_NonExistentTask_DoesNothing()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var task = new PunctuationTask
            {
                Id = 999, // Non-existent ID
                Sentence = "Neegzistuojantis sakinys",
                UserText = "Neegzistuojantis sakinys",
                CorrectAnswer = "Neegzistuojantis sakinys.",
                Explanation = "Period needed.",
                TaskStatus = false,
                Topic = "Testing",
                Options = CreateAnswerOptions(".", "!", "?")
            };

            // Act
            await _taskService.UpdateTaskAsync(task);
            var retrievedTask = await _taskService.GetTaskAsync(999);

            // Assert
            Assert.Null(retrievedTask);
        }

        [Fact]
        public async Task DeletePunctuationTask_NonExistentTask_DoesNothing()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            // Act
            await _taskService.DeleteTaskAsync(999); // Non-existent ID

            // Assert
            var task = await _taskService.GetTaskAsync(999);
            Assert.Null(task);
        }
    }
}
