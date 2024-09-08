using System.ComponentModel.DataAnnotations;

namespace Nostr.Models.Authentication;
public class UserModel
{
    public string PubKey { get; set; } = string.Empty;
    public string Npub { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 5 and 255 characters", MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Secret { get; set; }

    [Required(ErrorMessage = "Confirm Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 5 and 255 characters", MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Compare("Secret")]
    public string ConfirmPassword { get; set; }
}
