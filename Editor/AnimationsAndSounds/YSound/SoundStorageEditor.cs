using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Sounds;
using Yurowm.Utilities;

namespace Yurowm.Editors {
    [DashboardGroup("Content")]
    [DashboardTab("Sounds", "Melody")]
    public class SoundStorageEditor : StorageEditor<SoundBase> {
        public override string GetItemName(SoundBase item) {
            return item.ID;
        }

        public override Storage<SoundBase> OpenStorage() {
            return SoundBase.storage;
        }

        int unusedTag;
        int legacyTag;
        int missedTag;
        
        public override bool Initialize() {
            unusedTag = tags.New("Unused", .1f);
            legacyTag = tags.New("Legacy", .5f);
            missedTag = tags.New("Missed", 0f);
            return base.Initialize();
        }
        
        protected override void UpdateTags(SoundBase item) {
            base.UpdateTags(item);
            tags.Set(item, unusedTag, item.tag.HasFlag(SoundBase.Tag.Unused));
            tags.Set(item, legacyTag, item.tag.HasFlag(SoundBase.Tag.Legacy));
            tags.Set(item, missedTag, item.tag.HasFlag(SoundBase.Tag.Missed));
        }

        protected override void OnItemsContextMenu(GenericMenu menu, SoundBase[] items) {
            base.OnItemsContextMenu(menu, items);
            
            menu.AddItem(new GUIContent("Find References"), false, () => {
                var components = ReferenceScanner
                    .GetReferences<ContentSound>()
                    .SelectMany(r => (r.reference as GameObject)?.GetComponentsInChildren<ContentSound>())
                    .ToArray();
                
                
                var builder = new StringBuilder();
                
                builder.AppendLine("References:");
                builder.AppendLine();

                foreach (var component in components) {
                    foreach (var clip in component.clips) {
                        if (items.Any(i => clip.clip == i.ID)) {
                            builder.AppendLine($"{AssetDatabase.GetAssetPath(component)}: {clip.name}");
                        }
                    }
                }
                
                Debug.Log(builder.ToString());
            });
        }
    }
}