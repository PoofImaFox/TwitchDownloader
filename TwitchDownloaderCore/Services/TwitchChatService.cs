using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SkiaSharp;

using TwitchDownloaderCore.TwitchObjects;

namespace TwitchDownloaderCore.Services {
    public class TwitchChatService {
        private static HttpClient httpClient = new HttpClient();
        public TwitchChatService() {

        }

        public static async Task<byte[]> GetImage(string cachePath, string imageUrl, string imageId, string imageScale, string imageType) {
            byte[] imageBytes = null;

            string filePath = Path.Combine(cachePath, imageId + "_" + imageScale + "." + imageType);
            if (File.Exists(filePath)) {
                try {
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        byte[] bytes = new byte[stream.Length];
                        stream.Seek(0, SeekOrigin.Begin);
                        await stream.ReadAsync(bytes, 0, bytes.Length);

                        //Check if image file is not corrupt
                        if (bytes.Length > 0) {
                            using SKImage image = SKImage.FromEncodedData(bytes);
                            if (image != null) {
                                imageBytes = bytes;
                            }
                            else {
                                //Try to delete the corrupted image
                                try {
                                    stream.Dispose();
                                    File.Delete(filePath);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (IOException) {
                    //File being written to by parallel process? Maybe. Can just fallback to HTTP request.
                }
            }

            if (imageBytes != null)
                return imageBytes;

            imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            //Let's save this image to the cache
            try {
                using (FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
                    stream.Write(imageBytes, 0, imageBytes.Length);
                }
            }
            catch { }

            return imageBytes;
        }

        public static async Task<List<ChatBadge>> GetChatBadges(int streamerId, string cacheFolder) {
            List<ChatBadge> returnList = new List<ChatBadge>();

            JObject globalBadges = JObject.Parse(await httpClient.GetStringAsync("https://badges.twitch.tv/v1/badges/global/display"));
            JObject subBadges = JObject.Parse(await httpClient.GetStringAsync($"https://badges.twitch.tv/v1/badges/channels/{streamerId}/display"));

            string badgeFolder = Path.Combine(cacheFolder, "badges");
            if (!Directory.Exists(badgeFolder))
                TwitchHelper.CreateDirectory(badgeFolder);

            foreach (var badge in subBadges["badge_sets"].Union(globalBadges["badge_sets"])) {
                JProperty jBadgeProperty = badge.ToObject<JProperty>();
                string name = jBadgeProperty.Name;
                Dictionary<string, SKBitmap> versions = new Dictionary<string, SKBitmap>();

                foreach (var version in badge.First["versions"]) {
                    JProperty jVersionProperty = version.ToObject<JProperty>();
                    string versionString = jVersionProperty.Name;
                    string downloadUrl = version.First["image_url_2x"].ToString();

                    try {
                        string[] id_parts = downloadUrl.Split('/');
                        string id = id_parts[id_parts.Length - 2];
                        byte[] bytes = await GetImage(badgeFolder, downloadUrl, id, "2", "png");
                        using MemoryStream ms = new MemoryStream(bytes);
                        //For some reason, twitch has corrupted images sometimes :) for example
                        //https://static-cdn.jtvnw.net/badges/v1/a9811799-dce3-475f-8feb-3745ad12b7ea/1
                        SKBitmap badgeImage = SKBitmap.Decode(ms);
                        versions.Add(versionString, badgeImage);
                    }
                    catch (HttpRequestException) { }
                }

                returnList.Add(new ChatBadge(name, versions));
            }

            return returnList;
        }

        public static async Task<List<CheerEmote>> GetBits(string cacheFolder, string channel_id = "") {
            List<CheerEmote> returnCheermotes = new List<CheerEmote>();

            var request = new HttpRequestMessage() {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("{\"query\":\"query{cheerConfig{groups{nodes{id, prefix, tiers{bits}}, templateURL}},user(id:\\\"" + channel_id + "\\\"){cheer{cheerGroups{nodes{id,prefix,tiers{bits}},templateURL}}}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
            GqlCheerResponse cheerResponse = JsonConvert.DeserializeObject<GqlCheerResponse>(response);

            string bitFolder = Path.Combine(cacheFolder, "bits");
            if (!Directory.Exists(bitFolder))
                TwitchHelper.CreateDirectory(bitFolder);

            if (cheerResponse != null && cheerResponse.data != null) {
                List<CheerGroup> groupList = new List<CheerGroup>();

                foreach (CheerGroup group in cheerResponse.data.cheerConfig.groups) {
                    groupList.Add(group);
                }

                if (cheerResponse.data.user != null && cheerResponse.data.user.cheer != null && cheerResponse.data.user.cheer.cheerGroups != null) {
                    foreach (var group in cheerResponse.data.user.cheer.cheerGroups) {
                        groupList.Add(group);
                    }
                }

                foreach (CheerGroup group in groupList) {
                    string templateURL = group.templateURL;

                    foreach (CheerNode node in group.nodes) {
                        string prefix = node.prefix;
                        List<KeyValuePair<int, TwitchEmote>> tierList = new List<KeyValuePair<int, TwitchEmote>>();
                        CheerEmote newEmote = new CheerEmote() { prefix = prefix, tierList = tierList };
                        foreach (Tier tier in node.tiers) {
                            int minBits = tier.bits;
                            string url = templateURL.Replace("PREFIX", node.prefix.ToLower()).Replace("BACKGROUND", "dark").Replace("ANIMATION", "animated").Replace("TIER", tier.bits.ToString()).Replace("SCALE.EXTENSION", "2.gif");
                            TwitchEmote emote = new TwitchEmote(await GetImage(bitFolder, url, node.id + tier.bits, "2", "gif"), EmoteProvider.FirstParty, 2, prefix + minBits, prefix + minBits);
                            tierList.Add(new KeyValuePair<int, TwitchEmote>(minBits, emote));
                        }
                        returnCheermotes.Add(newEmote);
                    }
                }
            }

            return returnCheermotes;
        }
    }
}
