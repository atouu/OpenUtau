using OpenUtau.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtauDRPC.ViewModels {

    public class PreferencesViewModel : ReactiveObject {
        [Reactive] public string ApplicationId { get; set; }
        public Dictionary<string, string> SingerIconUrls => new(Preferences.Default.SingerIconUrls);
        public IEnumerable<string> Singers => SingerManager.Inst.Singers.Values.Select(v => v.Name);

        [Reactive] public string SelectedSinger { get; set; }
        [Reactive] public string SelectedSingerIconUrl { get; set; }

        public PreferencesViewModel() {
            ApplicationId = Preferences.Default.ApplicationId;
        }

        public void Add() {
            Preferences.Default.SingerIconUrls[SelectedSinger] = SelectedSingerIconUrl;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(SingerIconUrls));
        }

        public void Delete(object key) {
            Preferences.Default.SingerIconUrls.Remove((string) key);
            Preferences.Save();
            this.RaisePropertyChanged(nameof(SingerIconUrls));
        }
    }
}