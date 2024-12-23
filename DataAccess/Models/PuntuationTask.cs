using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class PunctuationTask : CustomTask
    {
        [NotMapped]
        public List<Highlight> Highlights { get; set; }
        public PunctuationTask()
        {
            Highlights = new List<Highlight>();
        }

        public void InitializeHighlights()
        {
            Highlights.Clear();
            for (var i = 0; i < Sentence.Length; i++)
            {
                if (Sentence[i] == ' ')
                {
                    Highlights.Add(new Highlight(i));
                }
            }
        }

        public struct Highlight
        {
            public int SpaceIndex { get; set; }  // The index of the space between words
            public bool IsSelected { get; set; } // Whether this space is currently highlighted
            public bool HasPunctuation { get; set; } // Indicates if a punctuation mark has been inserted

            public Highlight(int index)
            {
                SpaceIndex = index;
                IsSelected = false;
                HasPunctuation = false;
            }
        }

        public override int CalculateScore(bool isCorrect, int multiplier = 1)
        {
            return isCorrect ? 20 * multiplier : 0;
        }

        public void ToggleHighlight(int spaceIndex)
        {
            if (Highlights == null) return;

            for (var i = 0; i < Highlights.Count; i++)
            {
                var highlight = Highlights[i];
                highlight.IsSelected = highlight.SpaceIndex == spaceIndex;
                Highlights[i] = highlight;
            }
        }


        public void InsertPunctuation(string punctuation)
        {
            if (string.IsNullOrEmpty(this.UserText))
            {
                this.UserText = this.Sentence;
            }

            var selectedIndex = this.Highlights.FindIndex(h => h.IsSelected);
            if (selectedIndex == -1) return;

            var selectedHighlight = this.Highlights[selectedIndex];
            var insertionIndex = selectedHighlight.SpaceIndex;

            // If there's already punctuation at the previous character, replace it.
            if (insertionIndex > 0 && char.IsPunctuation(this.UserText[insertionIndex - 1]))
            {
                this.UserText = this.UserText.Remove(insertionIndex - 1, 1)
                                             .Insert(insertionIndex - 1, punctuation);
                selectedHighlight.IsSelected = false;
                this.Highlights[selectedIndex] = selectedHighlight;
            }
            else
            {
                this.UserText = this.UserText.Insert(insertionIndex, punctuation);
                var punctuationLength = punctuation.Length;

                // Update subsequent highlights' space indices.
                for (var i = 0; i < this.Highlights.Count; i++)
                {
                    if (this.Highlights[i].SpaceIndex >= insertionIndex)
                    {
                        var hl = this.Highlights[i];
                        hl.SpaceIndex += punctuationLength;
                        this.Highlights[i] = hl;
                    }
                }

                selectedHighlight.HasPunctuation = true;
                selectedHighlight.IsSelected = false;
            }
        }


        public void DeletePunctuation()
        {
            if (string.IsNullOrEmpty(this.UserText))
            {
                return;
            }

            var selectedIndex = this.Highlights.FindIndex(h => h.IsSelected);
            if (selectedIndex == -1) return;

            var selectedHighlight = this.Highlights[selectedIndex];
            var insertionIndex = selectedHighlight.SpaceIndex;

            if (insertionIndex > 0 && char.IsPunctuation(this.UserText[insertionIndex - 1]))
            {

                this.UserText = this.UserText.Remove(insertionIndex - 1, 1);

                // After removing character, all subsequent SpaceIndexes need to be adjusted
                for (var i = 0; i < this.Highlights.Count; i++)
                {
                    if (this.Highlights[i].SpaceIndex >= insertionIndex)
                    {
                        var hl = this.Highlights[i];
                        hl.SpaceIndex -= 1;
                        this.Highlights[i] = hl;
                    }
                }

                selectedHighlight.HasPunctuation = false;
                selectedHighlight.IsSelected = false;
            }
        }


        public bool IsAnswerCorrect()
        {
            if (string.IsNullOrEmpty(UserText) || string.IsNullOrEmpty(CorrectAnswer))
                return false;

            return UserText.Length == CorrectAnswer.Length &&
                   CorrectAnswer
                       .Select((ch, i) => new { Char = ch, Index = i })
                       .Where(x => char.IsPunctuation(x.Char))
                       .All(x => UserText[x.Index] == x.Char);
        }




    }
}

