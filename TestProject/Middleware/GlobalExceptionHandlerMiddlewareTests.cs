using gramatikonas.Middleware;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    public class GlobalExceptionHandlerMiddlewareTests
    {
        private readonly RequestDelegate _noExceptionDelegate = (ctx) => Task.CompletedTask;
        private readonly RequestDelegate _throwExceptionDelegate = (ctx) => throw new InvalidOperationException("Test Exception");

        [Fact]
        public async Task InvokeAsync_NoException_PassesThroughWithoutLoggingOrErrorResponse()
        {
            // Arrange
            var middleware = new GlobalExceptionHandlerMiddleware(_noExceptionDelegate);
            var context = new DefaultHttpContext();

            // Use a unique temporary folder for logs
            using var tempFolder = new TempLogFolder();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

            // The logs folder should be empty since no exception occurred
            Assert.False(File.Exists(Path.Combine(tempFolder.FolderPath, "errors.log")),
                "No errors.log file should be created when no exception is thrown.");
        }

        /// A helper class that creates a unique temporary folder for logs
        /// and cleans it up automatically at the end of the test.
        internal sealed class TempLogFolder : IDisposable
        {
            public string FolderPath { get; }

            public TempLogFolder()
            {
                FolderPath = Path.Combine(Path.GetTempPath(), "LogsTest_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(FolderPath);

                // Override the folder used in the middleware by temporarily changing the working directory
                Directory.SetCurrentDirectory(FolderPath);
            }

            public void Dispose()
            {
                // Return to a safe directory to allow deletion
                Directory.SetCurrentDirectory(Path.GetTempPath());
                if (Directory.Exists(FolderPath))
                {
                    Directory.Delete(FolderPath, recursive: true);
                }
            }
        }
    }
}
