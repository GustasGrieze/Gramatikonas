using lithuanian_language_learning_tool.Models;

namespace lithuanian_language_learning_tool.Components.Pages
{
    public partial class PunctuationTaskBase : TaskBase<PunctuationTask>
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (tasks.Count > 0)
            {
                foreach (var task in tasks)
                {
                    task.InitializeHighlights();
                }
            }
        }

        protected virtual async Task StartExercise(bool refetchNewTasks = false)
        {
            await base.StartExercise(refetchNewTasks);
            currentTask?.InitializeHighlights();

        }

        protected override async Task NextTask()
        {
            await base.NextTask();
            if (currentTaskIndex < tasks.Count)
            {
                currentTask.InitializeHighlights();
            }
        }
        
        protected void ToggleHighlight(int spaceIndex)
        {
            currentTask?.ToggleHighlight(spaceIndex);
            StateHasChanged();
        }


        protected void HandleInsertPunctuation(string punctuation)
        {
            if (currentTask == null) return;

            currentTask.InsertPunctuation(punctuation);
            StateHasChanged();
        }

        protected void HandleDeletePunctuation()
        {
            if (currentTask == null) return;

            currentTask.DeletePunctuation();
            StateHasChanged();
        }

        protected override bool IsAnswerCorrect(string selectedAnswer)
        {
            return currentTask?.IsAnswerCorrect() ?? false;
        }

    }
}