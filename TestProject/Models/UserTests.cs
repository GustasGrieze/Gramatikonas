using DataAccess.Models;
using System;

namespace TestProject.Models
{
    public class UserTests
    {
        [Fact]
        public void Admin_ShouldHaveAllPermissions()
        {
            // Arrange
            var user = new User { Role = UserRole.Admin };

            // Act
            var canViewProfile = user.HasPermission("view_profile");
            var canSaveProgress = user.HasPermission("save_progress");
            var canTryDemoLesson = user.HasPermission("try_demo_lesson");

            // Assert
            Assert.True(canViewProfile);
            Assert.True(canSaveProgress);
            Assert.True(canTryDemoLesson);
        }

        [Theory]
        [InlineData("view_profile", true)]
        [InlineData("save_progress", true)]
        [InlineData("participate_in_lessons", true)]
        [InlineData("earn_achievements", true)]
        [InlineData("view_statistics", true)]
        [InlineData("try_demo_lesson", false)] // Not a registered user permission
        public void RegisteredUser_ShouldHaveCorrectPermissions(string permission, bool expected)
        {
            // Arrange
            var user = new User { Role = UserRole.RegisteredUser };

            // Act
            var hasPermission = user.HasPermission(permission);

            // Assert
            Assert.Equal(expected, hasPermission);
        }

        [Theory]
        [InlineData("view_public_content", true)]
        [InlineData("try_demo_lesson", true)]
        [InlineData("view_profile", false)] // Not a guest permission
        [InlineData("save_progress", false)] // Not a guest permission
        public void Guest_ShouldHaveCorrectPermissions(string permission, bool expected)
        {
            // Arrange
            var user = new User { Role = UserRole.Guest };

            // Act
            var hasPermission = user.HasPermission(permission);

            // Assert
            Assert.Equal(expected, hasPermission);
        }

        [Fact]
        public void UserRole_ShouldCorrectlySetIsAdminAndIsGuest()
        {
            // Arrange
            var adminUser = new User { Role = UserRole.Admin };
            var guestUser = new User { Role = UserRole.Guest };

            // Assert
            Assert.True(adminUser.IsAdmin);
            Assert.False(adminUser.IsGuest);

            Assert.False(guestUser.IsAdmin);
            Assert.True(guestUser.IsGuest);
        }

        [Theory]
        [InlineData(50, 100, 50.0)] // 50% accuracy
        [InlineData(0, 100, 0.0)] // 0% accuracy
        [InlineData(25, 25, 100.0)] // 100% accuracy
        [InlineData(0, 0, 0.0)] // No attempts, accuracy should be 0
        public void AccuracyRate_ShouldCalculateCorrectly(int correctAnswers, int totalAttempts, double expectedAccuracy)
        {
            // Arrange
            var user = new User
            {
                CorrectAnswers = correctAnswers,
                TotalAttempts = totalAttempts
            };

            // Act
            var accuracyRate = user.AccuracyRate;

            // Assert
            Assert.Equal(expectedAccuracy, accuracyRate);
        }

        [Fact]
        public void UserScore_ShouldBeManagedCorrectly()
        {
            // Arrange
            var user = new User { CurrentScore = 50, HighScore = 100 };

            // Act
            user.CurrentScore += 30;
            if (user.CurrentScore > user.HighScore)
            {
                user.HighScore = user.CurrentScore;
            }

            // Assert
            Assert.Equal(80, user.CurrentScore);
            Assert.Equal(100, user.HighScore); // High score remains unchanged
        }

        [Fact]
        public void UserStreak_ShouldUpdateCorrectly()
        {
            // Arrange
            var user = new User { CurrentStreak = 5, BestStreak = 10 };

            // Act
            user.CurrentStreak += 1;
            if (user.CurrentStreak > user.BestStreak)
            {
                user.BestStreak = user.CurrentStreak;
            }

            // Assert
            Assert.Equal(6, user.CurrentStreak);
            Assert.Equal(10, user.BestStreak); // Best streak remains unchanged
        }

        [Fact]
        public void User_ShouldAddAchievementsCorrectly()
        {
            // Arrange
            var user = new User();
            var achievement = new UserAchievement
            {
                AchievementId = "achievement1",
                Name = "First Steps",
                Description = "Complete the first lesson",
                UnlockedAt = DateTime.UtcNow
            };

            // Act
            user.Achievements.Add(achievement);

            // Assert
            Assert.Single(user.Achievements);
            Assert.Equal("First Steps", user.Achievements[0].Name);
        }

        [Fact]
        public void LessonProgress_ShouldTrackCorrectly()
        {
            // Arrange
            var user = new User();
            var lessonId = "lesson1";

            // Act
            user.LessonProgress[lessonId] = 50; // 50% progress

            // Assert
            Assert.True(user.LessonProgress.ContainsKey(lessonId));
            Assert.Equal(50, user.LessonProgress[lessonId]);
        }


        [Fact]
        public void User_ShouldAddPracticeSessionsCorrectly()
        {
            // Arrange
            var user = new User();
            var session = new PracticeSession
            {
                SessionDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(30),
                ScoreEarned = 100,
                LessonType = "Grammar",
                CorrectAnswers = 20,
                TotalQuestions = 25
            };

            // Act
            user.PracticeSessions.Add(session);

            // Assert
            Assert.Single(user.PracticeSessions);
            Assert.Equal("Grammar", user.PracticeSessions[0].LessonType);
            Assert.Equal(100, user.PracticeSessions[0].ScoreEarned);
        }

    }
}
