using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PhoneticAssistantViewModel : ViewModelBase {
        public class G2pOption {
            public string Name { get; }
            public Func<G2pPack> Create { get; }

            private G2pOption(string name, Func<G2pPack> create) {
                Name = name;
                Create = create;
            }

            public static G2pOption CreateFor<T>() where T : G2pPack, new() {
                return new G2pOption(typeof(T).Name, () => new T());
            }

            public override string ToString() => Name;
        }

        public List<G2pOption> G2ps => g2ps;

        [Reactive] public G2pOption? G2p { get; set; }
        [Reactive] public string? Grapheme { get; set; }
        [Reactive] public string Phonemes { get; set; }

        private readonly List<G2pOption> g2ps = new List<G2pOption>() {
            G2pOption.CreateFor<ArpabetG2p>(),
            G2pOption.CreateFor<ArpabetPlusG2p>(),
            G2pOption.CreateFor<FrenchG2p>(),
            G2pOption.CreateFor<FrenchMillefeuilleG2p>(),
            G2pOption.CreateFor<GermanG2p>(),
            G2pOption.CreateFor<GermanMarzipanG2p>(),
            G2pOption.CreateFor<ItalianG2p>(),
            G2pOption.CreateFor<PortugueseG2p>(),
            G2pOption.CreateFor<RussianG2p>(),
            G2pOption.CreateFor<SpanishG2p>(),
            G2pOption.CreateFor<KoreanG2p>(),
            G2pOption.CreateFor<FilipinoG2p>(),
        };

        private Api.G2pPack? g2p;

        public PhoneticAssistantViewModel() {
            G2p = g2ps.FirstOrDefault(x=>x.Name == Preferences.Default.PhoneticAssistant) ?? g2ps.First();
            Grapheme = string.Empty;
            Phonemes = string.Empty;
            this.WhenAnyValue(x => x.G2p)
                .Subscribe(option => {
                    g2p = null;
                    if (option != null) {
                        g2p = option.Create();
                        Preferences.Default.PhoneticAssistant = option.Name;
                        Preferences.Save();
                        Refresh();
                    }
                });
            this.WhenAnyValue(x => x.Grapheme)
                .Subscribe(_ => Refresh());
        }

        private void Refresh() {
            if (Grapheme == null || g2p == null) {
                Phonemes = string.Empty;
                return;
            }
            string[] phonemes = g2p.Query(Grapheme);
            if (phonemes == null) {
                Phonemes = string.Empty;
                return;
            }
            Phonemes = string.Join(' ', phonemes);
        }
    }
}
