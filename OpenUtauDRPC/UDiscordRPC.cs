using DiscordRPC;
using OpenUtau.Core;
using OpenUtau.Core.Util;

namespace OpenUtauDRPC {
    public class UDiscordRPC : ICmdSubscriber {

        public DiscordRpcClient client;

        public UDiscordRPC() {
            client = new DiscordRpcClient(Preferences.Default.ApplicationId) {
                Logger = new DiscordRPC.Logging.ConsoleLogger(DiscordRPC.Logging.LogLevel.Info, true)
            };

            client.OnReady += (sender, e) => {
                Console.WriteLine("Connected to discord with user {0}", e.User.Username);
                Console.WriteLine("Avatar: {0}", e.User.GetAvatarURL(User.AvatarFormat.WebP));
                Console.WriteLine("Decoration: {0}", e.User.GetAvatarDecorationURL());
            };

            client.Initialize();

            client.SetPresence(new RichPresence() {
                Assets = new Assets() {
                    LargeImageKey = "https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png",
                    LargeImageText = "OpenUtau"
                }
            });

            DocManager.Inst.AddSubscriber(this);
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadPartNotification loadPart) {
                client.UpdateState($"Editing Track {loadPart.part.trackNo + 1} - {loadPart.part.name}");
                string? trackSinger = loadPart.project.tracks[loadPart.part.trackNo].Singer?.Name;
                if (!string.IsNullOrEmpty(trackSinger)
                    && Preferences.Default.SingerIconUrls.TryGetValue(trackSinger, out var iconUrl)) {
                    client.UpdateSmallAsset(iconUrl, trackSinger);
                } else {
                    client.UpdateSmallAsset("https://raw.githubusercontent.com/stakira/OpenUtau/refs/heads/pages/docs/assets/images/openutau.png", trackSinger);
                }
            } else if (cmd is LoadProjectNotification loadProject) {
                string projectName = Path.GetFileName(loadProject.project.FilePath);
                if (string.IsNullOrEmpty(projectName)) {
                    projectName = loadProject.project.name;
                }
                client.UpdateDetails($"In Project: {projectName}");
            }
        }
    }

}