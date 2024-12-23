using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public enum UserRole
    {
        Guest,
        RegisteredUser,
        Admin
    }

    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Google Authentication related properties
        public string GoogleId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime LastLoginAt { get; set; }


        // Role management
        public UserRole Role { get; set; } = UserRole.RegisteredUser;
        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsGuest => Role == UserRole.Guest;


        // Application specific properties
        public int CurrentScore { get; set; }
        public int HighScore { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public TimeSpan TotalStudyTime { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }


        // Achievement tracking
        public List<UserAchievement> Achievements { get; set; } = new();


        // Progress tracking
        [NotMapped]
        public Dictionary<string, int> LessonProgress { get; set; } = new();
        public List<PracticeSession> PracticeSessions { get; set; } = new();


        // Learning statistics
        public int CorrectAnswers { get; set; }
        public int TotalAttempts { get; set; }
        public double AccuracyRate => TotalAttempts > 0 ? (double)CorrectAnswers / TotalAttempts * 100 : 0;

        public bool HasPermission(string permission)
        {
            return Role switch
            {
                UserRole.Admin => true,
                UserRole.RegisteredUser => IsRegisteredUserPermission(permission),
                UserRole.Guest => IsGuestPermission(permission),
                _ => false
            };
        }

        private bool IsRegisteredUserPermission(string permission)
        {
            var registeredUserPermissions = new HashSet<string>
            {
                "view_profile",
                "save_progress",
                "participate_in_lessons",
                "earn_achievements",
                "view_statistics"
            };

            return registeredUserPermissions.Contains(permission);
        }

        private bool IsGuestPermission(string permission)
        {
            var guestPermissions = new HashSet<string>
            {
                "view_public_content",
                "try_demo_lesson"
            };

            return guestPermissions.Contains(permission);
        }
    }

    public class UserAchievement
    {
        [Key]
        public int Id { get; set; } // Primary key for EF

        public string AchievementId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime UnlockedAt { get; set; }

        // Foreign key to link to User
        public string UserId { get; set; }
        public User User { get; set; }
    }



    public class PracticeSession
    {
        [Key]
        public int Id { get; set; } // Primary key for EF

        public DateTime SessionDate { get; set; }
        public TimeSpan Duration { get; set; }
        public int ScoreEarned { get; set; }
        public string LessonType { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }

        // Foreign key to link to User
        public string UserId { get; set; }
        public User User { get; set; }
    }
}