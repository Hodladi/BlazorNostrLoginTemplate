using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Nostr.Data;
using Nostr.Models.Authentication;

namespace Nostr.Services;

public interface IUserService
{
    Task<User> GetUserByPubKeyAsync(string pubKey);
    Task<User> GetUserByUsernameAsync(string username);
    Task UpdateUserAsync(User user);
    bool VerifyPassword(string enteredPassword, string storedPasswordHash);
}

public class UserService : IUserService
{
    private readonly AuthDbContext _dbContext;
    public UserService(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> GetUserByPubKeyAsync(string pubKey)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == pubKey);
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        return await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
    }

    public async Task UpdateUserAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null.");
        }

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }
    public bool VerifyPassword(string enteredPassword, string storedPasswordHash)
    {
        return BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash);
    }
}
