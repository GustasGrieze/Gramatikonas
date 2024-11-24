using lithuanian_language_learning_tool.Data;
using lithuanian_language_learning_tool.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lithuanian_language_learning_tool.Services
{
    public interface IUserService
    {
        Task<User> GetOrCreateUserFromGoogleAsync(AuthenticationState authState);
        Task<User?> GetCurrentUserAsync(AuthenticationState authState);
        User CreateGuestUser();
        Task<bool> PromoteToAdminAsync(string userId);
        Task<bool> DemoteFromAdminAsync(string userId);

        Task<List<User>> GetTopUsersAsync(int topCount);

        Task UpdateUserAsync(User user);

    }

    public class UserService : IUserService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly HashSet<string> _adminEmails = new()
        {
            "3darijus@gmail.com",
            "testporator@gmail.com"
        };

        public UserService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<User> GetOrCreateUserFromGoogleAsync(AuthenticationState authState)
        {
            using var context = _contextFactory.CreateDbContext();

            var userClaims = authState.User;

            if (!userClaims.Identity?.IsAuthenticated ?? true)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            var googleId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = userClaims.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";
            var givenName = userClaims.FindFirst(ClaimTypes.GivenName)?.Value ?? "Unknown";
            var familyName = userClaims.FindFirst(ClaimTypes.Surname)?.Value ?? "User";
            var name = userClaims.FindFirst(ClaimTypes.Name)?.Value ?? $"{givenName} {familyName}";
            var profilePictureUrl = userClaims.FindFirst("picture")?.Value ?? string.Empty;

            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (existingUser != null)
            {
                var today = DateTime.UtcNow.Date;
                var lastLoginDate = existingUser.LastLoginAt.Date;

                if (lastLoginDate == today.AddDays(-1)) // Logged in yesterday
                {
                    existingUser.CurrentStreak++;
                    if (existingUser.CurrentStreak > existingUser.BestStreak)
                    {
                        existingUser.BestStreak = existingUser.CurrentStreak;
                    }
                }
                else if (lastLoginDate < today.AddDays(-1)) // Missed a day
                {
                    existingUser.CurrentStreak = 1;
                }

                existingUser.LastLoginAt = DateTime.UtcNow.ToLocalTime();
                await context.SaveChangesAsync(); // Update last login time
                return existingUser;
            }

            var newUser = new User
            {
                GoogleId = googleId,
                Email = email,
                GivenName = givenName,
                FamilyName = familyName,
                DisplayName = name,
                LastLoginAt = DateTime.UtcNow.ToLocalTime(),
                CreatedAt = DateTime.UtcNow.ToLocalTime(),
                ProfilePictureUrl = profilePictureUrl,
                Role = _adminEmails.Contains(email) ? UserRole.Admin : UserRole.RegisteredUser
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();
            return newUser;
        }

        public User CreateGuestUser()
        {

            var guestUser = new User
            {
                DisplayName = "Guest",
                Role = UserRole.Guest,
                Email = "",
                LastLoginAt = DateTime.UtcNow
            };

            return guestUser;
        }

        public async Task<User?> GetCurrentUserAsync(AuthenticationState authState)
        {
            using var context = _contextFactory.CreateDbContext();
            var userClaims = authState.User;

            if (!userClaims.Identity?.IsAuthenticated ?? true)
            {
                return CreateGuestUser();
            }

            var googleId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<bool> PromoteToAdminAsync(string userId)
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.Role = UserRole.Admin;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DemoteFromAdminAsync(string userId)
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.Role = UserRole.RegisteredUser;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateUserAsync(User user)
        {
            using var context = _contextFactory.CreateDbContext();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        public async Task<List<User>> GetTopUsersAsync(int topCount)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Users
                .OrderByDescending(u => u.HighScore)
                .Take(topCount)
                .ToListAsync();
        }
    }
}
