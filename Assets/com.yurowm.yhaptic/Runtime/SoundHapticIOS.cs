using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public class SoundHapticIOS : SoundEffect.Module {

        public HapticType type;
        
        public override void OnPlay() {
            if (SoundController.IsMute())
                return;

            if (Integration.Get<YHaptic>()?.GetActiveProvider() is YHapticIOS provider)
                provider.Play(type);
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("hapticType", type);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("hapticType", ref type);
        }
    }
    
    public class SoundHapticAHAP : SoundEffect.Module {

        public string path;
        
        public override void OnPlay() {
            if (SoundController.IsMute())
                return;
                
            var fullPath = SoundController.GetHapticFullPath(path);
                    
            if (!fullPath.IsNullOrEmpty())
                if (Integration.Get<YHaptic>()?.GetActiveProvider() is YHapticIOS provider)
                    provider.Play(fullPath);
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("path", path);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("path", ref path);
        }
    }
}


