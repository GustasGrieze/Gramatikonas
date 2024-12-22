using DataAccess.Models;
using gramatikonas.Components.Pages;

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