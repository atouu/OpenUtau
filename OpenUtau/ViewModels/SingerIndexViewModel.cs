using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;

namespace OpenUtau.App.ViewModels {
    public partial class SingerIndexViewModel : ViewModelBase {

        public class SingerEntry {
            public string Id { get; set; } = string.Empty;
            public Dictionary<string, string> Names { get; set; } = [];
            public SingerVariant[] Variants { get; set; } = [];
            public string HomepageUrl { get; set; } = string.Empty;
            public string ProfileImageUrl { get; set; } = string.Empty;
            public string[] Owners = [];
            public string[] Authors = [];

            public Task<Bitmap?> ProfileBitmap => GetImageUrlBitmap(ProfileImageUrl);
            public string AltNames => string.Join(" / ", Names.Select(v => v.Value));
            public string[] AllVariantsEn => [.. Variants.Select(v => v.Names["en"])];
            public string[] AllTags => [.. Variants.SelectMany(x => x.Tags).Distinct()];
            public string OwnersStr => string.Join(", ", Owners);
            public string AuthorsStr => string.Join(", ", Authors);
            public SingerVariant[] CompatVariants => [
                .. Variants.Where(v => v.Tags.Any(t => SingerTagFilters.Contains(t)))
            ];
        }

        public class SingerVariant {
            public string Id { get; set; } = string.Empty;
            public Dictionary<string, string> Names { get; set; } = [];
            public string DownloadPageUrl { get; set; } = string.Empty;
            public string FileUrl { get; set; } = string.Empty;
            public string[] Tags { get; set; } = [];

            public string AltNames => string.Join(" / ", Names.Select(v => v.Value));
            public Task<bool> IsValidDirectDownload =>
                Downloader.IsDirectDownload(FileUrl, ValidDLMimeTypes);
        }

        private static readonly string[] SingerTagFilters = [
            "diffsinger", "utau",
            "enunu", "vogen", "openutau"
        ];

        private static readonly string[] ValidDLMimeTypes = [
            "application/zip",
            "application/x-zip-compressed",
            "application/x-7z-compressed",
            "application/x-rar-compressed",
            "application/octet-stream"
        ];

        private CancellationTokenSource? cancellation;
        private IEnumerable<SingerEntry>? singers;

        [Reactive] public partial IEnumerable<SingerEntry>? SingersFiltered { get; set; }
        [Reactive] public partial SingerEntry? SelectedSinger { get; set; }
        [Reactive] public partial string Search { get; set; }
        [Reactive] public partial int Page { get; set; }
        [Reactive] public partial int Sorting { get; set; }

        [Reactive] public partial string? DownloadName { get; set; }
        [Reactive] public partial string? DownloadCurrentSize { get; set; }
        [Reactive] public partial string? DownloadTotalSize { get; set; }
        [Reactive] public partial double DownloadProgress { get; set; }
        [Reactive] public partial bool IsDownloading { get; set; }

        public Interaction<SingerVariant,bool> DownloadVariant { get; private set; } = new();

        public SingerIndexViewModel() {
            Search = string.Empty;

            SyncVoicebanks().ContinueWith(s => {
                singers = s.Result;
                SingersFiltered = singers;
            });

            this.WhenAnyValue(vm => vm.Sorting)
                .Subscribe(x => {
                    SelectedSinger = null;
                    if (x == 0) {
                        singers = singers?.OrderBy(v => v.Names["en"]);
                    } else {
                        singers = singers?.OrderByDescending(v => v.Names["en"]);
                    }
                    SingersFiltered = singers;
                    Search = string.Empty;
                });

            this.WhenAnyValue(vm => vm.Search)
                .Subscribe(x => {
                    SingersFiltered = singers?.Where(v => 
                        v.AltNames.Contains(x, StringComparison.CurrentCultureIgnoreCase));
                });
        }

        public void OnSelectedSinger(object singer) {
            SelectedSinger = singer as SingerEntry;
            Page = 1;
        }

        public void OnBack() {
            SelectedSinger = null;
            Page = 0;
        }

        public async Task OnDownloadVariant(SingerVariant variant) {
            if (!(await DownloadVariant.Handle(variant).ToTask())) {
                return;
            }

            cancellation = new();
            IsDownloading = true;
            DownloadName = variant.Names["en"];

            string dest = Path.Join(PathManager.Inst.CachePath, $"{new Guid(MD5.HashData(Encoding.UTF8.GetBytes(DownloadName)))}");

            var progress = new Progress<Downloader.DownloadProgress>(dp => {
                Dispatcher.UIThread.InvokeAsync(() => {
                    DownloadCurrentSize = Downloader.HumanizeFileSize(dp.BytesDownloaded);
                    DownloadTotalSize = Downloader.HumanizeFileSize(dp.TotalBytes);
                    DownloadProgress = dp.BytesDownloaded * 100 / dp.TotalBytes;
                }, DispatcherPriority.Background);
            });

            await Downloader.DownloadFile(variant.FileUrl, dest, cancellation.Token, progress);

            if (!cancellation.Token.IsCancellationRequested) {
                var setup = new SingerSetupDialog() {
                    DataContext = new SingerSetupViewModel() {
                        ArchiveFilePath = dest,
                    }
                };
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {
                        MainWindow: MainWindow mainWindow
                    })
                {
                    await setup.ShowDialog(mainWindow);
                }
            }

            IsDownloading = false;
        }

        public void OnDownloadCancel() {
            cancellation?.Cancel();
        }

        private static async Task<List<SingerEntry>> SyncVoicebanks() {
            List<SingerEntry> entries = [];

            using var client = new HttpClient();

            char[] abc = [.. "abcdefghijklmnopqrstuvwxyz"];

            var requests = abc.Select(chr => client.GetAsync($"https://raw.githubusercontent.com/openutau/svs-index/main/data/singers/{chr}.json"));

            await Task.WhenAll(requests);

            var responses = requests.Select(task => task.Result);

            var settings = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            foreach (var response in responses) {
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) {
                    continue;
                }
                var vbs = Json.Deserialize<List<SingerEntry>>(body, settings);
                if (vbs != null) {
                    entries.AddRange(vbs);
                }
            }

            return [.. entries.Where(v => v.AllTags.Any(t => SingerTagFilters.Contains(t))).OrderBy(v => v.Names["en"])];
        }

        private static async Task<Bitmap?> GetImageUrlBitmap(string url) {
            if (string.IsNullOrEmpty(url)) {
                return null;
            }

            try {
                using var client = new HttpClient() {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                var response = await client.GetByteArrayAsync(url);

                using var stream = new MemoryStream(response);

                return Bitmap.DecodeToHeight(stream, 300);
            } catch {
                return null;
            }
        }
    }
}