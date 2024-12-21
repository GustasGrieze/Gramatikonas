using lithuanian_language_learning_tool.Models;
using lithuanian_language_learning_tool.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace lithuanian_language_learning_tool.Components.Pages
{
    public abstract class TaskBase<TTask> : ComponentBase where TTask : CustomTask, new()
    {
        [Inject]
        protected IUserService UserService { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        protected ITaskService<TTask> TaskService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }


        protected int score = 0;
        protected int correctAnswersCount = 0;
        protected Timer timer = new Timer();
        protected DateTime startTime;
        protected List<TTask> tasks = new List<TTask>();
        protected int currentTaskIndex = 0;
        protected TTask currentTask;




        //-----
        protected string? feedbackMessage;
        protected string feedbackClass = "";
        //-----


        protected bool reviewMode = false;
        protected bool startExercise = false;
        protected bool showFeedback = false;
        protected bool showSummary = false;


        protected bool showFlash = false;
        protected bool _lastAnswerCorrect = false;

        protected string selectedTopic = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadSelectedTopicAsync();
            await LoadTasksFromDatabaseAsync();
        }

        protected async Task LoadSelectedTopicAsync()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("topic", out var topicValues))
            {
                selectedTopic = topicValues.FirstOrDefault();
            }

            await Task.CompletedTask;
        }

        protected async Task LoadTasksFromDatabaseAsync()
        {
            if (!string.IsNullOrEmpty(selectedTopic))
            {
                tasks = await TaskService.GetRandomTasksAsync(5, selectedTopic);
            }
            else
            {
                tasks = await TaskService.GetRandomTasksAsync(5);
            }

            if (tasks == null || tasks.Count == 0)
            {
                tasks = new List<TTask>();
            }
        }

        protected void InitTasks()
        {
            foreach (var task in tasks)
            {
                task.UserText = task.Sentence;
            }
        }

        protected void LoadCustomTasks(string fileContent)
        {
            tasks = ParseUploadedTasks(fileContent);
            StartExercise();
        }
        protected List<TTask> ParseUploadedTasks(string fileContent)
        {
            var uploadedTasks = JsonSerializer.Deserialize<List<TTask>>(fileContent);
            if (uploadedTasks != null && uploadedTasks.Count > 0)
            {
                return uploadedTasks;
            }
            else
            {
                throw new Exception("Failed to parse tasks from the file.");
            }
        }

        /**
         * Be carful as this selectedAnswer arguement is only optional due to different implentation in Spelling and Punctuation but is actually necessary.
         */
        protected abstract bool IsAnswerCorrect(string selectedAnswer);
        protected async Task CheckAnswer(string selectedAnswer = "")
        {
            showFeedback = true;
            currentTask.TaskStatus = IsAnswerCorrect(selectedAnswer);
            feedbackMessage = currentTask.TaskStatus
                ? "Teisingai!"
                : $"Neteisingai. Teisingas atsakymas: {tasks[currentTaskIndex].CorrectAnswer}";
            correctAnswersCount = currentTask.TaskStatus
                ? correctAnswersCount + 1
                : correctAnswersCount;
            currentTask.TaskStatus = currentTask.TaskStatus;

            _lastAnswerCorrect = currentTask.TaskStatus;

            score += currentTask.CalculateScore(currentTask.TaskStatus, multiplier: 2); // simple scoring system - needs improvement (time based score)

            await NextTask();
            showFlash = true;
            await Task.Delay(300);
            showFlash = false;
            
            StateHasChanged();


        }

        protected virtual async Task StartExercise(bool refetchNewTasks = false)
        {
            startTime = DateTime.Now;

            if (refetchNewTasks)
            {
                await LoadTasksFromDatabaseAsync();  
            }

            score = 0;
            currentTaskIndex = 0;
            correctAnswersCount = 0;
            feedbackMessage = null;
            showSummary = false;
            showFeedback = false;
            reviewMode = false;
            startExercise = true;

            if (tasks.Count > 0)
            {
                currentTask = tasks[currentTaskIndex];
                currentTask.UserText = currentTask.Sentence;
                InitTasks();  
            }

            RestartTasks(); 
            StateHasChanged();  
        }


        protected void RestartTasks()
        {

            currentTask.TaskStatus = false;
            foreach (var task in tasks)
            {
                task.Reset();
            }
            StateHasChanged();
        }

        protected void GoToTask(int taskIndex = 0)
        {
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                currentTaskIndex = taskIndex;
                currentTask = tasks[currentTaskIndex];
                feedbackMessage = null;
                showSummary = false;
                reviewMode = true;
            }
            else
            {
                feedbackMessage = "Invalid task index.";
            }
        }

        protected async Task  TimerOut()
        {
            await EndExercise();
        }
        protected async Task SkipTask()
        {
            await NextTask();
        }

        protected virtual async Task NextTask()
        {
            if (currentTaskIndex < tasks.Count - 1)
            {
                timer.Dispose();
                timer.ResetTimer();

                currentTaskIndex++;
                currentTask = tasks[currentTaskIndex];
                feedbackMessage = null;
                showFeedback = false;
                currentTask.TaskStatus = false;
                
                StateHasChanged();

            }
            else
            {
                await EndExercise();
            }
        }
        protected virtual async Task EndExercise()
        {
            showSummary = true;

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var currentUser = await UserService.GetCurrentUserAsync(authState);

            if(currentUser != null &&!currentUser.IsGuest)
            {
                await UpdateUserStats(currentUser);
                PracticeSession practiceSession = new PracticeSession();
                practiceSession.SessionDate = DateTime.Now;
                practiceSession.Duration = DateTime.Now - startTime;
                practiceSession.ScoreEarned = score;
                practiceSession.LessonType = tasks[0] is PunctuationTask ? "Punctuation" : "Spelling";
                practiceSession.CorrectAnswers = correctAnswersCount;
                practiceSession.TotalQuestions = tasks.Count;
                practiceSession.UserId = currentUser.Id; // Assuming User has an Id property
                await UserService.RecordPracticeSession(currentUser, practiceSession);
            }
       
            StateHasChanged();
        }
        protected async Task UpdateUserStats(User currentUser)
        {
            if (currentUser != null)
            {
               UpdateUserHighScore(currentUser);
               currentUser.TotalLessonsCompleted += tasks.Count;
               await UserService.UpdateUserAsync(currentUser);

            }
        }

        protected void UpdateUserHighScore(User currentUser)
        {
            if (currentUser != null)
            {
                if (score > currentUser.HighScore)
                {
                    currentUser.HighScore = score;
                }
            }
        } 


    }
}
