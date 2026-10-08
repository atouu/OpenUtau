using System.Drawing.Imaging;
using Avalonia.Media.Imaging;
using DiscordRPC;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtauDRPC {
    public class UDiscordRPC : ICmdSubscriber {

        private const string OpenUtauIcon = "https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png";
        private const string OpenUtauSite = "https://openutau.com/";

        private int currentTrack = -1;
        private readonly DiscordRpcClient client;
        private Task? queue;

        public UDiscordRPC() {
            client = new DiscordRpcClient(Preferences.Default.ApplicationId);

            client.Initialize();

            client.SetPresence(new RichPresence() {
                Assets = new Assets() {
                    LargeImageKey = OpenUtauIcon,
                    LargeImageText = "OpenUtau"
                },
                Buttons = [
                    new() { Label = "Visit OpenUtau", Url = OpenUtauSite }
                ]
            });

            UpdateProject(DocManager.Inst.Project);
        }

        private async void UpdateSinger(USinger singer) {
            var iconUrl = Preferences.Default.SingerIconUrls.GetValueOrDefault(singer.Name);
            client.UpdateSmallAsset(OpenUtauIcon, singer.Name);
            if (Preferences.Default.EnableLitterbox && (iconUrl == null || !(await Http.IsValid(iconUrl)))) {
                if (queue?.Status != TaskStatus.Running) {
                    queue = Task.Run(async () => {
                        var queueName = singer.Name;
                        using MemoryStream ms = new MemoryStream(singer.AvatarData);
                        using Bitmap bmp = new Bitmap(ms);
                        using MemoryStream png = new MemoryStream();
                        bmp.Save(png, new PngBitmapEncoderOptions());
                        try {
                            var newUrl = await Litterbox.Upload(png);
                            if (queueName == singer.Name) {
                                client.UpdateSmallAsset(newUrl, queueName);
                            }

                            Preferences.Default.SingerIconUrls[queueName] = newUrl;
                            Preferences.Save();
                        } catch (Exception e) {
                            Console.WriteLine(e.Message);
                            client.UpdateSmallAsset(OpenUtauIcon, queueName);
                        }
                    });
                }
            } else {
                if (iconUrl != null) {
                    client.UpdateSmallAsset(iconUrl, singer.Name);
                }
            }
            UpdateSingerButton(singer.Web);
        }

        private void UpdateSingerButton(string site) {
            if (string.IsNullOrEmpty(site)) {
                client.UpdateButtons([
                    new() { Label = "Visit OpenUtau", Url = OpenUtauSite }
                ]);
            } else {

                client.UpdateButtons([
                    new() { Label = "Visit OpenUtau", Url = OpenUtauSite },
                    new() { Label = "Visit Singer Website", Url = $"{new UriBuilder(site).Uri}" }
                ]);
            }
        }

        private void UpdateProject(UProject project) {
            string projectName = Path.GetFileName(project.FilePath);
            if (string.IsNullOrEmpty(projectName)) {
                projectName = project.name;
            }
            client.UpdateDetails($"In Project: {projectName}");
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadPartNotification loadPart) {
                currentTrack = loadPart.part.trackNo;
                client.UpdateState($"Editing Track {currentTrack + 1} - {loadPart.part.name}");
                USinger trackSinger = loadPart.project.tracks[currentTrack].Singer;
                if (string.IsNullOrEmpty(trackSinger?.Name)) {
                    client.UpdateSmallAsset(string.Empty);
                } else {
                    UpdateSinger(trackSinger);
                }
            } else if (cmd is TrackChangeSingerCommand singerChange) {
                if (currentTrack != -1 && currentTrack == singerChange.track.TrackNo) {
                    UpdateSinger(singerChange.track.Singer);
                }
            } else if (cmd is LoadProjectNotification loadProject) {
                UpdateProject(loadProject.project);
                client.UpdateState(null);
                client.UpdateSmallAsset(string.Empty);
            }
        }
    }

}