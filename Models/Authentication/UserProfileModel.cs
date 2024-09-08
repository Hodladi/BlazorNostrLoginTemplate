namespace Nostr.Models.Authentication;

public class UserProfileModel
{
    public string? PubKey { get; set; }
    public string? Npub { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}