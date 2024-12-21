using DataAccess.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class CustomTask
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Sentence { get; set; }  // The original sentence presented to the user
        [Required]
        public string UserText { get; set; }  // User's current version of the sentence
        // Database-mapped options
        [Required]
        public virtual List<AnswerOption> AnswerOptions { get; set; } = new();

        // In-code usage options (not mapped to the database)
        [NotMapped]
        public List<string> Options
        {
            get => this.GetOptionTexts(); // Calls the extension method
            set => this.SetOptionsFromStrings(value); // Calls the extension method
        }

        [Required]
        public string CorrectAnswer { get; set; }  // The correct version of the sentence with correct punctuation
        public string Explanation { get; set; } = ""; // The explanation for why this punctuation is correct
        public bool TaskStatus { get; set; } = false; // incorrect by default
        public string Topic { get; set; } = string.Empty; // topic empty string by default

        public virtual void Reset()
        {
            UserText = Sentence;
            TaskStatus = false;
        }

        public virtual int CalculateScore(bool isCorrect, int multiplier = 1)
        {
            return isCorrect ? 10 * multiplier : 0;
        }


    }
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OptionText { get; set; }

        public int CustomTaskId { get; set; }

        [ForeignKey("CustomTaskId")]
        public virtual CustomTask CustomTask { get; set; }
    }
}