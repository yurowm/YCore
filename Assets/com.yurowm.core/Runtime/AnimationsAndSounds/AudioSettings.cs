using Yurowm.Integrations;
using Yurowm.Serialization;
using Yurowm.Sounds;

namespace Yurowm.Audio {
    public class AudioSettings : SettingsModule {
                
        float sfx = 1;
        float music = 1;

        public override void Initialize() {
            SFX = sfx;
            Music = music;
        }
        
        public float SFX {
            get => sfx;
            set {
                if (sfx == value) return;
                sfx = value.Clamp01();
                SetDirty();
            }
        }

        public float Music {
            get => music;
            set {
                if (music != value) {
                    music = value.Clamp01();
                    SetDirty();
                }
            }
        }

        public override void Serialize(IWriter writer) {
            writer.Write("sfx", sfx);
            writer.Write("music", music);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("sfx", ref sfx);
            reader.Read("music", ref music);
        }
    }
}