namespace DataAccess.Models
{
    public class SpellingTask : CustomTask
    {
        public override int CalculateScore(bool isCorrect, int multiplier = 1)
        {
            return isCorrect ? 15 * multiplier : 0;
        }
    }
}