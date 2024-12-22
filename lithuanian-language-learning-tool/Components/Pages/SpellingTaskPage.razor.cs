using lithuanian_language_learning_tool.Models;

namespace lithuanian_language_learning_tool.Components.Pages
{
    public partial class SpellingTaskBase : TaskBase<SpellingTask>
    {
        protected override bool IsAnswerCorrect(string selectedAnswer)
        {
            return selectedAnswer == currentTask.CorrectAnswer;
        }


    }
}