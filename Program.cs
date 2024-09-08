using Microsoft.EntityFrameworkCore;
using Nostr.Components;
using Nostr.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Nostr.Middleware;
using Nostr.Services;
using Blazored.Modal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddDbContext<AuthDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(sp =>
{
	var navigationManager = sp.GetRequiredService<NavigationManager>();
	return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>(provider =>
	provider.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<INostrService, NostrService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddBlazoredModal();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();