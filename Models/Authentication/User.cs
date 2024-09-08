namespace Nostr.Models.Authentication;

public class User
{
    public int Id { get; set; }
    public string PubKey { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string? UserName { get; set; }
}