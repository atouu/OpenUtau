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
        [Reactive] private bool _enableLitterbox;

        public PreferencesViewModel() {
            _applicationId = Preferences.Default.ApplicationId;
            _enableLitterbox = Preferences.Default.EnableLitterbox;

            this.WhenAnyValue(vm => vm.EnableLitterbox)
                .Subscribe(enableLitterbox => {
                    Preferences.Default.EnableLitterbox = enableLitterbox;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ApplicationId)
                .Subscribe(applicationId => {
                    Preferences.Default.ApplicationId = applicationId;
                    Preferences.Save();
                });
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