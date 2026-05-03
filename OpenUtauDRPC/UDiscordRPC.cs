using DiscordRPC;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtauDRPC {
    public class UDiscordRPC : ICmdSubscriber {

        private int currentTrack = -1;
        public DiscordRpcClient client;

        public UDiscordRPC() {
            client = new DiscordRpcClient(Preferences.Default.ApplicationId);

            client.Initialize();

            client.SetPresence(new RichPresence() {
                Assets = new Assets() {
                    LargeImageKey = "https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png",
                    LargeImageText = "OpenUtau"
                },
                Buttons = [
                    new() { Label = "Visit OpenUtau", Url = "https://openutau.com/" }
                ]
            });

            UpdateProject(DocManager.Inst.Project);
        }

        private void UpdateSinger(USinger singer) {
            if (Preferences.Default.SingerIconUrls.TryGetValue(singer.Name, out var iconUrl)) {
                client.UpdateSmallAsset(iconUrl, singer.Name);
            } else {
                client.UpdateSmallAsset("https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png", singer.Name);
            }
            UpdateSingerButton(singer.Web);
        }

        private void UpdateSingerButton(string site) {
            if (string.IsNullOrEmpty(site)) {
                client.UpdateButtons([
                    new() { Label = "Visit OpenUtau", Url = "https://openutau.com/" }
                ]);
            } else {

                client.UpdateButtons([
                    new() { Label = "Visit OpenUtau", Url = "https://openutau.com/" },
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