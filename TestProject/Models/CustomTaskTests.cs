using DataAccess.Models;
using System.Collections.Generic;
namespace TestProject.Models
{
    public class CustomTaskTests
    {
        [Fact]
        public void Reset_ShouldResetUserTextAndTaskStatus()
        {
            // Arrange
            var task = new CustomTask
            {
                Sentence = "Original sentence.",
                UserText = "Modified sentence.",
                TaskStatus = true
            };

            // Act
            task.Reset();

            // Assert
            Assert.Equal("Original sentence.", task.UserText);
            Assert.False(task.TaskStatus);
        }

        [Theory]
        [InlineData(true, 1, 10)]  // Correct with multiplier 1
        [InlineData(true, 2, 20)]  // Correct with multiplier 2
        [InlineData(false, 1, 0)]  // Incorrect with multiplier 1
        [InlineData(false, 3, 0)]  // Incorrect with multiplier 3
        public void CalculateScore_ShouldReturnCorrectValue(bool isCorrect, int multiplier, int expectedScore)
        {
            // Arrange
            var task = new CustomTask();

            // Act
            var score = task.CalculateScore(isCorrect, multiplier);

            // Assert
            Assert.Equal(expectedScore, score);
        }

        [Fact]
        public void Options_Getter_ShouldReturnCorrectValues()
        {
            // Arrange
            var task = new CustomTask
            {
                AnswerOptions = new List<AnswerOption>
        {
            new AnswerOption { OptionText = "Option 1" },
            new AnswerOption { OptionText = "Option 2" }
        }
            };

            // Act
            var options = task.Options;

            // Assert
            Assert.Contains("Option 1", options);
            Assert.Contains("Option 2", options);
        }

        [Fact]
        public void Options_Setter_ShouldUpdateAnswerOptions()
        {
            // Arrange
            var task = new CustomTask();

            // Act
            task.Options = new List<string> { "Option A", "Option B" };

            // Assert
            Assert.Equal(2, task.AnswerOptions.Count);
            Assert.Equal("Option A", task.AnswerOptions[0].OptionText);
            Assert.Equal("Option B", task.AnswerOptions[1].OptionText);
        }


        [Fact]
        public void CustomTask_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var task = new CustomTask();

            // Assert
            Assert.Equal(string.Empty, task.Topic); // Default topic
            Assert.False(task.TaskStatus); // Default task status
            Assert.NotNull(task.AnswerOptions); // Ensure list is initialized
            Assert.Empty(task.AnswerOptions); // Ensure list starts empty
            Assert.NotNull(task.Options); // Ensure options list is initialized
            Assert.Empty(task.Options); // Ensure options list starts empty
            Assert.Equal(string.Empty, task.Explanation); // Default explanation
        }


        [Fact]
        public void AnswerOption_ShouldInitializeCorrectly()
        {
            // Arrange
            var answerOption = new AnswerOption
            {
                Id = 1,
                OptionText = "Test Option",
                CustomTaskId = 10
            };

            // Assert
            Assert.Equal(1, answerOption.Id);
            Assert.Equal("Test Option", answerOption.OptionText);
            Assert.Equal(10, answerOption.CustomTaskId);
        }


    }
}
