using gramatikonas.Components;
using DataAccess.Models;
using System.Collections.Generic;



namespace TestProject.Components
{
    public class OptionsSectionTest : TestContext
    {
        [Fact]
        public void OptionsSection_ShouldRenderOptions()
        {
            // Arrange
            using var ctx = new TestContext();
            var task = new PunctuationTask
            {
                Options = new List<string> { ".", ",", ";" }
            };

            // Act
            var cut = ctx.RenderComponent<OptionsSection<CustomTask>>(parameters => parameters
                .Add(p => p.Task, task)
            );

            // Assert
            Assert.Contains(".", cut.Markup);
            Assert.Contains(",", cut.Markup);
            Assert.Contains(";", cut.Markup);
        }

    }
}
