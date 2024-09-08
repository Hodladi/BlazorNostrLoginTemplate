using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Nostr.Middleware
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private bool _isInitialized;

        public CustomAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_isInitialized)
                return new AuthenticationState(_anonymous);

            var user = await GetAuthenticatedUserFromLocalStorageAsync();
            return new AuthenticationState(user ?? _anonymous);
        }

        public async Task InitializeAsync()
        {
            _isInitialized = true;
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task NotifyUserAuthentication(string userName, string nPub)
        {
            await SetUserInLocalStorageAsync(userName, nPub);

            var user = CreateAuthenticatedUser(userName, nPub);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task NotifyUserLogout()
        {
            await ClearUserFromLocalStorageAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }

        private async Task<ClaimsPrincipal?> GetAuthenticatedUserFromLocalStorageAsync()
        {
            var userName = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
            var nPub = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "nPub");

            return !string.IsNullOrWhiteSpace(userName) ? CreateAuthenticatedUser(userName, nPub) : null;
        }

        private ClaimsPrincipal CreateAuthenticatedUser(string userName, string nPub)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim("nPub", nPub)
            };

            var identity = new ClaimsIdentity(claims, "localStorageAuth");
            return new ClaimsPrincipal(identity);
        }

        private async Task SetUserInLocalStorageAsync(string userName, string nPub)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", userName);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "nPub", nPub);
        }

        private async Task ClearUserFromLocalStorageAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "nPub");
        }
    }
}
