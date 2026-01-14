using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.App {
    public class DownloadProgress(double downloaded, double total) {
        public double BytesDownloaded { get; } = downloaded;
        public double TotalBytes { get; } = total;
    }

    public partial class DownloadManager {

        public static async Task DownloadFile(string url, string dest, CancellationToken ct, IProgress<DownloadProgress>? progress = null) {
            try {
                var client = new HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? 0;

                using var cs = await response.Content.ReadAsStreamAsync(ct);
                using var fs = File.Create(dest);

                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await cs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                    await fs.WriteAsync(buffer, 0, read, ct);
                    totalRead += read;
                    progress?.Report(new DownloadProgress(totalRead, totalBytes));
                }
            } catch (OperationCanceledException) {
                File.Delete(dest);
            }
        }

        public static async Task<bool> IsDirectDownload(string url, string[] mimeTypes) {
            if (string.IsNullOrEmpty(url)) {
                return false;
            }

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            HttpResponseMessage response = await client.SendAsync(request);

            var contentHeaders = response.Content.Headers;

            return mimeTypes.Contains(contentHeaders.ContentType?.MediaType);
        }

        public static string HumanizeFileSize(double size) {
            // From https://stackoverflow.com/a/4975942
            string[] suf = [ "B", "KB", "MB", "GB", "TB", "PB", "EB" ];
            if (size == 0) return "0" + suf[0];
            long bytes = (long) Math.Abs(size);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(size) * num).ToString() + suf[place];
        }
    }
}