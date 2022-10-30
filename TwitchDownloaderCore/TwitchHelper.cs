using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SkiaSharp;

using TwitchDownloaderCore.Properties;

namespace TwitchDownloaderCore {
    public static class TwitchHelper {
        private static HttpClient httpClient = new HttpClient();
        private static readonly string[] bttvZeroWidth = { "SoSnowy", "IceCold", "SantaHat", "TopHat", "ReinDeer", "CandyCane", "cvMask", "cvHazmat" };

        public static async Task<Dictionary<string, SKBitmap>> GetTwitterEmojis(string cacheFolder) {
            Dictionary<string, SKBitmap> returnCache = new Dictionary<string, SKBitmap>();

            string emojiFolder = Path.Combine(cacheFolder, "emojis");
            if (!Directory.Exists(emojiFolder))
                TwitchHelper.CreateDirectory(emojiFolder);

            int emojiCount = Directory.GetFiles(emojiFolder, "*.png").Length;

            //Twemoji 14 has 3689 emoji images
            if (emojiCount < 3689) {
                string emojiZipPath = Path.GetTempFileName();
                byte[] emojiZipData = Resources.twemoji_14_0_0;
                await File.WriteAllBytesAsync(emojiZipPath, emojiZipData);
                using (ZipArchive archive = ZipFile.OpenRead(emojiZipPath)) {
                    var emojiAssetsPath = Path.Combine("twemoji-14.0.0", "assets", "72x72");
                    var emojis = archive.Entries.Where(x => Path.GetDirectoryName(x.FullName) == emojiAssetsPath && !String.IsNullOrWhiteSpace(x.Name));
                    foreach (var emoji in emojis) {
                        string filePath = Path.Combine(emojiFolder, emoji.Name.ToUpper().Replace("-", " "));
                        if (!File.Exists(filePath)) {
                            try {
                                emoji.ExtractToFile(filePath);
                            }
                            catch { }
                        }
                    }
                }
            }

            List<string> emojiList = new List<string>(Directory.GetFiles(emojiFolder, "*.png"));
            foreach (var emojiPath in emojiList) {
                SKBitmap emojiImage = SKBitmap.Decode(await File.ReadAllBytesAsync(emojiPath));
                returnCache.Add(Path.GetFileNameWithoutExtension(emojiPath), emojiImage);
            }

            return returnCache;
        }



        public static DirectoryInfo CreateDirectory(string path) {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(path);

            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    SetDirectoryPermissions(path);
                }
            }
            catch { }

            return directoryInfo;
        }

        public static void SetDirectoryPermissions(string path) {
            var folderInfo = new Mono.Unix.UnixFileInfo(path);
            folderInfo.FileAccessPermissions = Mono.Unix.FileAccessPermissions.AllPermissions;
            folderInfo.Refresh();
        }

        public static int TimestampToSeconds(string input) {
            //There might be a better way to do this, gets string 0h0m0s and returns timespan
            TimeSpan returnSpan = new TimeSpan(0);
            string[] inputArray = input.Remove(input.Length - 1).Replace('h', ':').Replace('m', ':').Split(':');

            returnSpan = returnSpan.Add(TimeSpan.FromSeconds(Int32.Parse(inputArray[inputArray.Length - 1])));
            if (inputArray.Length > 1)
                returnSpan = returnSpan.Add(TimeSpan.FromMinutes(Int32.Parse(inputArray[inputArray.Length - 2])));
            if (inputArray.Length > 2)
                returnSpan = returnSpan.Add(TimeSpan.FromHours(Int32.Parse(inputArray[inputArray.Length - 3])));

            return (int)returnSpan.TotalSeconds;
        }

        public static async Task<string> GetStreamerName(int id) {
            try {
                var request = new HttpRequestMessage() {
                    RequestUri = new Uri("https://gql.twitch.tv/gql"),
                    Method = HttpMethod.Post,
                    Content = new StringContent("{\"query\":\"query{user(id:\\\"" + id.ToString() + "\\\"){login}}\",\"variables\":{}}", Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                string response = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
                JObject res = JObject.Parse(response);
                return res["data"]["user"]["login"].ToString();
            }
            catch { return ""; }
        }
    }
}
