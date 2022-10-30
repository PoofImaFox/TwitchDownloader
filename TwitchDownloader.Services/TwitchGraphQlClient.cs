using TwitchDownloader.Services.TwitchGraphQl;

namespace TwitchDownloader.Services {
    public class TwitchGraphQlClient {
        public TwitchGraphQlClient(TwitchUserApi twitchUserApi) {
            TwitchUserApi = twitchUserApi;
        }

        public TwitchUserApi TwitchUserApi { get; }
    }
}