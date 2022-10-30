using System.Net;
using System.Threading.Tasks;
using System.Web;

using TwitchDownloaderCore.Options;

namespace TwitchDownloaderCore {
    public class ClipDownloader {
        private readonly ClipDownloadOptions _downloadOptions;

        public ClipDownloader(ClipDownloadOptions DownloadOptions) {
            _downloadOptions = DownloadOptions;
        }

        public async Task DownloadAsync() {
            await DownloadAsync(_downloadOptions.Id);
        }

        public async Task DownloadAsync(string clipId) {
            var taskLinks = await TwitchHelper.GetClipLinks(clipId);

            var downloadUrl = string.Empty;

            foreach (var quality in taskLinks[0]["data"]["clip"]["videoQualities"]) {
                if (quality["quality"].ToString() + "p" + (quality["frameRate"].ToString() == "30" ? "" : quality["frameRate"].ToString()) == _downloadOptions.Quality) {
                    downloadUrl = quality["sourceURL"].ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(downloadUrl)) {
                downloadUrl = taskLinks[0]["data"]["clip"]["videoQualities"].First["sourceURL"].ToString();
            }

            downloadUrl += "?sig=" + taskLinks[0]["data"]["clip"]["playbackAccessToken"]["signature"] + "&token=" + HttpUtility.UrlEncode(taskLinks[0]["data"]["clip"]["playbackAccessToken"]["value"].ToString());

            using var client = new WebClient();
            await client.DownloadFileTaskAsync(downloadUrl, _downloadOptions.Filename);
        }
    }
}
