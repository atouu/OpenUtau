
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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

            public Task<Bitmap?> ProfileBitmap => getImageUrlBitmap(ProfileImageUrl);
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
                DownloadManager.IsDirectDownload(FileUrl, ValidDLMimeTypes);
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

        private readonly CancellationTokenSource cancellation = new();
        private IEnumerable<SingerEntry>? singers;

        [Reactive] public IEnumerable<SingerEntry>? SingersFiltered { get; set; }
        [Reactive] public SingerEntry? SelectedSinger { get; set; }
        [Reactive] public string Search { get; set; }
        [Reactive] public bool IsIndexing { get; set; }
        [Reactive] public int Page { get; set; }
        [Reactive] public int Sorting { get; set; }
        [Reactive] public string? DownloadName { get; set; }
        [Reactive] public string? DownloadCurrentSize { get; set; }
        [Reactive] public string? DownloadTotalSize { get; set; }
        [Reactive] public double DownloadProgress { get; set; }
        [Reactive] public bool IsDownloading { get; set; }

        public Interaction<SingerVariant,bool> DownloadVariant { get; private set; }

        public SingerIndexViewModel() {
            DownloadVariant = new();
            DownloadName = string.Empty;
            Search = string.Empty;

            Task.Run(async () => {
                IsIndexing = true;
                singers = await SyncVoicebanks();
                SingersFiltered = singers;
                IsIndexing = false;
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
                });

            this.WhenAnyValue(vm => vm.Search)
                .Subscribe(x => {
                    SingersFiltered = singers?.Where(v => 
                        v.AltNames.Contains(x, StringComparison.CurrentCultureIgnoreCase));
                });
        }

        public void OnSelectedSinger(object singer) {
            if (SelectedSinger != singer) {
                SelectedSinger = null;
                SelectedSinger = (SingerEntry) singer;
            }
            Page = 1;
        }

        public void OnBack() {
            Page = 0;
        }

        public async Task OnDownloadVariant(object variant) {
            bool valid = await DownloadVariant.Handle((SingerVariant) variant);
            if (!valid) {
                return;
            }
            IsDownloading = true;
            DownloadName = ((SingerVariant)variant).Names["en"];

            string dest = Path.Join(PathManager.Inst.CachePath, $"{Guid.NewGuid()}");

            var progress = new Progress<DownloadProgress>(dp => {
                Dispatcher.UIThread.InvokeAsync(() => {
                    DownloadProgress = (double) dp.BytesDownloaded * 100 / dp.TotalBytes;
                    DownloadCurrentSize = DownloadManager.HumanizeFileSize(dp.BytesDownloaded);
                });
            });

            await DownloadManager.DownloadFile(((SingerVariant)variant).FileUrl, dest, progress, cancellation.Token);

            var setup = new SingerSetupDialog() {
                DataContext = new SingerSetupViewModel() {
                    ArchiveFilePath = dest,
                }
            };
            setup.Show();

            IsDownloading = false;
        }

        public void OnDownloadCancel() {
            cancellation.Cancel();
            IsDownloading = false;
        }

        private static async Task<List<SingerEntry>> SyncVoicebanks() {
            List<SingerEntry> entries = [];

            using var client = new HttpClient() {
                DefaultRequestHeaders = {
                    {"User-Agent", "Firefox"}
                },
                Timeout = TimeSpan.FromSeconds(30)
            };

            char[] abc = [.. "abcdefghijklmnopqrstuvwxyz"];

            var requests  = abc.Select(chr => client.GetAsync($"https://raw.githubusercontent.com/openutau/svs-index/main/data/singers/{chr}.json"));

            await Task.WhenAll(requests);

            var responses = requests.Select(task => task.Result);

            foreach (var response in responses) {
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) {
                    continue;
                }
                var settings = new JsonSerializerSettings() {
                    ContractResolver = new DefaultContractResolver() {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                };
                var vbs = JsonConvert.DeserializeObject<List<SingerEntry>>(body, settings);
                if (vbs != null) {
                    entries.AddRange(vbs);
                }
            }

            return [.. entries.Where(v => v.AllTags.Any(t => SingerTagFilters.Contains(t)))];
        }

        private static async Task<Bitmap?> getImageUrlBitmap(string url) {
            if (string.IsNullOrEmpty(url)) {
                return null;
            }

            try {
                using var client = new HttpClient() {
                    DefaultRequestHeaders = {
                        {"User-Agent", "Other"}
                    },
                    Timeout = TimeSpan.FromSeconds(30)
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