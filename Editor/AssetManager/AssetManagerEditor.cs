using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;
using Object = UnityEngine.Object;

namespace Yurowm.ContentManager {
    [DashboardGroup("Content")]
    [DashboardTab("Assets", "ContentTabIcon", -9999)]
    public class AssetManagerEditor : DashboardEditor<AssetManager> {

        AssetList assetList;

        GUIHelper.SearchPanel searchPanel;

        static bool needToBeUpdated = false;

        public override bool Initialize() {
            if (!metaTarget) {
                Debug.LogError("Asset Manager is not found");
                return false;
            }

            SortItems();
            
            assetList = new AssetList(metaTarget.aItems);
            assetList.onSelectedItemChanged = s => Selection.objects = s.Select(x => x.item).ToArray();

            searchPanel = new GUIHelper.SearchPanel("");

            return metaTarget;
        }
        
        void SortItems() {
            var sorted = metaTarget.aItems
                .Where(i => i.item != null)
                .OrderBy(i => i.path)
                .ThenBy(i => i.item.name)
                .DistinctBy(i => i.item)
                .ToArray();
            
            metaTarget.aItems.Reuse(sorted);
        }

        [MenuItem("Assets/Add To/Asset Manager")]
        public static void AddTo() {
            if (!AssetManager.Instance) {
                Debug.LogError("Asset Manager is not found");
                return;
            }

            AssetManager.Instance.Initialize();
                
            foreach (string guid in Selection.assetGUIDs) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IsNullOrEmpty()) continue;
                
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (!asset) continue;
                
                if (AssetManager.Instance.aItems.All(x => x.item != asset))
                    AssetManager.Instance.aItems.Add(new AssetManager.Item(asset));
            }
            
            needToBeUpdated = true;
        }

        void Sort() {
            SortItems();
            assetList.MarkAsChanged();
        }

        public override void OnGUI() {
            if (!metaTarget) {
                EditorGUILayout.HelpBox("Asset Manager is missing", MessageType.Error);
                return;
            }

            if (needToBeUpdated) {
                assetList.MarkAsChanged();
                needToBeUpdated = false;
            }

            Undo.RecordObject(metaTarget, "");
            
            using (GUIHelper.Vertical.Start(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                assetList.OnGUI();

            EditorUtility.SetDirty(metaTarget);
        }

        public override void OnToolbarGUI() {
            if (GUILayout.Button("New Group", EditorStyles.toolbarButton, GUILayout.Width(80)))
                assetList.AddNewFolder(null, AssetList.newGroupNameFormat);

            if (GUILayout.Button("Sort", EditorStyles.toolbarButton, GUILayout.Width(50)))
                Sort();

            searchPanel.OnGUI(x => assetList.SetSearchFilter(x), GUILayout.ExpandWidth(true));
        }

        public override AssetManager FindTarget() {
            return AssetManager.Instance;
        }

        static A CastAsset<A>(Object asset) where A : Object {
            return CastAsset(typeof(A), asset) as A;
        }

        static Object CastAsset(Type assetType, Object asset) {
            if (assetType.IsInstanceOfType(asset))
                return asset;
            
            if (typeof(Component).IsAssignableFrom(assetType)) {
                if (asset is GameObject gameObject)
                    return gameObject.GetComponent(assetType);
            }
            
            return null;
        }
        
        public static void OnSelectGUI<A>(string label, A selected, Action<A> onChange, 
            Func<A, bool> filter = null, params GUILayoutOption[] options) where A : Object {
            OnSelectGUI(label, selected, asset => {
                if (!asset) return filter?.Invoke(null) ?? true;
                var comp = CastAsset<A>(asset);
                return comp && (filter?.Invoke(comp) ?? true);
            }, newAsset => {
                selected = CastAsset<A>(newAsset);
                onChange?.Invoke(selected);
            }, options);
        }        
        
        public static void OnSelectGUI(string label, Type assetType, Object selected, Action<Object> onChange, 
            Func<Object, bool> filter = null, params GUILayoutOption[] options) {
            OnSelectGUI(label, selected, asset => {
                if (!asset) return filter?.Invoke(null) ?? true;
                var comp = CastAsset(assetType, asset);
                return comp && (filter?.Invoke(comp) ?? true);
            }, newAsset => {
                selected = CastAsset(assetType, newAsset);
                onChange?.Invoke(selected);
            }, options);
        }        
         
        public static Object OnSelectGUI(string label, Object selected, 
            Func<Object, bool> filter = null,
            Action<Object> onChange = null, params GUILayoutOption[] options) {

            AssetManager.Instance.Initialize();
            
            Rect rect = EditorGUILayout.GetControlRect(options);
            if (!label.IsNullOrEmpty())
                rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            
            if (AssetManager.Instance.aItems.Count > 0) {
                if (filter == null) 
                    filter = o => true;
                else if (!filter.Invoke(selected)) 
                    selected = AssetManager.Instance.aItems.Select(i => i.item).FirstOrDefault(filter);
            }

            if (GUI.Button(rect, selected?.name ?? "<NULL>", EditorStyles.popup)) {
                AssetManager.Instance.Initialize();
                
                GenericMenu menu = new GenericMenu();

                if (filter.Invoke(null))
                    menu.AddItem(new GUIContent("<NULL>"), selected == null, 
                        () => {
                            selected = null;
                            onChange?.Invoke(selected);
                        });
                
                foreach (var item in AssetManager.Instance.aItems.OrderBy(i => $"{i.path}/{i.item.name}")) {
                    if (!filter.Invoke(item.item)) continue;
                    var _item = item;
                    menu.AddItem(new GUIContent(item.path + "/" + item.item.name), item.item == selected, 
                        () => {
                            selected = _item.item;
                            onChange?.Invoke(selected);
                        });
                }
                    
                menu.DropDown(rect);
            }
                
            return selected;
        }
        
        class AssetList : HierarchyList<AssetManager.Item> {

            public AssetList(List<AssetManager.Item> collection) : base(collection, new List<TreeFolder>(), new TreeViewState()) {}

            internal const string newGroupNameFormat = "Folder{0}";
            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                if (selected.Count == 0) {
                    menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(null, newGroupNameFormat));
                } else {
                    if (selected.Count == 1 && selected[0].isFolderKind) {
                        FolderInfo parent = selected[0].asFolderKind;

                        menu.AddItem(new GUIContent("Add New Group"), false, () => AddNewFolder(parent, newGroupNameFormat));
                    } else {
                        FolderInfo parent = selected[0].parent;
                        if (selected.All(x => x.parent == parent))
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, parent, newGroupNameFormat));
                        else 
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, root, newGroupNameFormat));
                       
                    }

                    menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
                }
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                rect = ItemIconDrawer.DrawAuto(rect, info.content.item);
                base.DrawItem(rect, info);
            }

            public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
                result = null;

                if (AssetManager.Instance.aItems.All(x => x.item != o)) {
                    ItemInfo item = new ItemInfo(0, null);
                    item.content = new AssetManager.Item(o);
                    result = item;
                    return true;
                }

                return false;
            }

            public override string GetPath(AssetManager.Item element) {
                return element.path;
            }

            public override void SetPath(AssetManager.Item element, string path) {
                element.path = path;
            }

            public override string GetName(AssetManager.Item element) {
                return element.item ? $"{element.item.name} <i>({element.item.GetType()})</i>" : "null";
            }

            public override int GetUniqueID(AssetManager.Item element) {
                return element.item.GetInstanceID();
            }

            public override AssetManager.Item CreateItem() {
                return null;
            }

            public override bool CanRename(ItemInfo info) {
                return false;
            }
        }

    }
}