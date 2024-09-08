using Microsoft.AspNetCore.Mvc;
using NNostr.Client;
using Nostr.Data;
using Microsoft.EntityFrameworkCore;
using Nostr.Models.Authentication;
using Nostr.Services;
using User = Nostr.Models.Authentication.User;

namespace Nostr.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _dbContext;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthDbContext dbContext, ILogger<AuthController> logger, IUserService userService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userService = userService;
        }

        [HttpGet("challenge")]
        public IActionResult GetChallenge(string pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                return BadRequest("Public key is required.");
            }

            string challenge = Guid.NewGuid().ToString();
            return Ok(challenge);
        }

        [HttpPost("loginuser")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Signature) ||
                string.IsNullOrEmpty(loginRequest.PubKey) || string.IsNullOrEmpty(loginRequest.Challenge))
            {
                return BadRequest("Signature, public key, and challenge are required.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == loginRequest.PubKey);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (loginRequest.CreatedAt <= 0)
            {
                return BadRequest("Invalid or missing created_at time.");
            }

            bool isSignatureValid = VerifySignature(loginRequest.PubKey, loginRequest.Signature, loginRequest.Challenge, loginRequest.CreatedAt);
            if (!isSignatureValid)
            {
                return Unauthorized("Invalid signature.");
            }

            return Ok("User authenticated successfully.");
        }

        private bool VerifySignature(string pubKey, string signature, string challenge, long createdAt)
        {
            try
            {
                var nostrEvent = new NostrEvent
                {
                    PublicKey = pubKey,
                    Content = challenge,
                    Signature = signature,
                    Kind = 27235,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(createdAt)
                };

                nostrEvent.Id = nostrEvent.ComputeId<NostrEvent, NostrEventTag>();
                bool isValid = nostrEvent.Verify<NostrEvent, NostrEventTag>();
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature for public key: {PubKey}", pubKey);
                return false;
            }
        }

        [HttpPost("registernewuser")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (string.IsNullOrEmpty(registerRequest.PubKey) && 
                (string.IsNullOrEmpty(registerRequest.UserName) || string.IsNullOrEmpty(registerRequest.Secret)))
            {
                return BadRequest("Public key or both username and secret are required.");
            }

            if (!string.IsNullOrEmpty(registerRequest.PubKey))
            {
                var existingUserWithKey = await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == registerRequest.PubKey);
                if (existingUserWithKey != null)
                {
                    return Conflict("A user with this public key already exists.");
                }

                var newUser = new User
                {
                    PubKey = registerRequest.PubKey,
                    Secret = BCrypt.Net.BCrypt.HashPassword(registerRequest.Secret),
                    UserName = null
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                return Ok("User registered with PubKey successfully.");
            }

            if (!string.IsNullOrEmpty(registerRequest.UserName))
            {
                var existingUserWithName = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == registerRequest.UserName);
                if (existingUserWithName != null)
                {
                    return Conflict("A user with this username already exists.");
                }

                var newUser = new User
                {
                    UserName = registerRequest.UserName,
                    Secret = BCrypt.Net.BCrypt.HashPassword(registerRequest.Secret)
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                return Ok("User registered successfully.");
            }

            return BadRequest("Invalid registration data.");
        }

        [HttpPost("registerwithoutnostr")]
        public async Task<IActionResult> RegisterWithoutNostr([FromBody] RegisterRequest registerRequest)
        {
            if (string.IsNullOrEmpty(registerRequest.UserName) || string.IsNullOrEmpty(registerRequest.Secret))
            {
                return BadRequest("Username and secret are required.");
            }

            var existingUserWithName = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == registerRequest.UserName);
            if (existingUserWithName != null)
            {
                return Conflict("A user with this username already exists.");
            }

            var newUser = new User
            {
                UserName = registerRequest.UserName,
                Secret = BCrypt.Net.BCrypt.HashPassword(registerRequest.Secret)
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        [HttpPost("completeRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Secret))
                {
                    return BadRequest("Invalid user data. Username and secret are required.");
                }

                User existingUser = null;
                if (!string.IsNullOrEmpty(user.PubKey))
                {
                    existingUser = await _userService.GetUserByPubKeyAsync(user.PubKey);
                }

                if (existingUser == null)
                {
                    existingUser = await _userService.GetUserByUsernameAsync(user.UserName);
                }

                if (existingUser == null)
                {
                    return NotFound("User not found.");
                }

                existingUser.UserName = user.UserName;
                existingUser.Secret = BCrypt.Net.BCrypt.HashPassword(user.Secret);

                await _userService.UpdateUserAsync(existingUser);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while completing registration.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginWithUsernameAndPassword([FromBody] LoginModel login)
        {
            if (string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Invalid login data.");
            }

            var user = await _userService.GetUserByUsernameAsync(login.Username);
            if (user == null || !_userService.VerifyPassword(login.Password, user.Secret))
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok("User authenticated successfully.");
        }

        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required.");
            }

            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (existingUser != null)
            {
                return Conflict("Username is already taken.");
            }

            return Ok();
        }

        [HttpGet("check-pubkey")]
        public async Task<IActionResult> CheckPubKey([FromQuery] string pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                return BadRequest("Public key is required.");
            }

            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == pubKey);
            if (existingUser != null)
            {
                return Conflict("Public key is already registered.");
            }

            return Ok();
        }

        [HttpGet("get-pubkey")]
        public async Task<IActionResult> GetPubKey([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user.PubKey);
        }

        [HttpGet("get-username")]
        public async Task<IActionResult> GetUserName([FromQuery] string pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                return BadRequest("Username is required.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == pubKey);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user.UserName);
        }
        [HttpDelete("deletepubkey/{pubKey}")]
        public async Task<IActionResult> DeletePubKey(string pubKey)
        {
            if (string.IsNullOrEmpty(pubKey))
            {
                return BadRequest("Public key is required.");
            }

            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PubKey == pubKey);
                if (user == null)
                {
                    return NotFound("User with the specified public key not found.");
                }

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();

                return Ok("Public key deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the public key: {PubKey}", pubKey);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
