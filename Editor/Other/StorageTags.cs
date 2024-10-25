using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;
using Yurowm.Utilities;

namespace Yurowm.Editors {
    public class StorageTags<S> {

        string filterKey;
        
        int _filter = 0;
        int filter {
            get {
                if (filterKey.IsNullOrEmpty())
                    return _filter;
                return EditorStorage.Instance.GetInt(filterKey);
            }
            set {
                if (filterKey.IsNullOrEmpty())
                    _filter = value;
                else
                    EditorStorage.Instance.SetNumber(filterKey, value);
            }
        }
        
        bool overlap = false;

        List<Tag> tags = new List<Tag>();
        Dictionary<S, int> tagMasks = new Dictionary<S, int>();

        public void SetName(string name) {
            filterKey = $"storageFilter_{name}";
        }

        public int New(string name) {
            return New(name, YRandom.staticMain.Value(name.CheckSum()));
        }
        
        public int New(string name, float hue) {
            return New(name, new HSBColor(hue, 0.5f, 1f).ToColor());
        }
        
        public int New(string name, Color color) {
            var tag = tags.FirstOrDefault(t => t.name == name)
                      ?? EmitNewTag(name);
            tag.color = color;
            return tag.ID;
        }
        
        Tag EmitNewTag(string name) {
            if (tags.Count >= 32)
                throw new Exception("There are already 32 tags!");

            var result = new Tag(name, 1 << tags.Count);
            tags.Add(result);
            
            return result;
        }
        
        public Rect DrawLabelTags(Rect labelRect, S item) {
            foreach (var tag in GetAll(item)) 
                labelRect = ItemIconDrawer.DrawTag(labelRect, tag.name, tag.color, ItemIconDrawer.Side.Right);

            return labelRect;
        }
        
        public void Set(S item, int tagID, bool value) {
            if (!tagMasks.TryGetValue(item, out var mask)) {
                mask = 0;
                tagMasks.Add(item, mask);
            }
            
            var currentValue = (mask & tagID) != 0;
            
            if (currentValue == value)
                return;

            if (value)
                mask = mask | tagID;
            else
                mask = mask & ~tagID;
                
            tagMasks[item] = mask;
        }
        
        bool Has(S item, int tagMask, bool overlap = false) {
            if (!tagMasks.TryGetValue(item, out var links)) 
                return false;
            
            return Has(links, tagMask, overlap);
        }
        
        bool Has(int itemTagMask, int tagMask, bool overlap = false) {
            if (itemTagMask == 0) 
                return false;
            
            if (overlap) 
                return (itemTagMask & tagMask) == tagMask;
            else
                return (itemTagMask & tagMask) != 0;
        }
        
        public IEnumerable<Tag> GetAll(S item) {
            if (!tagMasks.TryGetValue(item, out var links) || links == 0) 
                yield break;
            
            for (var i = 0; i < tags.Count; i++) {
                var link = 1 << i;
                
                if ((links & link) != 0)
                    yield return tags[i];
            }
        }
        
        void SetFilter(HierarchyList<S> list, int filter) {
            this.filter = filter;
            
            if (filter == 0) 
                list.SetFilter(null);
            else
                list.SetFilter(i => Has(i.content, this.filter, overlap));
        }
        
        public void Clear() {
            tagMasks.Clear();
        }
        
        static readonly Color filterButtonColor = new Color(1f, 0.49f, 0.82f);
        
        public void FilterButton(HierarchyList<S> list) {
            if (!tags.IsEmpty()) {
                using (filter != 0 ? GUIHelper.Color.Start(filterButtonColor) : null) 
                    if (GUILayout.Button("Filter", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                        var menu = new GenericMenu();
                        
                        var activeTags = tags
                            .Where(t => list.itemCollection.Any(i => Has(i, t.ID)))
                            .ToArray();
                        
                        if (filter != 0)
                            menu.AddItem(new GUIContent("Clear"), false, () => {
                                SetFilter(list, 0);  
                            });
                        
                        if (activeTags.Length > 1)
                            menu.AddItem(new GUIContent("Overlap"), overlap, () => {
                                overlap = !overlap;
                                SetFilter(list, filter);
                            });

                        if (menu.GetItemCount() > 0)
                            menu.AddSeparator("");

                        if (activeTags.IsEmpty())
                            menu.AddDisabledItem(new GUIContent("No elements with tag"));
                        else
                            foreach (var tag in activeTags.OrderBy(t => t.name)) {
                                var _t = tag;
                                var enabled = (filter & tag.ID) != 0;

                                menu.AddItem(new GUIContent(tag.name), enabled, () => {
                                    if (enabled) 
                                        SetFilter(list, filter & ~_t.ID);
                                    else
                                        SetFilter(list, filter | _t.ID);
                                });
                            }
                        
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
            }
        }
        
        public void SetFilter(HierarchyList<S> list) {
            SetFilter(list, filter);
        }
        
        public class Tag {
            public readonly string name;
            public readonly int ID;
            public Color color;

            public Tag(string name, int ID) {
                this.name = name;
                this.ID = ID;
            }
        }
    }
}