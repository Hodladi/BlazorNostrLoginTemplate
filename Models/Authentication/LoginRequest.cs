namespace Nostr.Models.Authentication;

public class LoginRequest
{
    public string PubKey { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}