using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SkiaSharp;

using TwitchDownloaderCore.TwitchObjects;

namespace TwitchDownloaderCore.Services {
    public class TwitchEmoteService {
        private static HttpClient httpClient = new HttpClient();
        public TwitchEmoteService() {

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

        public static async Task<EmoteResponse> GetThirdPartyEmoteData(string streamerId, bool getBttv, bool getFfz, bool getStv) {
            EmoteResponse data = new EmoteResponse();

            if (getBttv) {
                JArray BTTV = JArray.Parse(await httpClient.GetStringAsync("https://api.betterttv.net/3/cached/emotes/global"));

                if (streamerId != null) {
                    //Channel might not have BTTV emotes
                    try {
                        JObject bttvChannel = JObject.Parse(await httpClient.GetStringAsync("https://api.betterttv.net/3/cached/users/twitch/" + streamerId));
                        BTTV.Merge(bttvChannel["channelEmotes"]);
                        BTTV.Merge(bttvChannel["sharedEmotes"]);
                    }
                    catch { }
                }

                foreach (var emote in BTTV) {
                    string id = emote["id"].ToString();
                    string name = emote["code"].ToString();
                    string mime = emote["imageType"].ToString();
                    string url = String.Format("https://cdn.betterttv.net/emote/{0}/[scale]x", id);
                    data.BTTV.Add(new EmoteResponseItem() { Id = id, Code = name, ImageType = mime, ImageUrl = url, IsZeroWidth = bttvZeroWidth.Contains(name) });
                }
            }

            if (getFfz) {
                JArray FFZ = JArray.Parse(await httpClient.GetStringAsync("https://api.betterttv.net/3/cached/frankerfacez/emotes/global"));

                if (streamerId != null) {
                    //Channel might not have FFZ emotes
                    try {
                        FFZ.Merge(JArray.Parse(await httpClient.GetStringAsync("https://api.betterttv.net/3/cached/frankerfacez/users/twitch/" + streamerId)));
                    }
                    catch { }
                }

                foreach (var emote in FFZ) {
                    string id = emote["id"].ToString();
                    string name = emote["code"].ToString();
                    string mime = emote["imageType"].ToString();
                    string url = String.Format("https://cdn.betterttv.net/frankerfacez_emote/{0}/[scale]", id);
                    data.FFZ.Add(new EmoteResponseItem() { Id = id, Code = name, ImageType = mime, ImageUrl = url });
                }
            }

            if (getStv) {
                JArray STV = JArray.Parse(await httpClient.GetStringAsync("https://7tv.io/v2/emotes/global"));

                if (streamerId != null) {
                    //Channel might not have 7TV emotes
                    try {
                        STV.Merge(JArray.Parse(await httpClient.GetStringAsync(String.Format("https://api.7tv.app/v2/users/{0}/emotes", streamerId))));
                    }
                    catch { }
                }


                foreach (var emote in STV) {
                    string id = emote["id"].ToString();
                    string name = emote["name"].ToString();
                    string mime = emote["mime"].ToString().Split('/')[1];
                    string url = String.Format("https://cdn.7tv.app/emote/{0}/[scale]x", id);
                    EmoteResponseItem emoteResponse = new EmoteResponseItem() { Id = id, Code = name, ImageType = mime, ImageUrl = url };
                    if (emote["visibility_simple"].ToList().Contains("ZERO_WIDTH"))
                        emoteResponse.IsZeroWidth = true;
                    data.STV.Add(emoteResponse);
                }
            }

            return data;
        }
        public static async Task<List<TwitchEmote>> GetThirdPartyEmotes(int streamerId, string cacheFolder, Emotes embededEmotes = null, bool bttv = true, bool ffz = true, bool stv = true) {
            List<TwitchEmote> returnList = new List<TwitchEmote>();
            List<string> alreadyAdded = new List<string>();

            string bttvFolder = Path.Combine(cacheFolder, "bttv");
            string ffzFolder = Path.Combine(cacheFolder, "ffz");
            string stvFolder = Path.Combine(cacheFolder, "stv");

            EmoteResponse emoteDataResponse = await GetThirdPartyEmoteData(streamerId.ToString(), bttv, ffz, stv);

            if (embededEmotes != null) {
                foreach (EmbedEmoteData emoteData in embededEmotes.thirdParty) {
                    try {
                        TwitchEmote newEmote = new TwitchEmote(emoteData.data, EmoteProvider.ThirdParty, emoteData.imageScale, emoteData.id, emoteData.name);
                        returnList.Add(newEmote);
                        alreadyAdded.Add(emoteData.name);
                    }
                    catch { }
                }
            }

            if (bttv) {
                if (!Directory.Exists(bttvFolder))
                    TwitchHelper.CreateDirectory(bttvFolder);

                foreach (var emote in emoteDataResponse.BTTV) {
                    if (alreadyAdded.Contains(emote.Code))
                        continue;
                    TwitchEmote newEmote = new TwitchEmote(await GetImage(bttvFolder, emote.ImageUrl.Replace("[scale]", "2"), emote.Id, "2", emote.ImageType), EmoteProvider.ThirdParty, 2, emote.Id, emote.Code);
                    if (emote.IsZeroWidth)
                        newEmote.IsZeroWidth = true;
                    returnList.Add(newEmote);
                    alreadyAdded.Add(emote.Code);
                }
            }

            if (ffz) {
                if (!Directory.Exists(ffzFolder))
                    TwitchHelper.CreateDirectory(ffzFolder);

                foreach (var emote in emoteDataResponse.FFZ) {
                    if (alreadyAdded.Contains(emote.Code))
                        continue;
                    TwitchEmote newEmote = new TwitchEmote(await GetImage(ffzFolder, emote.ImageUrl.Replace("[scale]", "2"), emote.Id, "2", emote.ImageType), EmoteProvider.ThirdParty, 2, emote.Id, emote.Code);
                    returnList.Add(newEmote);
                    alreadyAdded.Add(emote.Code);
                }
            }

            if (stv) {
                if (!Directory.Exists(stvFolder))
                    TwitchHelper.CreateDirectory(stvFolder);

                foreach (var emote in emoteDataResponse.STV) {
                    if (alreadyAdded.Contains(emote.Code))
                        continue;
                    TwitchEmote newEmote = new TwitchEmote(await GetImage(stvFolder, emote.ImageUrl.Replace("[scale]", "2"), emote.Id, "2", emote.ImageType), EmoteProvider.ThirdParty, 2, emote.Id, emote.Code);
                    if (emote.IsZeroWidth)
                        newEmote.IsZeroWidth = true;
                    returnList.Add(newEmote);
                    alreadyAdded.Add(emote.Code);
                }
            }

            return returnList;
        }

        public static async Task<List<TwitchEmote>> GetEmotes(List<Comment> comments, string cacheFolder, Emotes embededEmotes = null) {
            List<TwitchEmote> returnList = new List<TwitchEmote>();
            List<string> alreadyAdded = new List<string>();
            List<string> failedEmotes = new List<string>();

            string emoteFolder = Path.Combine(cacheFolder, "emotes");
            if (!Directory.Exists(emoteFolder))
                TwitchHelper.CreateDirectory(emoteFolder);

            if (embededEmotes != null) {
                foreach (EmbedEmoteData emoteData in embededEmotes.firstParty) {
                    try {
                        TwitchEmote newEmote = new TwitchEmote(emoteData.data, EmoteProvider.FirstParty, emoteData.imageScale, emoteData.id, emoteData.name);
                        returnList.Add(newEmote);
                        alreadyAdded.Add(emoteData.name);
                    }
                    catch { }
                }
            }

            foreach (var comment in comments) {
                if (comment.message.fragments == null)
                    continue;

                foreach (var fragment in comment.message.fragments) {
                    if (fragment.emoticon != null) {
                        string id = fragment.emoticon.emoticon_id;
                        if (!alreadyAdded.Contains(id) && !failedEmotes.Contains(id)) {
                            byte[] bytes = await GetImage(emoteFolder, String.Format("https://static-cdn.jtvnw.net/emoticons/v2/{0}/default/dark/2.0", id), id, "2", "png");
                            TwitchEmote newEmote = new TwitchEmote(bytes, EmoteProvider.FirstParty, 2, id, id);
                            alreadyAdded.Add(id);
                            returnList.Add(newEmote);
                        }
                    }
                }
            }

            return returnList;
        }
    }
}
