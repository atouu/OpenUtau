using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OpenUtauDRPC.ViewModels;

namespace OpenUtauDRPC.Views {
    public partial class PreferencesDialog : Window {
        public PreferencesDialog() {
            DataContext = new PreferencesViewModel();
            AvaloniaXamlLoader.Load(this);
        }
    }
}
