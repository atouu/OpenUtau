using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Util {

    public partial class Downloader {

        private static readonly HttpClient client = new();

        public class DownloadProgress(double downloaded, double total) {
            public double BytesDownloaded { get; } = downloaded;
            public double TotalBytes { get; } = total;
        }

        public static async Task DownloadFile(string url, string dest, CancellationToken ct, IProgress<DownloadProgress>? progress = null) {
            try {
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

        public static async Task<byte[]> DownloadBytes(string url, IProgress<int>? progress = null) {
            client.Timeout = TimeSpan.FromMinutes(5);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var contentLength = response.Content.Headers.ContentLength;
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            long totalRead = 0;
            int read;
            if (contentLength.HasValue && progress != null) progress.Report(0);
            while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, read);
                totalRead += read;
                if (contentLength.HasValue && progress != null) {
                    var percent = (int)(totalRead * 100 / contentLength.Value);
                    progress.Report(Math.Min(100, percent));
                }
            }
            if (progress != null) progress.Report(100);
            return ms.ToArray();
        }

        public static async Task<bool> IsDirectDownload(string url, string[] mimeTypes) {
            if (string.IsNullOrEmpty(url)) {
                return false;
            }

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