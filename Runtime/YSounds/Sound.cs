using System.Collections.Generic;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public abstract class Sound : SoundBase {
        public List<Module> modules = new ();
        
        public override void Play(params object[] args) {
            modules.ForEach(m => m.OnPlay());
        }
        
        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("modules", modules);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            modules.Reuse(reader.ReadCollection<Module>("modules"));
        }
        
        public abstract class Module: ISerializable {
            public abstract void OnPlay();

            public virtual void Serialize(IWriter writer) { }

            public virtual void Deserialize(IReader reader) { }
        }
    }
}