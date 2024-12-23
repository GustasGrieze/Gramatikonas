using gramatikonas.Components.Pages;
using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Svg.Dom;
using Xunit;
using Bunit;
using DataAccess.Services;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;

namespace TestProject.Components
{
    public class TaskBaseTests : TestContext
    {
        private class TestableTaskBase : TaskBase<CustomTask>
        {
            public List<CustomTask> Tasks => base.tasks;
            public CustomTask CurrentTask => base.currentTask;
            public int Score
            {
                get => base.score;
                set => base.score = value;
            }

            public bool StartExercise => base.startExercise;
            public bool ShowSummary => base.showSummary;

            public void SetTasks(List<CustomTask> taskList) => base.tasks = taskList;
            public void InitTasks() => base.InitTasks();

            public async Task InvokeCheckAnswer(string selectedAnswer) => await CheckAnswer(selectedAnswer);
            public async Task InvokeNextTask() => await NextTask();
            public async Task InvokeStartExercise() => await StartExercise();
            public async Task InvokeEndExercise() => await EndExercise();
            public void InvokeRestartTasks() => RestartTasks();
            public async Task InvokeUpdateUserStats(User user) => await UpdateUserStats(user);
            public void InvokeUpdateUserHighScore(User user) => UpdateUserHighScore(user);
            protected override bool IsAnswerCorrect(string selectedAnswer) => selectedAnswer == CurrentTask?.CorrectAnswer;
        }

        [Fact]
        public void InitTasks_SetsUserTextCorrectly()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Task 1" },
                new CustomTask { Sentence = "Task 2" }
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);
            var cut = RenderComponent<TestableTaskBase>(); // Renders the component
            cut.Instance.SetTasks(testTasks);

            // Act
            cut.Instance.InitTasks();

            // Assert
            Assert.Equal("Task 1", cut.Instance.Tasks[0].UserText);
            Assert.Equal("Task 2", cut.Instance.Tasks[1].UserText);
        }

        [Fact]
        public async Task CheckAnswer_UpdatesScoreOnCorrectAnswer()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Task 1", CorrectAnswer = "CorrectAnswer" }
            };

            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);

            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());

            // Act
            await cut.InvokeAsync(() => cut.Instance.InvokeCheckAnswer("CorrectAnswer"));

            // Assert
            Assert.Equal(20, cut.Instance.Score);
        }

        [Fact]
        public async Task NextTask_MovesToNextTask()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Task 1", CorrectAnswer = "Answer1" },
                new CustomTask { Sentence = "Task 2", CorrectAnswer = "Answer2" }
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);


            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());
            // Act
            await cut.InvokeAsync(() => cut.Instance.InvokeNextTask());

            // Assert
            Assert.Equal("Task 2", cut.Instance.CurrentTask.Sentence);
        }

        [Fact]
        public async Task StartExercise_ResetsState()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Test Task 1" },
                new CustomTask { Sentence = "Test Task 2" }
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);

            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());

            // Assert
            Assert.True(cut.Instance.StartExercise);
            Assert.Equal(0, cut.Instance.Score);
            Assert.Equal(testTasks[0], cut.Instance.CurrentTask);
        }

        [Fact]
        public async Task RestartTasks_ResetsState()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Test Task 1" },
                new CustomTask { Sentence = "Test Task 2" }
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);

            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());
            await cut.InvokeAsync(() => cut.Instance.InvokeCheckAnswer("CorrectAnswer"));

            // Act
            await cut.InvokeAsync(() => cut.Instance.InvokeRestartTasks());

            // Assert
            Assert.False(cut.Instance.CurrentTask.TaskStatus);

        }

        // Test for EndExercise functionality
        [Fact]
        public async Task EndExercise_EndsTheExercise()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Test Task 1" },
                new CustomTask { Sentence = "Test Task 2" }
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);

            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());

            // Act
            await cut.InvokeAsync(() => cut.Instance.InvokeEndExercise());

            // Assert
            Assert.True(cut.Instance.ShowSummary);
            Assert.Equal(0, cut.Instance.Score);
        }

        [Fact]
        public async Task ScoreIncreasesWithCorrectAnswers()
        {
            // Arrange
            var testTasks = new List<CustomTask>
            {
                new CustomTask { Sentence = "Task 1", CorrectAnswer = "CorrectAnswer1" },
                new CustomTask { Sentence = "Task 2", CorrectAnswer = "CorrectAnswer2" }
            };

            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(testTasks);

            await cut.InvokeAsync(() => cut.Instance.InvokeStartExercise());

            await cut.InvokeAsync(() => cut.Instance.InvokeCheckAnswer("CorrectAnswer1"));
            await cut.InvokeAsync(() => cut.Instance.InvokeNextTask());
            await cut.InvokeAsync(() => cut.Instance.InvokeCheckAnswer("CorrectAnswer2"));

            // Assert
            Assert.Equal(40, cut.Instance.Score);
        }

        [Fact]
        public async Task UpdateUserStats_UpdatesUserCorrectly()
        {
            // Arrange
            var user = new User
            {
                HighScore = 50,
                TotalLessonsCompleted = 5
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.SetTasks(new List<CustomTask>
            {
                new CustomTask { Sentence = "Task 1", CorrectAnswer = "CorrectAnswer" },
                new CustomTask { Sentence = "Task 2", CorrectAnswer = "CorrectAnswer" }
            });
            cut.Instance.Score = 80;

            // Act
            await cut.Instance.InvokeUpdateUserStats(user);

            // Assert
            Assert.Equal(7, user.TotalLessonsCompleted); // +2 tasks
            Assert.Equal(80, user.HighScore); // High score updated
            mockUserService.Verify(s => s.UpdateUserAsync(user), Times.Once); // Service called
        }

        [Fact]
        public async Task UpdateUserStats_DoesNothingForNullUser()
        {
            // Arrange
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();

            // Act
            await cut.Instance.InvokeUpdateUserStats(null);

            // Assert
            mockUserService.Verify(s => s.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public void UpdateUserHighScore_UpdatesHighScoreIfHigher()
        {
            // Arrange
            var user = new User
            {
                HighScore = 50
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.Score = 80;

            // Act
            cut.Instance.InvokeUpdateUserHighScore(user);

            // Assert
            Assert.Equal(80, user.HighScore);
        }

        [Fact]
        public void UpdateUserHighScore_DoesNotUpdateIfLowerScore()
        {
            // Arrange
            var user = new User
            {
                HighScore = 50
            };
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.Score = 30;

            // Act
            cut.Instance.InvokeUpdateUserHighScore(user);

            // Assert
            Assert.Equal(50, user.HighScore);
        }

        [Fact]
        public void UpdateUserHighScore_DoesNotThrowForNullUser()
        {
            // Arrange
            // Mock dependencies
            var mockUserService = new Mock<IUserService>();
            Services.AddSingleton(mockUserService.Object);
            var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
            Services.AddSingleton(mockAuthStateProvider.Object);
            var mockTaskService = new Mock<ITaskService<CustomTask>>();
            Services.AddSingleton(mockTaskService.Object);

            var cut = RenderComponent<TestableTaskBase>();
            cut.Instance.Score = 80;

            // Act & Assert
            var exception = Record.Exception(() => cut.Instance.InvokeUpdateUserHighScore(null));
            Assert.Null(exception);
        }
    }
}
