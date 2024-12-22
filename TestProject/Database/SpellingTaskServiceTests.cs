// File: SpellingTaskServiceTests.cs
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
    public class SpellingTaskServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly ITaskService<SpellingTask> _taskService;
        private readonly DatabaseFixture _fixture;

        public SpellingTaskServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _taskService = _fixture.ServiceProvider.GetRequiredService<ITaskService<SpellingTask>>();
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
        public async Task AddAndRetrieveSpellingTask()
        {
            // Arrange
            var task = new SpellingTask
            {
                Sentence = "Rašyk teisingai",
                UserText = "Rašyk teisingai",
                CorrectAnswer = "Rašyk teisingai.",
                Explanation = "A period is needed at the end of the sentence.",
                TaskStatus = false,
                Topic = "Basic Spelling",
                Options = CreateAnswerOptions(".", "!", "?") // Initialize options
            };

            // Act
            await _taskService.AddTaskAsync(task);
            var retrievedTask = await _taskService.GetTaskAsync(task.Id);

            // Assert
            Assert.NotNull(retrievedTask);
            Assert.Equal("Rašyk teisingai", retrievedTask.UserText);
            Assert.Equal("Rašyk teisingai.", retrievedTask.CorrectAnswer);
            Assert.Equal(3, retrievedTask.AnswerOptions.Count);
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == ".");
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == "!");
            Assert.Contains(retrievedTask.AnswerOptions, o => o.OptionText == "?");
        }

        [Fact]
        public async Task UpdateSpellingTaskOptions()
        {
            // Arrange
            var task = new SpellingTask
            {
                Sentence = "Taisyti klaidas",
                UserText = "Taisyti klaidas",
                CorrectAnswer = "Taisyti klaidas.",
                Explanation = "A period is needed at the end of the sentence.",
                TaskStatus = false,
                Topic = "Advanced Spelling",
                Options = CreateAnswerOptions(",", ".", "!") // Initialize options
            };
            await _taskService.AddTaskAsync(task);

            // Act
            var newOptions = CreateAnswerOptions(":", "?", "-");
            task.CorrectAnswer = "Pasirinkumu pataisymas!";
            task.Options = newOptions; // Update options
            await _taskService.UpdateTaskAsync(task);
            var updatedTask = await _taskService.GetTaskAsync(task.Id);

            // Assert
            Assert.NotNull(updatedTask);
            Assert.Equal("Pasirinkumu pataisymas!", updatedTask.CorrectAnswer);
            Assert.Equal(3, updatedTask.AnswerOptions.Count);
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == ":");
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == "?");
            Assert.Contains(updatedTask.AnswerOptions, o => o.OptionText == "-");
            Assert.DoesNotContain(updatedTask.AnswerOptions, o => o.OptionText == ",");
            Assert.DoesNotContain(updatedTask.AnswerOptions, o => o.OptionText == ".");
            Assert.DoesNotContain(updatedTask.AnswerOptions, o => o.OptionText == "!");
        }

        [Fact]
        public async Task DeleteSpellingTask()
        {
            // Arrange
            var task = new SpellingTask
            {
                Sentence = "Rašyk teisingai",
                UserText = "Rašyk teisingai",
                CorrectAnswer = "Rašyk teisingai.",
                Explanation = "A period is needed at the end of the sentence.",
                TaskStatus = false,
                Topic = "Basic Spelling",
                Options = CreateAnswerOptions(".", "!", "?") // Initialize options
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
        public async Task GetAllSpellingTasks_ReturnsAllTasks()
        {
            // Arrange
            var tasks = new List<SpellingTask>
            {
                new SpellingTask
                {
                    Sentence = "Rašyk teisingai",
                    UserText = "Rašyk teisingai",
                    CorrectAnswer = "Rašyk teisingai.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Basic Spelling",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new SpellingTask
                {
                    Sentence = "Taisyti klaidas",
                    UserText = "Taisyti klaidas",
                    CorrectAnswer = "Taisyti klaidas.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
                    Options = CreateAnswerOptions(".", "?", "!")
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
            Assert.Contains(allTasks, t => t.Sentence == "Rašyk teisingai");
            Assert.Contains(allTasks, t => t.Sentence == "Taisyti klaidas");
        }

        [Fact]
        public async Task GetOptionsAsync_ReturnsCorrectOptions()
        {
            // Arrange
            var task = new SpellingTask
            {
                Sentence = "Rašyk teisingai",
                UserText = "Rašyk teisingai",
                CorrectAnswer = "Rašyk teisingai.",
                Explanation = "A period is needed.",
                TaskStatus = false,
                Topic = "Basic Spelling",
                Options = CreateAnswerOptions(".", "!", "?") // Initialize options
            };
            await _taskService.AddTaskAsync(task);

            // Act
            var options = await _taskService.GetOptionsAsync(task.Id);

            // Assert
            Assert.NotNull(options);
            Assert.Equal(3, options.Count);
            Assert.Contains(".", options);
            Assert.Contains("!", options);
            Assert.Contains("?", options);
        }

        [Fact]
        public async Task GetRandomSpellingTasksAsync_ReturnsRandomTasks()
        {
            // Arrange
            var tasks = new List<SpellingTask>
            {
                new SpellingTask
                {
                    Sentence = "Rašyk teisingai",
                    UserText = "Rašyk teisingai",
                    CorrectAnswer = "Rašyk teisingai.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Basic Spelling",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new SpellingTask
                {
                    Sentence = "Taisyti klaidas",
                    UserText = "Taisyti klaidas",
                    CorrectAnswer = "Taisyti klaidas.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
                    Options = CreateAnswerOptions(".", "?", "!")
                },
                new SpellingTask
                {
                    Sentence = "Rašyk neteisingai",
                    UserText = "Rašyk neteisingai",
                    CorrectAnswer = "Rašyk neteisingai!",
                    Explanation = "An exclamation mark is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
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
        public async Task GetRandomSpellingTasksAsync_WithTopic_ReturnsFilteredRandomTasks()
        {
            // Arrange
            var tasks = new List<SpellingTask>
            {
                new SpellingTask
                {
                    Sentence = "Rašyk teisingai",
                    UserText = "Rašyk teisingai",
                    CorrectAnswer = "Rašyk teisingai.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Basic Spelling",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new SpellingTask
                {
                    Sentence = "Taisyti klaidas",
                    UserText = "Taisyti klaidas",
                    CorrectAnswer = "Taisyti klaidas.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
                    Options = CreateAnswerOptions(".", "?", "!")
                },
                new SpellingTask
                {
                    Sentence = "Rašyk neteisingai",
                    UserText = "Rašyk neteisingai",
                    CorrectAnswer = "Rašyk neteisingai!",
                    Explanation = "An exclamation mark is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
                    Options = CreateAnswerOptions("!", ".", "?")
                }
            };

            foreach (var t in tasks)
            {
                await _taskService.AddTaskAsync(t);
            }

            // Act
            var randomTasks = await _taskService.GetRandomTasksAsync(1, "Advanced Spelling");

            // Assert
            Assert.Single(randomTasks);
            Assert.Equal("Advanced Spelling", randomTasks.First().Topic);
        }

        [Fact]
        public async Task GetRandomSpellingTasksAsync_WithInvalidCount_ReturnsAllAvailableTasks()
        {
            // Arrange
            var tasks = new List<SpellingTask>
            {
                new SpellingTask
                {
                    Sentence = "Rašyk teisingai",
                    UserText = "Rašyk teisingai",
                    CorrectAnswer = "Rašyk teisingai.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Basic Spelling",
                    Options = CreateAnswerOptions(".", "!", "?")
                },
                new SpellingTask
                {
                    Sentence = "Taisyti klaidas",
                    UserText = "Taisyti klaidas",
                    CorrectAnswer = "Taisyti klaidas.",
                    Explanation = "A period is needed.",
                    TaskStatus = false,
                    Topic = "Advanced Spelling",
                    Options = CreateAnswerOptions(".", "?", "!")
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
            Assert.Contains(randomTasks, t => t.Sentence == "Rašyk teisingai");
            Assert.Contains(randomTasks, t => t.Sentence == "Taisyti klaidas");
        }

        [Fact]
        public async Task GetRandomSpellingTasksAsync_WithInvalidTopic_ReturnsEmptyList()
        {
            // Arrange
            var task = new SpellingTask
            {
                Sentence = "Rašyk teisingai",
                UserText = "Rašyk teisingai",
                CorrectAnswer = "Rašyk teisingai.",
                Explanation = "A period is needed.",
                TaskStatus = false,
                Topic = "Basic Spelling",
                Options = CreateAnswerOptions(".", "!", "?")
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
        public async Task UpdateSpellingTask_NonExistentTask_DoesNothing()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var task = new SpellingTask
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
        public async Task DeleteSpellingTask_NonExistentTask_DoesNothing()
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
