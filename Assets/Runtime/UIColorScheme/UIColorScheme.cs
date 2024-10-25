using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Colors {
    public class UIColorScheme : ISerializableID, IStorageElementExtraData {
        
        [PreloadStorage]
        public static Storage<UIColorScheme> storage = new("UIColor", TextCatalog.StreamingAssets);
        public static UIColorScheme current = new();
        
        [OnLaunch]
        static void OnLaunch() {
            var def = storage.GetDefault<UIColorScheme>();
            if (def != null)
                current.Apply(def);
        }
        
        public string ID {get; set;}
        
        public Dictionary<string, ColorEntry> colors = new();
        
        public StorageElementFlags storageElementFlags { get; set; }
        
        public bool GetColor(string key, out Color color) {
            if (colors.TryGetValue(key, out var entry)) {
                color = entry.color;
                return true;
            }
                
            
            color = default;
            return false;
        }
                
        public Color GetColor(string key) {
            return GetColor(key, out var result) ? result : Color.white;
        }
        
        public void SetColor(string key, Color color) {
            colors[key] = new ColorEntry {
                key = key,
                color = color
            };
        }

        public void Apply(UIColorScheme scheme) {
            foreach (var entry in scheme.colors.Values) 
                SetColor(entry.key, entry.color);
        }
        
        public struct ColorEntry {
            public Color color;
            public string key;
        }

        public void Serialize(IWriter writer) {
            writer.Write("ID", ID);
            writer.Write("storageElementFlags", storageElementFlags);
            writer.Write("colors", colors.Values
                .Where(p => !p.key.IsNullOrEmpty())
                .GroupBy(p => p.key)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().color));
        }

        public void Deserialize(IReader reader) {
            ID = reader.Read<string>("ID");
            storageElementFlags = reader.Read<StorageElementFlags>("storageElementFlags");
            colors = reader.ReadDictionary<Color>("colors")
                .ToDictionary(p => p.Key, p => new ColorEntry {
                    key = p.Key,
                    color = p.Value
                });
        }
    }
    
    
    public static class UIColorSchemeExtensions {
        public static void Repaint(this GameObject gameObject, UIColorScheme scheme) {
            foreach (var component in gameObject.GetComponentsInChildren<ColorSchemeRepaintTag>(true)) {
                if (component.global)
                    component.Refresh();
                else
                    component.Refresh(scheme);
            }
        }
        
        public static string ColorizeUI(this string text, string uiColor) {
            if (UIColorScheme.current.GetColor(uiColor, out var color))
                return text.Colorize(color);
            return text;
        }
    }
}