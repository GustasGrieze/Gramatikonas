using Bunit;
using Xunit;
using System.Linq;
using DataAccess.Models;
using Microsoft.AspNetCore.Components;
using gramatikonas.Components;

namespace TestProject.Components
{
    public class PunctuationTaskDisplayTests : TestContext
    {
        [Fact]
        public void RendersHighlighedSpacesProperly()
        {
            // Arrange
            var pTask = new PunctuationTask
            {
                Sentence = "Hello world example",
                UserText = "Hello world example"
            };
            pTask.InitializeHighlights();
            // "Hello world example" => spaces at indices 5, 11 => 2 highlights

            // Act
            var cut = RenderComponent<PunctuationTaskDisplay>(parameters => parameters
                .Add(p => p.PuncTask, pTask)
            );

            // Assert
            // The component splits the text into substrings around each highlight
            // So it will contain spans for:
            //  - "Hello" plus highlight
            //  - "world" plus highlight
            //  - "example"
            var spans = cut.FindAll("span");
            Assert.NotEmpty(spans);

            // We expect 3 substring spans + 2 "space" spans = 5 spans total
            //  Substrings: "Hello", "world", "example"
            //  Spaces: 2
            // Implementation note: The final <span> might contain the last substring
            Assert.Equal(5, spans.Count);

            // Check that the first substring is "Hello"
            Assert.Contains("Hello", spans[0].TextContent);

            // The second <span> should be an empty space with background (clickable) 
            Assert.True(string.IsNullOrWhiteSpace(spans[1].TextContent));

            // The third <span> should have "world"
            Assert.Contains("world", spans[2].TextContent);
        }

        [Fact]
        public void OnToggleHighlight_Called_WhenSpaceIsClicked()
        {
            // Arrange
            var pTask = new PunctuationTask
            {
                Sentence = "Hello world",
                UserText = "Hello world"
            };
            pTask.InitializeHighlights();
            // Spaces: index 5 => single highlight

            // We'll track whether we got the correct highlight index
            int toggledSpaceIndex = -1;

            // Act
            var cut = RenderComponent<PunctuationTaskDisplay>(parameters => parameters
                .Add(p => p.PuncTask, pTask)
                .Add(p => p.OnToggleHighlight, EventCallback.Factory.Create<int>(this, (idx) =>
                {
                    toggledSpaceIndex = idx;
                }))
            );

            // The second <span> is likely the clickable space (the first <span> has "Hello")
            var spaceSpan = cut.FindAll("span")[1];

            // Click the space span
            spaceSpan.Click();

            // Assert
            Assert.Equal(5, toggledSpaceIndex);
        }
    }
}
