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
                }
            });

            UpdateProject(DocManager.Inst.Project);
        }

        private void UpdateSinger(string singerName) {
            if (!string.IsNullOrEmpty(singerName)
                && Preferences.Default.SingerIconUrls.TryGetValue(singerName, out var iconUrl)) {
                client.UpdateSmallAsset(iconUrl, singerName);
            } else {
                client.UpdateSmallAsset($"https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png", singerName);
            }
        }

        private void UpdateProject(UProject project) {
            string projectName = Path.GetFileName(project.FilePath);
            if (string.IsNullOrEmpty(projectName)) {
                projectName = project.name;
            }
            client.UpdateDetails($"In Project: {projectName}");
        }

        private void ClearStatus() {
            client.UpdateState(null);
            client.UpdateSmallAsset();
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadPartNotification loadPart) {
                currentTrack = loadPart.part.trackNo;
                client.UpdateState($"Editing Track {currentTrack + 1} - {loadPart.part.name}");
                string trackSinger = loadPart.project.tracks[currentTrack].Singer?.Name;
                if (string.IsNullOrEmpty(trackSinger)) {
                    client.UpdateSmallAsset();
                } else {
                    UpdateSinger(trackSinger);
                }
            } else if (cmd is TrackChangeSingerCommand singerChange) {
                if (currentTrack != -1 && currentTrack == singerChange.track.TrackNo) {
                    UpdateSinger(singerChange.track.Singer.Name);
                }
            } else if (cmd is LoadProjectNotification loadProject) {
                UpdateProject(loadProject.project);
                ClearStatus();
            }
        }
    }

}