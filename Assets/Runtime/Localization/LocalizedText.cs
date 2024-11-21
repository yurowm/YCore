using System.Collections;
using Yurowm.Serialization;

namespace Yurowm.Localizations {
    public class LocalizedText : ISerializable, ILocalized {
        
        public bool localized;
        public string text = "";
        public string key = "";
        
        public void Serialize(IWriter writer) {
            writer.Write("localized", localized);
            if (localized)
                writer.Write("key", key);
            else
                writer.Write("text", text);
        }

        public void Deserialize(IReader reader) {
            reader.Read("localized", ref localized);
            if (localized) 
                reader.Read("key", ref key);
            else
                reader.Read("text", ref text);
        }

        public string GetText() {
            if (localized)
                return Localization.content?[key] ?? key;
            return text;
        }

        public override string ToString() {
            if (localized)
                return $"[{key}]";
            
            return text;
        }

        public IEnumerable GetLocalizationKeys() {
            if (localized)
                yield return key;
        }
    }
}