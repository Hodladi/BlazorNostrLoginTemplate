using Newtonsoft.Json.Linq;
using NNostr.Client;

namespace Nostr.Services
{
    public interface INostrService
    {
        Task<string> GetDisplayNameAsync(string npubHex);
    }

    public class NostrService : INostrService
    {
        public async Task<string> GetDisplayNameAsync(string npubHex)
        {
            var client = new NostrClient(new Uri("wss://relay.nostrich.cc"));

            await client.Connect();

            var subscriptionId = "get-display-name-subscription";
            await client.CreateSubscription(subscriptionId, new[]
            {
                new NostrSubscriptionFilter
                {
                    Kinds = new[] { 0 },
                    Authors = new[] { npubHex }
                }
            });

            string? displayName = null;
            client.EventsReceived += (_, args) =>
            {
                if (args.subscriptionId == subscriptionId)
                {
                    foreach (var nostrEvent in args.events)
                    {
                        if (nostrEvent.Content != null)
                        {
                            var content = JObject.Parse(nostrEvent.Content);
                            displayName = content["name"]?.ToString();
                            client.CloseSubscription(subscriptionId);
                        }
                    }
                }
            };

            await Task.Delay(5000);

            return displayName ?? "Display name not found.";
        }
    }
}