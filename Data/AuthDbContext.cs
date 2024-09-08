using Microsoft.EntityFrameworkCore;
using Nostr.Models.Authentication;

namespace Nostr.Data;

public class AuthDbContext : DbContext
{
	public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
	{
	}

	public DbSet<User> Users { get; set; }
}
