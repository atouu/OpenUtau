
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Util {
    public static partial class DownloadManager {

        public static async Task DownloadFile(Stream cs, string dest, IProgress<double> progress, CancellationToken ct) {
            using var fs = File.Create(dest);

            var buffer = new byte[8192];
            long bytesRead = 0;
            int bytesReadInChunk;

            while ((bytesReadInChunk = await cs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                await fs.WriteAsync(buffer, 0, bytesReadInChunk, ct);
                bytesRead += bytesReadInChunk;

                progress.Report((double)bytesRead);
            }
        }

        public static async Task<bool> IsDirectDownload(string url, string[] mimeTypes) {

            if (string.IsNullOrEmpty(url)) {
                return false;
            }

            using var client = new HttpClient() {
                DefaultRequestHeaders = {
                    {"User-Agent", "Firefox"}
                },
                Timeout = TimeSpan.FromSeconds(10)
            };

            url = await GetDirectDownloadLink(url, client);

            var request = new HttpRequestMessage(HttpMethod.Head, url);
            HttpResponseMessage response = await client.SendAsync(request);

            var contentHeaders = response.Content.Headers;

            return mimeTypes.Contains($"{contentHeaders.ContentType}");
        }

        public static async Task<string> GetDirectDownloadLink(string url, HttpClient client) {
            if (url.StartsWith("https://www.mediafire.com/file")) {
                url = await GetMediaFireDirectLink(url, client);
            }
            return url;
        }

        public static string HumanizeFileSize(double size) {
            string[] suf = [ "B", "KB", "MB", "GB", "TB", "PB", "EB" ];
            if (size == 0)
                return "0" + suf[0];
            long bytes = (long) Math.Abs(size);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(size) * num).ToString() + suf[place];
        }

        private static async Task<string> GetMediaFireDirectLink(string url, HttpClient client) {
            var response = await client.GetStringAsync(url);
            Match match = MediaFireMatch().Match(response);
            return match.Value;
        }

        [GeneratedRegex(@"https?:\/\/download[0-9]+\.mediafire\.com\/[^'\""]+")]
        private static partial Regex MediaFireMatch();
    }
}