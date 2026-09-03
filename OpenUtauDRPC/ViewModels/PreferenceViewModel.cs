using OpenUtau.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenUtauDRPC.ViewModels {

    public partial class PreferencesViewModel : ReactiveObject {
        [Reactive] private string _applicationId;
        public Dictionary<string, string> SingerIconUrls => new(Preferences.Default.SingerIconUrls);
        public IEnumerable<string> Singers => SingerManager.Inst.Singers.Values.Select(v => v.Name);

        [Reactive] private string _selectedSinger;
        [Reactive] private string _selectedSingerIconUrl;

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