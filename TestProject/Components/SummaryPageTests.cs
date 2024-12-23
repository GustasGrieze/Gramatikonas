using DataAccess.Models;
using gramatikonas.Components;
using System.Collections.Generic;

namespace TestProject.Components.Tests
{
    public class SummaryPageTests : TestContext
    {
        // Helper method to create a list of PunctuationTask
        private List<PunctuationTask> CreateTasks(int count, bool defaultStatus = false)
        {
            var tasks = new List<PunctuationTask>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(new PunctuationTask
                {
                    Id = i + 1,
                    Sentence = $"Sentence {i + 1}",
                    UserText = $"User Text {i + 1}",
                    CorrectAnswer = $"Correct Answer {i + 1}",
                    TaskStatus = defaultStatus
                });
            }
            return tasks;
        }

        [Fact]
        public void Renders_Header_Correctly()
        {
            // Arrange
            var tasks = CreateTasks(3);

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.Score, 30)
                .Add(p => p.CorrectAnswersCount, 3)
            );

            // Assert
            var header = component.Find("h1.overview-title");
            Assert.Equal("Užduoties apžvalga", header.TextContent);
        }


        [Fact]
        public void Renders_Correct_Number_Of_Task_Buttons()
        {
            // Arrange
            var tasks = CreateTasks(5);

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.Score, 50)
                .Add(p => p.CorrectAnswersCount, 5)
            );

            // Assert
            var buttons = component.FindAll("button.summary-button");
            Assert.Equal(5, buttons.Count);
        }

        [Theory]
        [InlineData(3, new bool[] { true, false, true }, new string[] { "green", "red", "green" })]
        [InlineData(2, new bool[] { false, false }, new string[] { "red", "red" })]
        [InlineData(4, new bool[] { true, true, true, true }, new string[] { "green", "green", "green", "green" })]
        public void Task_Buttons_Have_Correct_Classes(int taskCount, bool[] statuses, string[] expectedClasses)
        {
            // Arrange
            var tasks = CreateTasks(taskCount);
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i].TaskStatus = statuses[i];
            }

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.TaskStatus, new List<bool>(statuses))
                .Add(p => p.Score, 20)
                .Add(p => p.CorrectAnswersCount, 2)
            );

            // Assert
            var buttons = component.FindAll("button.summary-button");
            for (int i = 0; i < taskCount; i++)
            {
                Assert.Contains(expectedClasses[i], buttons[i].ClassList);
            }
        }

        [Fact]
        public void Displays_Score_And_CorrectAnswers_Correctly()
        {
            // Arrange
            var tasks = CreateTasks(4);
            int score = 40;
            int correctAnswers = 4;

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.Score, score)
                .Add(p => p.CorrectAnswersCount, correctAnswers)
            );

            // Assert
            var scoreElement = component.FindAll(".correct-answers")[1];
            Assert.Contains(score.ToString(), scoreElement.InnerHtml);

            var correctAnswersElement = component.FindAll(".correct-answers")[0];
            Assert.Contains($"{correctAnswers} / {tasks.Count}", correctAnswersElement.InnerHtml);
        }

        [Fact]
        public void Clicking_Task_Button_Invokes_OnTaskSelected_With_Correct_Index()
        {
            // Arrange
            var tasks = CreateTasks(3);
            int? selectedIndex = null;

            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.OnTaskSelected, (int index) => selectedIndex = index)
                .Add(p => p.Score, 30)
                .Add(p => p.CorrectAnswersCount, 3)
            );

            for (int i = 0; i < tasks.Count; i++)
            {
                // Act
                var button = component.FindAll("button.summary-button")[i];
                component.InvokeAsync(() => button.Click()); // Ensure interaction happens on the latest render tree

                // Assert
                Assert.Equal(i, selectedIndex);
                selectedIndex = null; // Reset for the next iteration
            }
        }


        [Fact]
        public void Clicking_Restart_Button_Invokes_OnRestart_Callback()
        {
            // Arrange
            var tasks = CreateTasks(2);
            bool restartClicked = false;

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.OnRestart, () => restartClicked = true)
                .Add(p => p.Score, 20)
                .Add(p => p.CorrectAnswersCount, 2)
            );

            var restartButton = component.Find("button.next-button");
            restartButton.Click();

            // Assert
            Assert.True(restartClicked);
        }

        [Fact]
        public void Handles_Empty_Tasks_List_Correctly()
        {
            // Arrange
            var tasks = new List<PunctuationTask>();

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.Score, 0)
                .Add(p => p.CorrectAnswersCount, 0)
            );

            // Assert
            var buttons = component.FindAll("button.summary-button");
            Assert.Empty(buttons);
            // Optionally, verify if there's a message indicating no tasks
            // For example, if you have a conditional message in the component:
            // var noTasksMessage = component.Find(".no-tasks-message");
            // Assert.Contains("No tasks available", noTasksMessage.InnerHtml);
        }

        [Fact]
        public void Component_Renders_Correctly_With_Varying_TaskStatus()
        {
            // Arrange
            var tasks = CreateTasks(3);
            tasks[0].TaskStatus = true;  // Task 1 is completed
            tasks[1].TaskStatus = false; // Task 2 is incomplete
            tasks[2].TaskStatus = true;  // Task 3 is completed

            // Act
            var component = RenderComponent<SummaryPage<PunctuationTask>>(parameters => parameters
                .Add(p => p.Tasks, tasks)
                .Add(p => p.Score, 25)
                .Add(p => p.CorrectAnswersCount, 2)
            );

            // Assert
            var buttons = component.FindAll("button.summary-button");
            Assert.Equal(3, buttons.Count);

            Assert.Contains("green", buttons[0].ClassList); // First button should be green
            Assert.Contains("red", buttons[1].ClassList);   // Second button should be red
            Assert.Contains("green", buttons[2].ClassList); // Third button should be green
        }

    }
}
