using DataAccess.Models;
using Xunit;

namespace TestProject.Models
{
    public class SpellingTaskTests
    {
        [Theory]
        [InlineData(true, 1, 15)]  // Correct answer, multiplier 1
        [InlineData(true, 2, 30)]  // Correct answer, multiplier 2
        [InlineData(false, 1, 0)]  // Incorrect answer, multiplier 1
        [InlineData(false, 3, 0)]  // Incorrect answer, multiplier 3
        public void CalculateScore_ShouldReturnCorrectValues(bool isCorrect, int multiplier, int expectedScore)
        {
            // Arrange
            var task = new SpellingTask();

            // Act
            var score = task.CalculateScore(isCorrect, multiplier);

            // Assert
            Assert.Equal(expectedScore, score);
        }
    }
}
