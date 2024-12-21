using DataAccess.Models;
namespace DataAccess.Helpers
{
    public static class CustomTaskExtensions
    {
        // Converts List<string> to List<TaskOption>
        public static void SetOptionsFromStrings(this CustomTask task, List<string> optionTexts)
        {
            if (task.AnswerOptions == null)
            {
                task.AnswerOptions = new List<AnswerOption>();
            }
            else
            {
                task.AnswerOptions.Clear();
            }

            if (optionTexts != null)
            {
                task.AnswerOptions.AddRange(optionTexts.Select(ot => new AnswerOption { OptionText = ot }));
            }
        }


        // Converts List<TaskOption> to List<string>
        public static List<string> GetOptionTexts(this CustomTask task)
        {
            return task.AnswerOptions?.Select(o => o.OptionText).ToList() ?? new List<string>();
        }


    }
}