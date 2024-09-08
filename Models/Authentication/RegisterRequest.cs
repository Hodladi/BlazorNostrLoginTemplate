using System.ComponentModel.DataAnnotations;

namespace Nostr.Models.Authentication;

public class RegisterRequest
{
    public string? PubKey { get; set; }
    
    [Required(ErrorMessage = "Secret is required")]
    public string Secret { get; set; }
    
    public string? UserName { get; set; }
}
