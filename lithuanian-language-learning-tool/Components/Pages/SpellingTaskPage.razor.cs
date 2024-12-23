using gramatikonas.Models;

namespace gramatikonas.Components.Pages
{
    public partial class SpellingTaskBase : TaskBase<SpellingTask>
    {
        protected override bool IsAnswerCorrect(string selectedAnswer)
        {
            return selectedAnswer == currentTask.CorrectAnswer;
        }


    }
}