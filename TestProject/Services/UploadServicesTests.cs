using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Models;
using DataAccess.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestProject.Services
{
    public class UploadServiceTests
    {
        private readonly Mock<ILogger<UploadService>> _mockLogger;
        private readonly Mock<ITaskService<PunctuationTask>> _mockPunctuationTaskService;
        private readonly Mock<ITaskService<SpellingTask>> _mockSpellingTaskService;
        private readonly UploadService _uploadService;

        public UploadServiceTests()
        {
            _mockLogger = new Mock<ILogger<UploadService>>();
            _mockPunctuationTaskService = new Mock<ITaskService<PunctuationTask>>();
            _mockSpellingTaskService = new Mock<ITaskService<SpellingTask>>();
            _uploadService = new UploadService(
                _mockLogger.Object,
                _mockPunctuationTaskService.Object,
                _mockSpellingTaskService.Object);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_ValidPunctuationTasks_Success()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { ".", ",", "!" } },
                    { "CorrectAnswer", "." },
                    { "Explanation", "Explanation here" }
                }
            });

            var punctuationTasksJson = JsonSerializer.Serialize(new List<PunctuationTask>
            {
                new PunctuationTask
                {
                    Sentence = "This is a test sentence",
                    Options = new List<string> { ".", ",", "!" },
                    CorrectAnswer = ".",
                    Explanation = "Explanation here",
                    UserText = "This is a test sentence",
                    Topic = "TestTopic"
                }
            });

            // Act
            await _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic");

            // Assert
            _mockPunctuationTaskService.Verify(s => s.AddTaskAsync(It.Is<PunctuationTask>(t =>
                t.Sentence == "This is a test sentence" &&
                t.Options.SequenceEqual(new List<string> { ".", ",", "!" }) &&
                t.CorrectAnswer == "." &&
                t.Explanation == "Explanation here" &&
                t.UserText == "This is a test sentence" &&
                t.Topic == "TestTopic"
            )), Times.Once);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_ValidSpellingTasks_Success()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { "test", "tset", "tost" } },
                    { "CorrectAnswer", "test" },
                    { "Explanation", "Explanation here" }
                }
            });

            var spellingTasksJson = JsonSerializer.Serialize(new List<SpellingTask>
            {
                new SpellingTask
                {
                    Sentence = "This is a test sentence",
                    Options = new List<string> { "test", "tset", "tost" },
                    CorrectAnswer = "test",
                    Explanation = "Explanation here",
                    UserText = "This is a test sentence",
                    Topic = "TestTopic"
                }
            });

            // Act
            await _uploadService.ValidateAndUploadAsync(jsonContent, "spelling", "TestTopic");

            // Assert
            _mockSpellingTaskService.Verify(s => s.AddTaskAsync(It.Is<SpellingTask>(t =>
                t.Sentence == "This is a test sentence" &&
                t.Options.SequenceEqual(new List<string> { "test", "tset", "tost" }) &&
                t.CorrectAnswer == "test" &&
                t.Explanation == "Explanation here" &&
                t.UserText == "This is a test sentence" &&
                t.Topic == "TestTopic"
            )), Times.Once);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_EmptyJson_ThrowsTaskUploadException()
        {
            // Arrange
            string jsonContent = "   ";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Equal("Failas yra tuščias.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_InvalidJson_ThrowsTaskUploadException()
        {
            // Arrange
            string jsonContent = "{ invalid json }";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Equal("JSON failas yra netinkamai suformatuotas.", ex.Message);
            VerifyLoggerError("Klaida įkeliant užduotį");
        }

        [Fact]
        public async Task ValidateAndUploadAsync_InvalidTaskType_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { ".", ",", "!" } },
                    { "CorrectAnswer", "." },
                    { "Explanation", "Explanation here" }
                }
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "invalidType", "TestTopic"));

            Assert.Equal("Neteisingas užduoties tipas.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_MissingFields_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    // Missing "Sentence"
                    { "Options", new List<string> { ".", ",", "!" } },
                    { "CorrectAnswer", "." },
                    { "Explanation", "Explanation here" }
                }
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Equal("Netinkama užduoties struktūra: trūksta laukų 'Sentence'.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_InvalidOptionsForPunctuation_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { ".", "invalidOption", "!" } },
                    { "CorrectAnswer", "." },
                    { "Explanation", "Explanation here" }
                }
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Equal("Netinkama Options struktūra: skyrybos užduotyse leidžiami tik skyrybos ženklai.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_InvalidOptionsForSpelling_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { "test", "tset1", "tost" } }, // "tset1" contains a digit
                    { "CorrectAnswer", "test" },
                    { "Explanation", "Explanation here" }
                }
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "spelling", "TestTopic"));

            Assert.Equal("Netinkama Options struktūra: rašybos užduotyse leidžiamos tik raidės arba tarpai.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_TasksListIsEmpty_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Equal("Failas neturi užduočių arba yra tuščias.", ex.Message);
        }

        [Fact]
        public async Task ValidateAndUploadAsync_AddTaskAsyncThrowsException_ThrowsTaskUploadException()
        {
            // Arrange
            var jsonContent = JsonSerializer.Serialize(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Sentence", "This is a test sentence" },
                    { "Options", new List<string> { ".", ",", "!" } },
                    { "CorrectAnswer", "." },
                    { "Explanation", "Explanation here" }
                }
            });

            _mockPunctuationTaskService
                .Setup(s => s.AddTaskAsync(It.IsAny<PunctuationTask>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TaskUploadException>(() =>
                _uploadService.ValidateAndUploadAsync(jsonContent, "punctuation", "TestTopic"));

            Assert.Contains("Klaida įkeliant užduotis: Database error", ex.Message);
            VerifyLoggerError("Klaida įkeliant užduotį");
        }

        [Fact]
        public void LogException_LogsError()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act
            _uploadService.LogException(exception);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Klaida įkeliant užduotį.")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        private void VerifyLoggerError(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
