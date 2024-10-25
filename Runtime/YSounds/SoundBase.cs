using System.Collections.Generic;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public abstract class SoundBase : ISerializableID, IStorageElementPath {
        
        [PreloadStorage]
        public static Storage<SoundBase> storage = new("Sounds", TextCatalog.StreamingAssets);

        public enum Tag {
            Unused = 1 << 0,
            UsedForce = 1 << 1,
            Legacy = 1 << 2,
            Missed = 1 << 3
        }
        
        public Tag tag;
        
        public abstract void Play(params object[] args);
        public string ID { get; set; }
        
        public string sePath { get; set; }
        
        public virtual void Serialize(IWriter writer) {
            writer.Write("ID", ID);
            writer.Write("tag", tag);
            writer.Write("sePath", sePath);
        }

        public virtual void Deserialize(IReader reader) {
            ID = reader.Read<string>("ID");
            sePath = reader.Read<string>("sePath");
            reader.Read("tag", ref tag);
        }
    }
}