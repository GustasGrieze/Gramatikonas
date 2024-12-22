// File: AdminTaskUploadTests.cs

using Bunit;
using Bunit.TestDoubles; // For InputFileContent
using DataAccess.Models;
using BusinessLogic.Services;
using gramatikonas.Exceptions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using gramatikonas.Components; // Ensure semicolon

namespace TestProject.Components
{
    public class AdminTaskUploadTests : TestContext, IDisposable
    {
        private readonly Mock<IUploadService> _mockUploadService;

        public AdminTaskUploadTests()
        {
            _mockUploadService = new Mock<IUploadService>();
            Services.AddSingleton(_mockUploadService.Object);
        }

        public void Dispose()
        {
            // Cleanup if necessary
        }

        [Fact]
        public void RenderForm_Correctly()
        {
            // Arrange & Act
            var cut = RenderComponent<AdminTaskUpload>();

            // Assert
            // Verify the presence of the header
            var header = cut.Find("h3");
            Assert.Equal("Įkelti Naujas Užduotis", header.TextContent.Trim());

            // Verify the presence of the form
            var form = cut.Find("form");
            Assert.NotNull(form);

            // Verify the submit button
            var submitButton = cut.Find("button.next-button");
            Assert.Equal("Įkelti Užduotis", submitButton.TextContent.Trim());
        }

        [Fact]
        public void ValidationErrors_Shown_WhenRequiredFieldsMissing()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            // Act
            cut.Find("form").Submit();

            // Assert
            Assert.Contains("Prašome pasirinkti užduoties tipą.", cut.Markup);
            Assert.Contains("Prašome įkelti užduočių failą.", cut.Markup);
        }

        [Theory]
        [InlineData("punctuation")]
        [InlineData("spelling")]
        public void TopicDropdown_ShowsCorrectOptions_BasedOnTaskType(string taskType)
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            // Act
            var taskTypeSelect = cut.Find("select");
            taskTypeSelect.Change(taskType);

            // Assert
            var expectedTopics = taskType == "punctuation"
                ? new[]
                {
                    "Vienarūšės sakinio dalys",
                    "Pažyminiai",
                    "Sudėtiniai sakiniai",
                    "Priedėliai",
                    "Įterpiniai"
                }
                : new[]
                {
                    "Nosinės",
                    "Žodžių rašyba, kartu ir atskirai",
                    "Įrašyk praleistas raides"
                };

            foreach (var topic in expectedTopics)
            {
                Assert.Contains(topic, cut.Markup);
            }
        }

        [Fact]
        public void TopicDropdown_IsNotVisible_Initially()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            // Act & Assert
            var allSelects = cut.FindAll("select");
            Assert.Single(allSelects); // only TaskType dropdown

            Assert.DoesNotContain("Tema:", cut.Markup);
        }

        [Fact]
        public void TopicDropdown_IsVisible_AfterTaskTypeSelection()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            // Act
            var firstSelect = cut.Find("select");
            firstSelect.Change("punctuation");

            // Assert
            var allSelects = cut.FindAll("select");
            Assert.Equal(2, allSelects.Count);

            Assert.Contains("Tema:", cut.Markup);
        }


        [Fact]
        public void FileUpload_SetsFileNameAndContent()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            var fileContent = "{\"tasks\": []}";
            var fileName = "tasks.json";
            var file = InputFileContent.CreateFromText(fileContent, fileName);

            // Act
            var inputFileComponent = cut.FindComponent<InputFile>();
            inputFileComponent.UploadFiles(file);

            // Assert
            Assert.Contains($"Pasirinktas failas: {fileName}", cut.Markup);
        }

        [Fact]
        public async Task SuccessfulFormSubmission_CallsUploadService_AndShowsSuccessMessage()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            var taskType = "punctuation";
            var selectedTopic = "Vienarūšės sakinio dalys";
            var fileName = "tasks.json";
            var fileContent = "{\"tasks\": []}";
            var file = InputFileContent.CreateFromText(fileContent, fileName);

            // Mock successful upload
            _mockUploadService
                .Setup(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic))
                .Returns(Task.CompletedTask);

            // Act
            // 1. Select task type
            cut.Find("select").Change(taskType);

            // 2. Select topic
            var topicSelect = cut.FindAll("select")[1];
            topicSelect.Change(selectedTopic);

            // 3. Upload file
            var inputFileComponent = cut.FindComponent<InputFile>();
            inputFileComponent.UploadFiles(file);

            // 4. Submit form
            cut.Find("form").Submit();

            // Assert
            Assert.Contains("Užduotys sėkmingai įkeltos.", cut.Markup);
            _mockUploadService.Verify(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic), Times.Once);
        }

        [Fact]
        public async Task FormSubmission_WithTaskUploadException_ShowsErrorMessage()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            var taskType = "spelling";
            var selectedTopic = "Nosinės";
            var fileName = "tasks.json";
            var fileContent = "{\"tasks\": []}";
            var file = InputFileContent.CreateFromText(fileContent, fileName);

            var exceptionMessage = "Invalid task data.";
            _mockUploadService
                .Setup(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic))
                .ThrowsAsync(new TaskUploadException(exceptionMessage));

            // Act
            // 1. Select task type
            cut.Find("select").Change(taskType);

            // 2. Select topic
            var topicSelect = cut.FindAll("select")[1];
            topicSelect.Change(selectedTopic);

            // 3. Upload file
            var inputFileComponent = cut.FindComponent<InputFile>();
            inputFileComponent.UploadFiles(file);

            // 4. Submit form
            cut.Find("form").Submit();

            // Assert
            Assert.Contains($"Klaida įkeliant užduotis: {exceptionMessage}", cut.Markup);
            _mockUploadService.Verify(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic), Times.Once);
        }

        [Fact]
        public async Task FormSubmission_WithGeneralException_ShowsErrorMessage()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            var taskType = "spelling";
            var selectedTopic = "Nosinės";
            var fileName = "tasks.json";
            var fileContent = "{\"tasks\": []}";

            var exceptionMessage = "Unexpected error occurred.";
            _mockUploadService
                .Setup(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            // 1. Select task type
            cut.Find("select").Change(taskType);

            // 2. Select topic
            var topicSelect = cut.FindAll("select")[1];
            topicSelect.Change(selectedTopic);

            // 3. Upload file
            var inputFileComponent = cut.FindComponent<InputFile>();
            inputFileComponent.UploadFiles(InputFileContent.CreateFromText(fileContent, fileName));

            // 4. Submit form
            cut.Find("form").Submit();

            // Assert
            Assert.Contains($"Nenumatyta klaida: {exceptionMessage}", cut.Markup);
            _mockUploadService.Verify(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic), Times.Once);
        }

        [Fact]
        public async Task FormSubmission_WithInvalidJson_ShowsValidationError()
        {
            // Arrange
            var cut = RenderComponent<AdminTaskUpload>();

            var taskType = "punctuation";
            var selectedTopic = "Vienarūšės sakinio dalys";
            var fileName = "invalid_tasks.json";
            var fileContent = "Invalid JSON Content";
            var file = InputFileContent.CreateFromText(fileContent, fileName);

            var exceptionMessage = "JSON is invalid.";
            _mockUploadService
                .Setup(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic))
                .ThrowsAsync(new TaskUploadException(exceptionMessage));

            // Act
            // 1. Select task type
            cut.Find("select").Change(taskType);

            // 2. Select topic
            var topicSelect = cut.FindAll("select")[1];
            topicSelect.Change(selectedTopic);

            // 3. Upload invalid JSON file
            var inputFileComponent = cut.FindComponent<InputFile>();
            inputFileComponent.UploadFiles(file);

            // 4. Submit form
            cut.Find("form").Submit();

            // Assert
            Assert.Contains($"Klaida įkeliant užduotis: {exceptionMessage}", cut.Markup);
            _mockUploadService.Verify(us => us.ValidateAndUploadAsync(fileContent, taskType, selectedTopic), Times.Once);
        }
    }
}
