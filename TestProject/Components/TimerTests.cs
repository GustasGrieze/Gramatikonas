using Bunit;
using lithuanian_language_learning_tool.Components;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Xunit;

namespace TestProject.Components
{
    public class TimerTests : TestContext
    {
        [Fact]
        public async Task Timer_CountsDownAndCallsTimerOut_WhenTimeExpires()
        {
            // Arrange
            bool timerOutCalled = false;

            // Render the Timer component with 1 second
            var cut = RenderComponent<Timer>(parameters => parameters
                .Add(p => p.SecondsToRun, 1)
                .Add(p => p.TimerOut, EventCallback.Factory.Create(this, () =>
                {
                    timerOutCalled = true;
                }))
            );

            // Act
            // Wait a bit longer than 1 second so the timer can finish
            await Task.Delay(1500);

            // Assert
            Assert.True(timerOutCalled, "TimerOut callback was not invoked after time expired.");
        }

        [Fact]
        public async Task Timer_DisplaysCorrectTime_WhileCountingDown()
        {
            // Arrange
            var cut = RenderComponent<Timer>(parameters => parameters
                .Add(p => p.SecondsToRun, 2)
            );

            // The initial text is "00:02" (2 seconds)
            Assert.Contains("00:02", cut.Markup);

            // Act
            // Wait ~1 second
            await Task.Delay(1100);

            // Assert that the Timer text is now "00:01"
            // (Because we used 2 seconds, after ~1 second, it should display "00:01")
            Assert.Contains("00:01", cut.Markup);
        }
    }
}
