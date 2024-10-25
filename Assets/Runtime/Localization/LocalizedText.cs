using System.Collections;
using Yurowm.Serialization;

namespace Yurowm.Localizations {
    public class LocalizedText : ISerializable {
        
        public string text = "";
        
        public void Serialize(IWriter writer) {
            writer.Write("text", text);
        }

        public void Deserialize(IReader reader) {
            reader.Read("text", ref text);
        }

        public string GetText() {
            return text;
        }

        public override string ToString() {
            return text;
        }
    }
}