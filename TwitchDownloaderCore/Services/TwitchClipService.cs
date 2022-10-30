using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TwitchDownloaderCore.TwitchObjects;

namespace TwitchDownloaderCore.Services {
    public class TwitchClipService {
        private static HttpClient httpClient = new HttpClient();

        public TwitchClipService() {

        }

        public static async Task<GqlVideoResponse> GetVideoInfo(int videoId) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"query\":\"query{video(id:\\\"" + videoId + "\\\"){title,thumbnailURLs(height:180,width:320),createdAt,lengthSeconds,owner{id,displayName}}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GqlVideoResponse>(response);
        }

        public static async Task<JObject> GetVideoToken(int videoId, string authToken) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"operationName\":\"PlaybackAccessToken_Template\",\"query\":\"query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: \\\"web\\\", playerBackend: \\\"mediaplayer\\\", playerType: $playerType}) @include(if: $isLive) {    value    signature    __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: \\\"web\\\", playerBackend: \\\"mediaplayer\\\", playerType: $playerType}) @include(if: $isVod) {    value    signature    __typename  }}\",\"variables\":{\"isLive\":false,\"login\":\"\",\"isVod\":true,\"vodID\":\"" + videoId + "\",\"playerType\":\"embed\"}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            if (authToken != null && authToken != "")
                request.Headers.Add("Authorization", "OAuth " + authToken);
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return JObject.Parse(response);
        }

        public static async Task<string[]> GetVideoPlaylist(int videoId, string token, string sig) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri(String.Format("http://usher.twitch.tv/vod/{0}?nauth={1}&nauthsig={2}&allow_source=true&player=twitchweb", videoId, token, sig)),
                Method = HttpMethod.Get
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string playlist = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return playlist.Split('\n');
        }

        public static async Task<GqlClipResponse> GetClipInfo(object clipId) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"query\":\"query{clip(slug:\\\"" + clipId + "\\\"){title,thumbnailURL,createdAt,durationSeconds,broadcaster{id,displayName},videoOffsetSeconds,video{id}}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GqlClipResponse>(response);
        }

        public static async Task<JArray> GetClipLinks(object clipId) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("[{\"operationName\":\"VideoAccessToken_Clip\",\"variables\":{\"slug\":\"" + clipId + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"36b89d2507fce29e5ca551df756d27c1cfe079e2609642b4390aa4c35796eb11\"}}}]", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            JArray result = JArray.Parse(response);
            return result;
        }

        public static async Task<GqlVideoSearchResponse> GetGqlVideos(string channelName, string cursor = "", int limit = 50) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"query\":\"query{user(login:\\\"" + channelName + "\\\"){videos(first: " + limit + "" + (cursor == "" ? "" : ",after:\\\"" + cursor + "\\\"") + ") { edges { node { title, id, lengthSeconds, previewThumbnailURL(height: 180, width: 320), createdAt, viewCount }, cursor }, pageInfo { hasNextPage, hasPreviousPage }, totalCount }}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GqlVideoSearchResponse>(response);
        }

        public static async Task<GqlClipSearchResponse> GetGqlClips(string channelName, string period = "LAST_WEEK", string cursor = "", int limit = 50) {
            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"query\":\"query{user(login:\\\"" + channelName + "\\\"){clips(first: " + limit + ", after: \\\"" + cursor + "\\\", criteria: { period: " + period + " }) {  edges { cursor, node { id, slug, title, createdAt, durationSeconds, thumbnailURL, viewCount } }, pageInfo { hasNextPage, hasPreviousPage } }}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GqlClipSearchResponse>(response);
        }
    }
}