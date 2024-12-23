using DataAccess.Models;
using System;

namespace TestProject.Services
{
    public class StreakTests
    {
        [Fact]
        public void IncrementStreak_WhenUserLogsInConsecutiveDays()
        {
            var user = new User
            {
                CurrentStreak = 3,
                LastLoginAt = DateTime.UtcNow.AddDays(-1) // Last login was yesterday
            };

            // Simulate login
            UpdateUserStreak(user);

            Assert.Equal(4, user.CurrentStreak); // Streak should increment
        }

        [Fact]
        public void ResetStreak_WhenUserSkipsADay()
        {

            var user = new User
            {
                CurrentStreak = 5,
                LastLoginAt = DateTime.UtcNow.AddDays(-2) // Last login was 2 days ago
            };

            // Simulate login
            UpdateUserStreak(user);

            Assert.Equal(0, user.CurrentStreak); // Streak should reset
        }

        [Fact]
        public void StartNewStreak_WhenUserLogsInForTheFirstTime()
        {
            var user = new User
            {
                CurrentStreak = 0,
                LastLoginAt = DateTime.MinValue // Never logged in before
            };

            // Simulate login
            UpdateUserStreak(user);

            Assert.Equal(0, user.CurrentStreak); // Streak starts at 1
        }

        private void UpdateUserStreak(User user)
        {
            var today = DateTime.UtcNow.Date;
            var lastLoginDate = user.LastLoginAt.Date;

            if (lastLoginDate == today.AddDays(-1)) // Logged in yesterday
            {
                user.CurrentStreak++;
            }
            else if (lastLoginDate < today.AddDays(-1)) // Missed a day
            {
                user.CurrentStreak = 0; // Reset streak
            }

            user.LastLoginAt = DateTime.UtcNow; // Update login time
        }
    }

}
