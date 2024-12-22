using System;
using System.Linq;
using Xunit;
using DataAccess.Models;

namespace TestProject.Models
{
    public class PunctuationTaskTests
    {
        [Fact]
        public void InitializeHighlights_SetsHighlightForEverySpaceInSentence()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world from xUnit",
                UserText = "Hello world from xUnit"
            };

            // Act
            task.InitializeHighlights();

            // Assert
            // "Hello world from xUnit" has 3 spaces: indices after "Hello", "world", "from"
            // Indices: "Hello"(5), "world"(11), "from"(16)
            Assert.Equal(3, task.Highlights.Count);
            Assert.Contains(task.Highlights, h => h.SpaceIndex == 5);
            Assert.Contains(task.Highlights, h => h.SpaceIndex == 11);
            Assert.Contains(task.Highlights, h => h.SpaceIndex == 16);
        }

        [Fact]
        public void InitializeHighlights_EmptySentence_CreatesNoHighlights()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "",
                UserText = ""
            };

            // Act
            task.InitializeHighlights();

            // Assert
            Assert.Empty(task.Highlights);
        }

        [Fact]
        public void ToggleHighlight_SetsCorrectSpaceIndexAsSelected()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world example",
                UserText = "Hello world example"
            };
            task.InitializeHighlights(); // Creates highlights at spaces

            // Act
            // Suppose we toggle the highlight for the first space index
            var firstSpaceIndex = task.Highlights[0].SpaceIndex;
            task.ToggleHighlight(firstSpaceIndex);

            // Assert
            // The highlight at firstSpaceIndex should be IsSelected = true, others false
            Assert.True(task.Highlights[0].IsSelected, "Expected the first highlight to be selected.");

            for (int i = 1; i < task.Highlights.Count; i++)
            {
                Assert.False(task.Highlights[i].IsSelected, $"Expected highlight {i} to be not selected.");
            }
        }

        [Fact]
        public void ToggleHighlight_NoHighlights_DoesNothing()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "NoSpacesHere",
                UserText = "NoSpacesHere"
            };
            // No spaces => no highlights
            task.InitializeHighlights();

            // Act
            // Attempt to toggle a space index that doesn't exist
            task.ToggleHighlight(5);

            // Assert
            Assert.Empty(task.Highlights); // Nothing changes
        }

        [Fact]
        public void InsertPunctuation_NoHighlightSelected_DoesNothing()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            task.InitializeHighlights();

            // No highlights are selected by default
            // Act
            task.InsertPunctuation(",");

            // Assert
            Assert.Equal("Hello world", task.UserText);
        }

        [Fact]
        public void InsertPunctuation_SelectedHighlight_InsertsAndShiftsSpaceIndices()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            task.InitializeHighlights();
            // "Hello world" has 1 space at index 5
            // Mark it selected
            var indexOfSpace = task.Highlights[0].SpaceIndex; // 5
            task.ToggleHighlight(indexOfSpace);

            // Act
            task.InsertPunctuation(",");

            // Assert
            // The user text should now become "Hello, world"
            Assert.Equal("Hello, world", task.UserText);

            // The space index for "world" was 5, we inserted 1 char at 5
            // so new space index should shift from 5 to 6
            Assert.Single(task.Highlights);
            Assert.False(task.Highlights[0].HasPunctuation);
            Assert.Equal(6, task.Highlights[0].SpaceIndex);
        }

        [Fact]
        public void InsertPunctuation_ReplacesExistingPunctuationIfPresent()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            task.InitializeHighlights();
            // Mark the highlight as selected
            var indexOfSpace = task.Highlights[0].SpaceIndex; // 5
            task.ToggleHighlight(indexOfSpace);

            // Insert the first punctuation
            task.InsertPunctuation(",");
            // "Hello, world"

            // Act
            // Insert a new punctuation in the same place
            task.InsertPunctuation("!");

            // Assert
            // The punctuation should be replaced at [5], not appended
            Assert.Equal("Hello! world", task.UserText);
            Assert.Equal(6, task.Highlights[0].SpaceIndex);
            Assert.False(task.Highlights[0].IsSelected);
        }

        [Fact]
        public void DeletePunctuation_NoHighlightSelected_DoesNothing()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            task.InitializeHighlights();

            // Act
            task.DeletePunctuation();

            // Assert
            // Should remain unchanged
            Assert.Equal("Hello world", task.UserText);
            Assert.False(task.Highlights.Any(h => h.HasPunctuation));
        }

        [Fact]
        public void DeletePunctuation_SelectedHighlight_DeletesPunctuationAndShiftsIndices()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            task.InitializeHighlights();
            // Select the highlight, insert punctuation, then remove it
            var indexOfSpace = task.Highlights[0].SpaceIndex; // 5
            task.ToggleHighlight(indexOfSpace);
            task.InsertPunctuation(",");
            // Now "Hello, world"

            // Re-select (the new index of the highlight is 6)
            task.ToggleHighlight(6);

            // Act
            // Delete the punctuation at [5]
            task.DeletePunctuation();

            // Assert
            // Should revert back to "Hello world"
            Assert.Equal("Hello world", task.UserText);
            Assert.Single(task.Highlights);
            Assert.Equal(5, task.Highlights[0].SpaceIndex);
            Assert.False(task.Highlights[0].HasPunctuation);
        }

        [Fact]
        public void DeletePunctuation_WhenNoPunctuationPresent_DoesNothing()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world test",
                UserText = "Hello world test"
            };
            task.InitializeHighlights();
            // Select highlight for the first space
            var indexOfSpace = task.Highlights[0].SpaceIndex;
            task.ToggleHighlight(indexOfSpace);

            // Act
            task.DeletePunctuation();

            // Assert
            Assert.Equal("Hello world test", task.UserText);
            Assert.False(task.Highlights[0].HasPunctuation);
        }

        [Fact]
        public void IsAnswerCorrect_WhenUserTextMatchesCorrectAnswerPunctuation_ReturnsTrue()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello, world!",
                CorrectAnswer = "Hello, world!"
            };

            // Act
            var result = task.IsAnswerCorrect();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAnswerCorrect_WhenUserTextDiffers_ReturnsFalse()
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world",
                CorrectAnswer = "Hello, world!"
            };

            // Act
            var result = task.IsAnswerCorrect();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAnswerCorrect_WhenUserTextIsEmptyOrCorrectAnswerIsEmpty_ReturnsFalse()
        {
            // Arrange
            var taskEmptyUser = new PunctuationTask
            {
                Sentence = "Hi",
                UserText = "",
                CorrectAnswer = "Hi!"
            };

            var taskEmptyCorrect = new PunctuationTask
            {
                Sentence = "Hi",
                UserText = "Hi!",
                CorrectAnswer = ""
            };

            // Act & Assert
            Assert.False(taskEmptyUser.IsAnswerCorrect());
            Assert.False(taskEmptyCorrect.IsAnswerCorrect());
        }

        [Theory]
        [InlineData(true, 1, 20)]
        [InlineData(true, 2, 40)]
        [InlineData(false, 1, 0)]
        [InlineData(false, 3, 0)]
        public void CalculateScore_ReturnsExpectedScore(bool isCorrect, int multiplier, int expectedScore)
        {
            // Arrange
            var task = new PunctuationTask
            {
                Sentence = "Test"
            };

            // Act
            var result = task.CalculateScore(isCorrect, multiplier);

            // Assert
            Assert.Equal(expectedScore, result);
        }
    }
}
