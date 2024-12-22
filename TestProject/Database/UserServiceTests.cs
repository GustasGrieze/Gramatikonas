using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using TestProject.Database;
using Xunit;

namespace TestProject.Database
{
    public class UserServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private readonly IUserService _userService;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public UserServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _userService = _fixture.ServiceProvider.GetRequiredService<IUserService>();
            _contextFactory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // Helper method to create a User with all required properties
        private User CreateUser(
            string id,
            string googleId,
            string email,
            string displayName,
            string givenName,
            string familyName,
            UserRole role = UserRole.RegisteredUser)
        {
            return new User
            {
                Id = id,
                GoogleId = googleId,
                Email = email,
                DisplayName = displayName,
                GivenName = givenName,
                FamilyName = familyName,
                Role = role,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
        }

        #region GetOrCreateUserFromGoogleAsync Tests

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.GetOrCreateUserFromGoogleAsync(authState));
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_NewUser_CreatesUserSuccessfully()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-123"),
                new Claim(ClaimTypes.Email, "newuser@example.com"),
                new Claim(ClaimTypes.GivenName, "New"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Name, "New User"),
                new Claim("picture", "http://example.com/picture.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(user);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("google-id-123", result.GoogleId);
            Assert.Equal("newuser@example.com", result.Email);
            Assert.Equal("New", result.GivenName);
            Assert.Equal("User", result.FamilyName);
            Assert.Equal("New User", result.DisplayName);
            Assert.Equal("http://example.com/picture.jpg", result.ProfilePictureUrl);
            Assert.Equal(UserRole.RegisteredUser, result.Role);
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
            Assert.True((DateTime.UtcNow - result.CreatedAt.ToUniversalTime()).TotalSeconds < 60);
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_ExistingUser_UpdatesStreakCorrectly_LoggedInYesterday()
        {
            // Arrange
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var existingUser = CreateUser(
                id: "user-456",
                googleId: "google-id-456",
                email: "existinguser@example.com",
                displayName: "Existing User",
                givenName: "Existing",
                familyName: "User"
            );
            existingUser.LastLoginAt = yesterday;
            existingUser.CreatedAt = yesterday;
            existingUser.CurrentStreak = 2;
            existingUser.BestStreak = 3;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-456"),
                new Claim(ClaimTypes.Email, "existinguser@example.com"),
                new Claim(ClaimTypes.GivenName, "Existing"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Name, "Existing User"),
                new Claim("picture", "http://example.com/picture.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.CurrentStreak); // Incremented
            Assert.Equal(3, result.BestStreak); // No change
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_ExistingUser_MissedDay_ResetsCurrentStreak()
        {
            // Arrange
            var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
            var existingUser = CreateUser(
                id: "user-789",
                googleId: "google-id-789",
                email: "misseddayuser@example.com",
                displayName: "Missed Day",
                givenName: "Missed",
                familyName: "Day"
            );
            existingUser.LastLoginAt = twoDaysAgo;
            existingUser.CreatedAt = twoDaysAgo;
            existingUser.CurrentStreak = 5;
            existingUser.BestStreak = 5;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-789"),
                new Claim(ClaimTypes.Email, "misseddayuser@example.com"),
                new Claim(ClaimTypes.GivenName, "Missed"),
                new Claim(ClaimTypes.Surname, "Day"),
                new Claim(ClaimTypes.Name, "Missed Day"),
                new Claim("picture", "http://example.com/picture.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentStreak); // Reset to 1
            Assert.Equal(5, result.BestStreak); // No change
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_AdminUser_AssignsAdminRole()
        {
            // Arrange
            var adminEmail = "3darijus@gmail.com"; // From _adminEmails
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-admin"),
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.GivenName, "Admin"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Name, "Admin User"),
                new Claim("picture", "http://example.com/adminpicture.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(UserRole.Admin, result.Role);
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_UserAlreadyExists_UpdatesLastLoginAtOnlyIfNecessary()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var existingUser = CreateUser(
                id: "user-777",
                googleId: "google-id-777",
                email: "existing777@example.com",
                displayName: "Existing SevenSevenSeven",
                givenName: "Existing",
                familyName: "SevenSevenSeven"
            );
            existingUser.LastLoginAt = today; // Already logged in today
            existingUser.CreatedAt = today.AddDays(-10);
            existingUser.CurrentStreak = 5;
            existingUser.BestStreak = 5;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-777"),
                new Claim(ClaimTypes.Email, "existing777@example.com"),
                new Claim(ClaimTypes.GivenName, "Existing"),
                new Claim(ClaimTypes.Surname, "SevenSevenSeven"),
                new Claim(ClaimTypes.Name, "Existing SevenSevenSeven"),
                new Claim("picture", "http://example.com/existing777.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.CurrentStreak); // No increment
            Assert.Equal(5, result.BestStreak); // No change
            Assert.Equal(today, result.LastLoginAt.Date); // No update needed
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_UserLoggedInAfterMissedDay_SetsCurrentStreakTo1()
        {
            // Arrange
            var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);
            var existingUser = CreateUser(
                id: "user-888",
                googleId: "google-id-888",
                email: "misseduser@example.com",
                displayName: "Missed User",
                givenName: "Missed",
                familyName: "User"
            );
            existingUser.LastLoginAt = threeDaysAgo;
            existingUser.CreatedAt = threeDaysAgo;
            existingUser.CurrentStreak = 4;
            existingUser.BestStreak = 5;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-888"),
                new Claim(ClaimTypes.Email, "misseduser@example.com"),
                new Claim(ClaimTypes.GivenName, "Missed"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Name, "Missed User"),
                new Claim("picture", "http://example.com/misseduser.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentStreak); // Reset to 1
            Assert.Equal(5, result.BestStreak); // No change
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
        }

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_UserWithMissingClaims_AssignsDefaultValues()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-654")
                // Missing Email, GivenName, Surname, Name, picture
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(user);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("google-id-654", result.GoogleId);
            Assert.Equal("unknown@example.com", result.Email);
            Assert.Equal("Unknown", result.GivenName);
            Assert.Equal("User", result.FamilyName);
            Assert.Equal("Unknown User", result.DisplayName);
            Assert.Equal(string.Empty, result.ProfilePictureUrl);
            Assert.Equal(UserRole.RegisteredUser, result.Role);
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
            Assert.True((DateTime.UtcNow - result.CreatedAt.ToUniversalTime()).TotalSeconds < 60);
        }

        #endregion

        #region CreateGuestUser Test

        [Fact]
        public void CreateGuestUser_ReturnsGuestUser()
        {
            // Act
            var guestUser = _userService.CreateGuestUser();

            // Assert
            Assert.NotNull(guestUser);
            Assert.Equal("Guest", guestUser.DisplayName);
            Assert.Equal(UserRole.Guest, guestUser.Role);
            Assert.True((DateTime.UtcNow - guestUser.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
            Assert.True((DateTime.UtcNow - guestUser.CreatedAt.ToUniversalTime()).TotalSeconds < 60);
        }

        #endregion

        #region GetCurrentUserAsync Tests

        [Fact]
        public async Task GetCurrentUserAsync_UserAuthenticated_ReturnsUser()
        {
            // Arrange
            var existingUser = CreateUser(
                id: "user-999",
                googleId: "google-id-999",
                email: "currentuser@example.com",
                displayName: "Current User",
                givenName: "Current",
                familyName: "User",
                role: UserRole.RegisteredUser
            );

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-999"),
                new Claim(ClaimTypes.Email, "currentuser@example.com"),
                new Claim(ClaimTypes.GivenName, "Current"),
                new Claim(ClaimTypes.Surname, "User"),
                new Claim(ClaimTypes.Name, "Current User"),
                new Claim("picture", "http://example.com/currentuser.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetCurrentUserAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("currentuser@example.com", result.Email);
            Assert.Equal("Current", result.GivenName);
            Assert.Equal("User", result.FamilyName);
            Assert.Equal("Current User", result.DisplayName);
        }

        [Fact]
        public async Task GetCurrentUserAsync_UserNotAuthenticated_ReturnsGuestUser()
        {
            // Arrange
            var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            // Act
            var guestUser = await _userService.GetCurrentUserAsync(authState);

            // Assert
            Assert.NotNull(guestUser);
            Assert.Equal("Guest", guestUser.DisplayName);
            Assert.Equal(UserRole.Guest, guestUser.Role);
            Assert.True((DateTime.UtcNow - guestUser.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
            Assert.True((DateTime.UtcNow - guestUser.CreatedAt.ToUniversalTime()).TotalSeconds < 60);
        }

        #endregion

        #region PromoteToAdminAsync Tests

        [Fact]
        public async Task PromoteToAdminAsync_UserExists_PromotesSuccessfully()
        {
            // Arrange
            var user = CreateUser(
                id: "user-1",
                googleId: "google-id-user1",
                email: "user1@example.com",
                displayName: "User One",
                givenName: "User",
                familyName: "One"
            );

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _userService.PromoteToAdminAsync("user-1");

            // Assert
            Assert.True(result);
            using (var context = _contextFactory.CreateDbContext())
            {
                var updatedUser = await context.Users.FindAsync("user-1");
                Assert.Equal(UserRole.Admin, updatedUser.Role);
            }
        }

        [Fact]
        public async Task PromoteToAdminAsync_UserDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _userService.PromoteToAdminAsync("non-existent-user");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DemoteFromAdminAsync Tests

        [Fact]
        public async Task DemoteFromAdminAsync_UserExists_DemotesSuccessfully()
        {
            // Arrange
            var user = CreateUser(
                id: "admin-1",
                googleId: "google-id-admin-1",
                email: "admin1@example.com",
                displayName: "Admin One",
                givenName: "Admin",
                familyName: "One",
                role: UserRole.Admin
            );

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _userService.DemoteFromAdminAsync("admin-1");

            // Assert
            Assert.True(result);
            using (var context = _contextFactory.CreateDbContext())
            {
                var updatedUser = await context.Users.FindAsync("admin-1");
                Assert.Equal(UserRole.RegisteredUser, updatedUser.Role);
            }
        }

        [Fact]
        public async Task DemoteFromAdminAsync_UserDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _userService.DemoteFromAdminAsync("non-existent-admin");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateUserAsync Test

        [Fact]
        public async Task UpdateUserAsync_UpdatesUserSuccessfully()
        {
            // Arrange
            var user = CreateUser(
                id: "user-2",
                googleId: "google-id-user2",
                email: "user2@example.com",
                displayName: "User Two",
                givenName: "User",
                familyName: "Two",
                role: UserRole.RegisteredUser
            );

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Modify user
            user.DisplayName = "Updated User Two";
            user.Role = UserRole.Admin;

            // Act
            await _userService.UpdateUserAsync(user);

            // Assert
            using (var context = _contextFactory.CreateDbContext())
            {
                var updatedUser = await context.Users.FindAsync("user-2");
                Assert.Equal("Updated User Two", updatedUser.DisplayName);
                Assert.Equal(UserRole.Admin, updatedUser.Role);
            }
        }

        #endregion

        #region GetTopUsersByHighScoreAsync Tests

        [Fact]
        public async Task GetTopUsersByHighScoreAsync_ReturnsTopUsers()
        {
            // Arrange
            var users = new List<User>
            {
                CreateUser(
                    id: "user1",
                    googleId: "google-id-user1",
                    email: "user1@example.com",
                    displayName: "User One",
                    givenName: "User",
                    familyName: "One"
                ),
                CreateUser(
                    id: "user2",
                    googleId: "google-id-user2",
                    email: "user2@example.com",
                    displayName: "User Two",
                    givenName: "User",
                    familyName: "Two"
                ),
                CreateUser(
                    id: "user3",
                    googleId: "google-id-user3",
                    email: "user3@example.com",
                    displayName: "User Three",
                    givenName: "User",
                    familyName: "Three"
                ),
                CreateUser(
                    id: "user4",
                    googleId: "google-id-user4",
                    email: "user4@example.com",
                    displayName: "User Four",
                    givenName: "User",
                    familyName: "Four"
                )
            };

            users[0].HighScore = 100;
            users[1].HighScore = 200;
            users[2].HighScore = 150;
            users[3].HighScore = 250;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }

            // Act
            var topUsers = await _userService.GetTopUsersByHighScoreAsync();

            // Assert
            Assert.Equal(4, topUsers.Count);
            Assert.Equal("user4@example.com", topUsers[0].Email); // Highest score first
            Assert.Equal("user2@example.com", topUsers[1].Email);
            Assert.Equal("user3@example.com", topUsers[2].Email);
            Assert.Equal("user1@example.com", topUsers[3].Email);
        }

        [Fact]
        public async Task GetTopUsersByHighScoreAsync_LessThan10Users_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                CreateUser(
                    id: "user1",
                    googleId: "google-id-user1",
                    email: "user1@example.com",
                    displayName: "User One",
                    givenName: "User",
                    familyName: "One"
                ),
                CreateUser(
                    id: "user2",
                    googleId: "google-id-user2",
                    email: "user2@example.com",
                    displayName: "User Two",
                    givenName: "User",
                    familyName: "Two"
                ),
                CreateUser(
                    id: "user3",
                    googleId: "google-id-user3",
                    email: "user3@example.com",
                    displayName: "User Three",
                    givenName: "User",
                    familyName: "Three"
                )
            };

            users[0].HighScore = 100;
            users[1].HighScore = 200;
            users[2].HighScore = 150;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }

            // Act
            var topUsers = await _userService.GetTopUsersByHighScoreAsync();

            // Assert
            Assert.Equal(3, topUsers.Count);
            Assert.Equal("user2@example.com", topUsers[0].Email);
            Assert.Equal("user3@example.com", topUsers[1].Email);
            Assert.Equal("user1@example.com", topUsers[2].Email);
        }

        #endregion

        #region GetTopUsersByCurrentStreakAsync Test

        [Fact]
        public async Task GetTopUsersByCurrentStreakAsync_ReturnsTopUsers()
        {
            // Arrange
            var users = new List<User>
            {
                CreateUser(
                    id: "user1",
                    googleId: "google-id-user1",
                    email: "user1@example.com",
                    displayName: "User One",
                    givenName: "User",
                    familyName: "One"
                ),
                CreateUser(
                    id: "user2",
                    googleId: "google-id-user2",
                    email: "user2@example.com",
                    displayName: "User Two",
                    givenName: "User",
                    familyName: "Two"
                ),
                CreateUser(
                    id: "user3",
                    googleId: "google-id-user3",
                    email: "user3@example.com",
                    displayName: "User Three",
                    givenName: "User",
                    familyName: "Three"
                ),
                CreateUser(
                    id: "user4",
                    googleId: "google-id-user4",
                    email: "user4@example.com",
                    displayName: "User Four",
                    givenName: "User",
                    familyName: "Four"
                )
            };

            users[0].CurrentStreak = 5;
            users[1].CurrentStreak = 10;
            users[2].CurrentStreak = 7;
            users[3].CurrentStreak = 3;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }

            // Act
            var topUsers = await _userService.GetTopUsersByCurrentStreakAsync();

            // Assert
            Assert.Equal(4, topUsers.Count);
            Assert.Equal("user2@example.com", topUsers[0].Email); // Highest streak first
            Assert.Equal("user3@example.com", topUsers[1].Email);
            Assert.Equal("user1@example.com", topUsers[2].Email);
            Assert.Equal("user4@example.com", topUsers[3].Email);
        }

        #endregion

        #region GetTopUsersByBestStreakAsync Test

        [Fact]
        public async Task GetTopUsersByBestStreakAsync_ReturnsTopUsers()
        {
            // Arrange
            var users = new List<User>
            {
                CreateUser(
                    id: "user1",
                    googleId: "google-id-user1",
                    email: "user1@example.com",
                    displayName: "User One",
                    givenName: "User",
                    familyName: "One"
                ),
                CreateUser(
                    id: "user2",
                    googleId: "google-id-user2",
                    email: "user2@example.com",
                    displayName: "User Two",
                    givenName: "User",
                    familyName: "Two"
                ),
                CreateUser(
                    id: "user3",
                    googleId: "google-id-user3",
                    email: "user3@example.com",
                    displayName: "User Three",
                    givenName: "User",
                    familyName: "Three"
                ),
                CreateUser(
                    id: "user4",
                    googleId: "google-id-user4",
                    email: "user4@example.com",
                    displayName: "User Four",
                    givenName: "User",
                    familyName: "Four"
                )
            };

            users[0].BestStreak = 15;
            users[1].BestStreak = 20;
            users[2].BestStreak = 10;
            users[3].BestStreak = 25;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }

            // Act
            var topUsers = await _userService.GetTopUsersByBestStreakAsync();

            // Assert
            Assert.Equal(4, topUsers.Count);
            Assert.Equal("user4@example.com", topUsers[0].Email); // Highest best streak first
            Assert.Equal("user2@example.com", topUsers[1].Email);
            Assert.Equal("user1@example.com", topUsers[2].Email);
            Assert.Equal("user3@example.com", topUsers[3].Email);
        }

        #endregion

        #region GetTopUsersByLessonsCompletedAsync Test

        [Fact]
        public async Task GetTopUsersByLessonsCompletedAsync_ReturnsTopUsers()
        {
            // Arrange
            var users = new List<User>
            {
                CreateUser(
                    id: "user1",
                    googleId: "google-id-user1",
                    email: "user1@example.com",
                    displayName: "User One",
                    givenName: "User",
                    familyName: "One"
                ),
                CreateUser(
                    id: "user2",
                    googleId: "google-id-user2",
                    email: "user2@example.com",
                    displayName: "User Two",
                    givenName: "User",
                    familyName: "Two"
                ),
                CreateUser(
                    id: "user3",
                    googleId: "google-id-user3",
                    email: "user3@example.com",
                    displayName: "User Three",
                    givenName: "User",
                    familyName: "Three"
                ),
                CreateUser(
                    id: "user4",
                    googleId: "google-id-user4",
                    email: "user4@example.com",
                    displayName: "User Four",
                    givenName: "User",
                    familyName: "Four"
                )
            };

            users[0].TotalLessonsCompleted = 30;
            users[1].TotalLessonsCompleted = 50;
            users[2].TotalLessonsCompleted = 40;
            users[3].TotalLessonsCompleted = 20;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }

            // Act
            var topUsers = await _userService.GetTopUsersByLessonsCompletedAsync();

            // Assert
            Assert.Equal(4, topUsers.Count);
            Assert.Equal("user2@example.com", topUsers[0].Email); // Highest lessons first
            Assert.Equal("user3@example.com", topUsers[1].Email);
            Assert.Equal("user1@example.com", topUsers[2].Email);
            Assert.Equal("user4@example.com", topUsers[3].Email);
        }

        #endregion

        #region RecordPracticeSession Tests

        [Fact]
        public async Task RecordPracticeSession_AddsSessionAndUpdatesUserStats()
        {
            // Arrange
            var user = CreateUser(
                id: "user-3",
                googleId: "google-id-user3",
                email: "user3@example.com",
                displayName: "User Three",
                givenName: "User",
                familyName: "Three"
            );
            user.TotalStudyTime = TimeSpan.FromHours(1);
            user.TotalAttempts = 10;
            user.CorrectAnswers = 8;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            var session = new PracticeSession
            {
                Duration = TimeSpan.FromMinutes(30),
                TotalQuestions = 5,
                CorrectAnswers = 4,
                UserId = "user-3",
                LessonType = "Listening",
                SessionDate = DateTime.UtcNow,
                ScoreEarned = 120
            };

            // Act
            await _userService.RecordPracticeSession(user, session);

            // Assert
            using (var context = _contextFactory.CreateDbContext())
            {
                var updatedUser = await context.Users
                    .Include(u => u.PracticeSessions)
                    .FirstOrDefaultAsync(u => u.Id == "user-3");
                Assert.NotNull(updatedUser);
                Assert.Single(updatedUser.PracticeSessions);
                Assert.Equal(TimeSpan.FromHours(1.5), updatedUser.TotalStudyTime);
                Assert.Equal(15, updatedUser.TotalAttempts);
                Assert.Equal(12, updatedUser.CorrectAnswers);

                var savedSession = updatedUser.PracticeSessions.First();
                Assert.Equal("Listening", savedSession.LessonType);
                Assert.Equal(session.SessionDate, savedSession.SessionDate);
                Assert.Equal(120, savedSession.ScoreEarned);
            }
        }


        [Fact]
        public async Task RecordPracticeSession_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var user = CreateUser(
                id: "user-6",
                googleId: "google-id-user6",
                email: "user6@example.com",
                displayName: "User Six",
                givenName: "User",
                familyName: "Six"
            );

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // With the null check in UserService.RecordPracticeSession:
            // This should now throw ArgumentNullException instead of NullReferenceException.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _userService.RecordPracticeSession(user, null));
        }

        #endregion

        #region GetPracticeSessionsForUser Test

        [Fact]
        public async Task GetPracticeSessionsForUser_ReturnsSessions()
        {
            // Arrange
            var user = CreateUser(
                id: "user-4",
                googleId: "google-id-user4",
                email: "user4@example.com",
                displayName: "User Four",
                givenName: "User",
                familyName: "Four"
            );

            var sessions = new List<PracticeSession>
            {
                new PracticeSession
                {
                    UserId = "user-4",
                    Duration = TimeSpan.FromMinutes(20),
                    TotalQuestions = 10,
                    CorrectAnswers = 7,
                    LessonType = "Reading"
                },
                new PracticeSession
                {
                    UserId = "user-4",
                    Duration = TimeSpan.FromMinutes(30),
                    TotalQuestions = 15,
                    CorrectAnswers = 12,
                    LessonType = "Vocabulary"
                }
            };

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(user);
                context.PracticeSessions.AddRange(sessions);
                await context.SaveChangesAsync();
            }

            // Act
            var userSessions = await _userService.GetPracticeSessionsForUser("user-4");

            // Assert
            Assert.Equal(2, userSessions.Count);
            Assert.All(userSessions, s => Assert.Equal("user-4", s.UserId));
        }


        #endregion

        #region GetOrCreateUserFromGoogleAsync Additional Tests

        [Fact]
        public async Task GetOrCreateUserFromGoogleAsync_UserAlreadyExists_UpdatesLastLoginAt()
        {
            // Arrange
            var existingUser = CreateUser(
                id: "user-321",
                googleId: "google-id-321",
                email: "existingloginuser@example.com",
                displayName: "Existing LoginUser",
                givenName: "Existing",
                familyName: "LoginUser"
            );
            existingUser.LastLoginAt = DateTime.UtcNow.Date.AddDays(-1);
            existingUser.CreatedAt = DateTime.UtcNow.Date.AddDays(-2);
            existingUser.CurrentStreak = 1;
            existingUser.BestStreak = 1;

            using (var context = _contextFactory.CreateDbContext())
            {
                context.Users.Add(existingUser);
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "google-id-321"),
                new Claim(ClaimTypes.Email, "existingloginuser@example.com"),
                new Claim(ClaimTypes.GivenName, "Existing"),
                new Claim(ClaimTypes.Surname, "LoginUser"),
                new Claim(ClaimTypes.Name, "Existing LoginUser"),
                new Claim("picture", "http://example.com/existingloginuser.jpg")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(userPrincipal);

            // Act
            var result = await _userService.GetOrCreateUserFromGoogleAsync(authState);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.CurrentStreak); // Incremented from 1 to 2
            Assert.Equal(2, result.BestStreak); // Updated best streak
            Assert.True((DateTime.UtcNow - result.LastLoginAt.ToUniversalTime()).TotalSeconds < 60);
        }

        #endregion
    }
}
