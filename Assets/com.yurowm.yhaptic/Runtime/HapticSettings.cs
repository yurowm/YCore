using UnityEngine.Serialization;
using Yurowm.Integrations;
using Yurowm.Serialization;
using Yurowm.UI;

namespace Yurowm {
    public class HapticSettings : SettingsModule {
        public bool enabled = true;

        public override void Initialize() {
            base.Initialize();
            SetVibeEnable(enabled);
        }
        
        public void SetVibeEnable(bool value) {
            if (enabled != value) {
                enabled = value;
                SetDirty();
                if (enabled)
                    ContentSound.Shot("HapticActivated", null);
            }
        }
        
        [ReferenceValue("HapticEnabled")]
        static int HapticEnabled() {
            return GameSettings.Instance.GetModule<HapticSettings>().enabled ? 1 : 0;
        }
        
        public override void Serialize(IWriter writer) {
            writer.Write("enabled", enabled);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("enabled", ref enabled);
        }
    }
}