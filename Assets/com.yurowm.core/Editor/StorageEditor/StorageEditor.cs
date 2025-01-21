using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Dashboard;
using Yurowm.Editors;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.HierarchyLists;
using Yurowm.Icons;
using Yurowm.InUnityReporting;
using Yurowm.ObjectEditors;
using Yurowm.Spaces;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {
    public abstract class StorageEditor<S> : DashboardEditor, IStorageEditor where S : class, ISerializable {
        protected Storage<S> storage { get; private set; }
        ItemsList list;
        List<S> selected = new();

        GUIHelper.LayoutSplitter splitter;
        GUIHelper.Scroll settingsScroll = new(GUILayout.ExpandHeight(true));
        GUIHelper.SearchPanel searchPanel = new("");

        public override bool isScrollable => false;

        bool rawView = false;

        Dictionary<S, SerializableReportEditor> rawViewDict = new();
        
        static Texture2D textIcon;
        static Texture2D saveIcon;
        static Texture2D plusIcon;
        static Texture2D menuIcon;

        #region New Item
        
        protected virtual bool CanCreateNewItem() {
            return true;
        }

        public void CreateNewItem() {
            try {
                var popup = DashboardPopup.Create<NewItemPopup>();
                popup.Setup(typeof(S), 
                    window as YDashboard,
                    o => {
                        if (o is S s) AddItem(s);
                    }, o => FilterNewItem(o as S, out popup.error));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        protected void DuplicateNewItem(S original) {
            try {
                var popup = DashboardPopup.Create<NewItemPopup>();
                popup.item = original.Clone();
                popup.Setup(typeof(S),
                    window as YDashboard,
                    o => {
                        if (o is S s) AddItem(s);
                    }, o => FilterNewItem(o as S, out popup.error));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        protected void AddItem(S item) {
            UpdateTags(item);
            storage.items.Add(item);
            
            Sort();
            
            list.Reload();
            
            selected.Clear();
            selected.Add(item);
        }

        protected virtual bool FilterNewItem(S item, out string reason) {
            if (item is ISerializableID sid) {
                var id = sid.ID;
                
                if (id.IsNullOrEmpty()) {
                    reason = "ID is empty";
                    return false;
                }
                
                if (storage.items
                    .CastIfPossible<ISerializableID>()
                    .Any(i => i.ID == id)) {
                    reason = "The storage already contains an item with such ID";
                    return false;
                }
            }
                
            reason = null;
            return true;
        }
        
        public virtual void SetupNewItem(S item) {
            if (item is ISerializableID sid && sid.ID.IsNullOrEmpty())
                sid.ID = item.GetType().Name;
        }
        
        #endregion

        public abstract string GetItemName(S item);
        
        public virtual string GetTitle(S item) {
            return GetItemName(item);
        }
        
        public virtual string GetFolderName(S item) {
            if (item is IStorageElementPath sep)
                return sep.sePath;
            return string.Empty;
        }
        
        public virtual void SetFolder(S item, string path) {
            if (item is IStorageElementPath sep)
                sep.sePath = path;
        }
        
        Rect DrawItemInternal(Rect rect, S item) {
            if (Event.current.type != EventType.Repaint) return default;
            
            rect = ItemIconDrawer.DrawAuto(rect, item);

            rect = tags.DrawLabelTags(rect, item);

            return DrawItem(rect, item);
        }

        protected virtual Rect DrawItem(Rect rect, S item) {
            return rect;
        }

        public abstract Storage<S> OpenStorage();
        
        public IEnumerable<ISerializable> GetStoredItems() {
            foreach (var item in storage)
                yield return item;
        }

        public override bool Initialize() {
            
            storage = OpenStorage();
            
            Sort();
            
            rawViewDict.Clear();
            
            list = new ItemsList(storage.items, 
                GetFolderName,
                SetFolder,
                GetItemName);
            list.onSelectedItemChanged += s => selected = s;
            list.newItem = CreateNewItem;
            list.duplicateItem = DuplicateNewItem;
            list.onContextMenu += OnItemsContextMenu;
            list.drawItem = DrawItemInternal;
            list.searchDataProvider = GetSearchData;
            
            tags.SetName(storage.Name);
            
            if (typeof(IStorageElementExtraData).IsAssignableFrom(typeof(S))) {
                defaultTag = tags.New("Default", .1f);
                wipTag = tags.New("WIP", .8f);
            }
            
            if (typeof(IPlatformExpression).IsAssignableFrom(typeof(S)))
                platformExpressionTag = tags.New("PExp", .9f);
                
            splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 250f);

            storage.items.ForEach(UpdateTags);
            
            tags.SetFilter(list);
            
            return true;
        }

        public override void OnGUI() {
            if (textIcon == null) textIcon = EditorIcons.GetUnityIcon("UnityEditor.ConsoleWindow@2x", "d_UnityEditor.ConsoleWindow@2x");
            if (saveIcon == null) saveIcon = EditorIcons.GetUnityIcon("SaveAs@2x", "d_SaveAs@2x");
            if (menuIcon == null) menuIcon = EditorIcons.GetUnityIcon("_Menu@2x", "d__Menu@2x");
            if (plusIcon == null) plusIcon = EditorIcons.GetIcon("PlusIcon");
            
            using (splitter.Start(true, true)) {
                if (splitter.Area()) {
                    list.OnGUI();
                }
                if (splitter.Area()) {
                    if (!selected.IsEmpty()) {
                        if (rawView) {
                            DrawRawItem(selected[0]);
                        } else {
                            using (settingsScroll.Start()) {
                                foreach (var item in selected.Take(10)) {
                                    if (item == null) return;
                                    GUILayout.Label(GetTitle(item), Styles.title);

                                    var type = item.GetType();
                                    
                                    GUILayout.Label($"<{item.GetType().FullName}>", Styles.miniLabelBlack);
                                    
                                    ObjectEditor.CopyPaste("Data", item); 
                                    
                                    BaseTypesEditor.OnSelectTypeGUI<S>("Type", type, t => {
                                        var newItem = Activator.CreateInstance(t) as S;
                                        Serializer.Instance.Deserialize(newItem, item.ToJson());
                                        Replace(item, newItem);
                                    }, false);
                                    
                                    ObjectEditor.Edit(item, this);
                                    UpdateTags(item);
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
            }
        }
        
        void Replace(S oldItem, S newItem) {
            if (oldItem == null) return;
            if (newItem == null) return;
            if (oldItem == newItem) return;
            
            var index = storage.items.IndexOf(oldItem);
            if (index >= 0) 
                storage.items[index] = newItem;
            
            index = selected.IndexOf(oldItem);
            if (index >= 0)
                selected[index] = newItem;
            
            var s = selected.ToArray();
            list.Reload();
            list.Select(s);
        }
        
        void DrawRawItem(S item) {
            if (!rawViewDict.TryGetValue(item, out var editor)) {
                editor = new SerializableReportEditor();
                var report = new SerializableReport(item);
                report.Refresh();
                editor.SetProvider(report);
                rawViewDict.Add(item, editor);
            }
            
            editor.OnGUI(GUILayout.ExpandHeight(true));
        }

        public virtual void OnStorageToolbarGUI() {}

        public override void OnToolbarGUI() {
            using (GUIHelper.Horizontal.Start(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
                if (rawView != GUILayout.Toggle(rawView, textIcon, EditorStyles.toolbarButton, GUILayout.Width(25))) {
                    rawView = !rawView;
                    if (rawView) rawViewDict.Clear();
                }
                
                if (GUILayout.Button(plusIcon, EditorStyles.toolbarButton, GUILayout.Width(25))) 
                    CreateNewItem();

                OnStorageToolbarGUI();
                
                searchPanel.OnGUI(list.SetSearchFilter, GUILayout.ExpandWidth(true)); 
                
                tags.FilterButton(list);
                
                if (GUILayout.Button(saveIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
                    Apply();
                
                if (GUILayout.Button(menuIcon, EditorStyles.toolbarButton, GUILayout.Width(25))) {
                    var menu = new GenericMenu();
                    OnOtherContextMenu(menu);
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }
        }
        
        protected virtual void OnOtherContextMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Load legacy"), false, () => {
                tags.Clear();
                var z = storage.ser;
                storage.ser = Serializator.GetSerializer();
                storage.Load().Complete();
                storage.ser = z;
                storage.items.ForEach(UpdateTags);
                Sort();
                list.itemCollection = storage.items;
                list.Reload();
            });
            menu.AddItem(new GUIContent("Reset"), false, () => {
                tags.Clear();
                storage.Load().Complete();
                storage.items.ForEach(UpdateTags);
                Sort();
                list.itemCollection = storage.items;
                list.Reload();
            });
            
            menu.AddItem(new GUIContent("Raw Data/Save to System Buffer"), false, () => {
                EditorGUIUtility.systemCopyBuffer = Serializer.Instance.Serialize(storage);
            });
            
            menu.AddItem(new GUIContent("Raw Data/Source to System Buffer"), false, async () =>
                EditorGUIUtility.systemCopyBuffer = await storage.GetSource());
            
            menu.AddItem(new GUIContent("Raw Data/Inject (Hard)"), false, () => {
                try {
                    var raw = EditorGUIUtility.systemCopyBuffer;
                    var reference = Serializer.Instance.Deserialize(raw);
                    if (reference != null && reference.GetType() == storage.GetType()) {
                        Serializer.Instance.Deserialize(storage, raw);
                        Sort();
                        list.itemCollection = storage.items;
                        list.Reload();
                        UnityEngine.Debug.Log("Successfully Injected");
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            });
            
            
            menu.AddItem(new GUIContent("Raw Data/Inject (Soft)"), false, () => {
                try {
                    var raw = EditorGUIUtility.systemCopyBuffer;
                    var reference = Serializer.Instance.Deserialize(raw);
                    if (reference != null && reference.GetType() == storage.GetType()) {
                        foreach (var rItem in (Storage<S>) reference) {
                            if (rItem is ISerializableID sid) {
                                var eItem = storage.GetItemByID(sid.ID);
                                if (eItem != null) 
                                    storage.items.Remove(eItem);
                            }
                            storage.items.Add(rItem);
                        }
                        Sort();
                        list.itemCollection = storage.items;
                        list.Reload();
                        UnityEngine.Debug.Log("Successfully Injected");
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            });
        }
        
        protected virtual void Sort() {
            storage.items.SortBy(GetItemName);
        }
        
        protected void Reload() {
            list.Reload();
        }
        
        public virtual void Apply() {
            storage.Apply().Complete();
        }

        protected virtual void OnItemsContextMenu(GenericMenu menu, S[] items) { }

        protected virtual IEnumerable GetSearchData(S item) {
            yield return item.GetType().FullName;
            
            if (item is ISerializableID sID)
                yield return sID.ID;
        }
        
        #region Tags
        
        protected StorageTags<S> tags = new StorageTags<S>();

        int defaultTag;
        int platformExpressionTag;
        int wipTag;
        
        protected void UpdateTags() {
            storage.items.ForEach(UpdateTags);
        }
        
        protected virtual void UpdateTags(S item) {
            if (item is IStorageElementExtraData extraData) {
                tags.Set(item, defaultTag, extraData.storageElementFlags.HasFlag(StorageElementFlags.DefaultElement));
                tags.Set(item, wipTag, extraData.storageElementFlags.HasFlag(StorageElementFlags.WorkInProgress));
            }
            
            if (item is IPlatformExpression ipe)
                tags.Set(item, platformExpressionTag, !ipe.platformExpression.IsNullOrEmpty());
        }

        #endregion
        
        public class ItemsList : HierarchyList<S> {
            List<IInfo> selected = new();
            Func<S, string> getName;
            Func<S, string> getFolderName;
            Action<S, string> setFolderName;
            
            public Func<S, IEnumerable> searchDataProvider;
            
            public Action newItem;
            
            public Action<GenericMenu, S[]> onContextMenu = delegate {};

            public ItemsList(List<S> collection, Func<S, string> getFolderName, Action<S, string> setFolderName, Func<S, string> getName) : base(collection, null, new TreeViewState()) {
                this.getName = getName;
                this.getFolderName = getFolderName;
                this.setFolderName = setFolderName;
                onSelectionChanged += selection => {
                    selected.Clear();
                    selected.AddRange(selection);
                };
                Reload();
                ExpandAll();
            }

            protected override void RowGUI(RowGUIArgs args) {
                if (getFolderName == null) {
                    if (!info.ContainsKey(args.item.id)) return;
                    IInfo i = info[args.item.id];
                    if (i.isItemKind) DrawItem(args.rowRect, i.asItemKind);
                } else
                    base.RowGUI(args);
            }
            
            public override string GetPath(S element) {
                return getFolderName?.Invoke(element) ?? "";
            }

            public override void SetPath(S element, string path) {
                setFolderName?.Invoke(element, path);
            }
            
            public override int GetUniqueID(S element) {
                return element.GetHashCode();
            }

            public override S CreateItem() {
                return null;
            }
            
            public Action<S> duplicateItem;
            public Func<Rect, S, Rect> drawItem;

            public override void DrawItem(Rect rect, ItemInfo info) {
                if (drawItem != null)
                    rect = drawItem.Invoke(rect, info.content);

                if (rect.width > 0)
                    base.DrawItem(rect, info);
            }
            
            public override string GetLabel(ItemInfo info) {
                return getName == null ? info.content.ToString() : getName(info.content);
            }

            public override string GetName(S element) {
                return element.ToString();
            }
            
            const string newGroupNameFormat = "Folder{0}";

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) { 
                selected = selected.Where(x => x.isItemKind).ToList();
                
                if (newItem != null)
                    menu.AddItem(new GUIContent("New"), false, newItem.Invoke);
                
                menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(null, newGroupNameFormat));
                
                if (selected.Count > 0) {
                    if (selected.Count == 1) {
                        if (duplicateItem != null) 
                            menu.AddItem(new GUIContent("Duplicate"), false, () => duplicateItem(selected[0].asItemKind.content));
                        if (selected[0].asItemKind.content is ISerializableID sid) 
                            menu.AddItem(new GUIContent("Copy ID"), false, () => EditorGUIUtility.systemCopyBuffer = sid.ID);
                        menu.AddItem(new GUIContent("Edit"), false, () => ObjectEditorWindow
                            .Show(selected[0].asItemKind.content, this, null, null, true));
                    }
                    
                    if (selected.Select(i => i.asItemKind.content).CastOne<IStorageElementPath>() != null) {
                        FolderInfo parent = selected[0].parent;
                        
                        if (selected.All(x => x.parent == parent))
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, parent, newGroupNameFormat));
                        else 
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, root, newGroupNameFormat));
                    }
                    
                    menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
                } 
                
                onContextMenu.Invoke(menu, selected.Select(x => x.asItemKind.content).ToArray());
            }

            protected override bool CanRename(TreeViewItem item) {
                var info = GetInfo(item.id);
                
                if (info == null)
                    return false;
                
                return info.isFolderKind;
            }
            
            protected virtual bool ItemSearchFilter(S item, string filter) {
                if (item is ISearchable searchable)
                    if (searchable.GetSearchData().Any(s => Search(s, filter)))
                        return true;
                if (searchDataProvider != null)
                    if (searchDataProvider.Invoke(item).Collect<string>().Any(s => Search(s, filter)))
                        return true;
                return false;
            }

            protected override bool ItemSearchFilter(IInfo item, string filter) {
                return base.ItemSearchFilter(item, filter) 
                    || item is ItemInfo i && ItemSearchFilter(i.content, filter);
            }

            public void Select(IEnumerable<S> selection) {
                var items = selection.ToList();
                var ids = items
                    .Select(GetUniqueID)
                    .ToHashSet();
                
                ids.UnionWith(selected.Where(s => s.isFolderKind).Select(f => f.index));
                
                SetSelection(ids.ToArray());
                
                onSelectedItemChanged(items);
                onSelectionChanged(items.Select(GetInfo).ToList());
            }
        }
    }

    public interface IStorageEditor {
        public IEnumerable<ISerializable> GetStoredItems();
    }
    
    class NewItemPopup : DashboardPopup {
        Func<object, bool> filter;
        Action<object> addItem;
        GUIHelper.Scroll scroll = new GUIHelper.Scroll();
        Type baseType;
        public object item;

        public string error;
        
        List<Type> types;
        TypeList typeList;
        GUIHelper.SearchPanel searchPanel = new GUIHelper.SearchPanel("");
        
        enum Mode {
            ItemEditing,
            TypeSelection
        }
        
        Mode mode = Mode.TypeSelection;
        
        public void Setup(Type baseType, YDashboard dashboard, 
            Action<object> addItem,
            Func<object, bool> filter) {
            
            if (dashboard == null) return;
            
            title = "New Item";
            this.filter = filter;
            this.addItem = addItem;
            this.baseType = baseType;
            level = Level.Editor;
         
            types = baseType
                .FindInheritorTypes(false)
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.FullName)
                .ToList();
            
            if (types.IsEmpty()) {
                Debug.LogError("There is no type available");    
                return;
            }
            
            if (types.Count == 1) {
                AcceptType(types[0]);
            } else {
                typeList = new TypeList(types);
                
                typeList.onDoubleClick += AcceptType;
                
                mode = item == null ? Mode.TypeSelection : Mode.ItemEditing;
            }
            
            dashboard.ShowPopup(this);
        }

        void AcceptType(Type type) {
            if (item != null && item.GetType() == type) {
                mode = Mode.ItemEditing; 
                return;
            }

            try {
                string raw = null;
                if (item is ISerializable s1) 
                    raw = Serializer.Instance.Serialize(s1);

                item = Activator.CreateInstance(type);
                        
                if (!raw.IsNullOrEmpty() && item is ISerializable s2)
                    Serializer.Instance.Deserialize(s2, raw);
                
                Repaint();
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                mode = Mode.ItemEditing; 
            }
        }

        public override void OnGUI() {
            switch (mode) {
                case Mode.TypeSelection: {
                    typeList.current = item?.GetType();
                    OnTypeSelectionGUI(); return;
                }
                case Mode.ItemEditing: 
                    OnItemEditionGUI();
                    return;
            }
        }
        
        static readonly Color suitableColor = new Color(0.48f, 1f, 0.52f); 
        static readonly Color nonSuitableColor = new Color(1f, 0.36f, 0.37f); 

        void OnItemEditionGUI() {
            
            using (GUIHelper.Toolbar.Start()) {
                GUILayout.FlexibleSpace();
                
                var suitable = item != null && (filter?.Invoke(item) ?? true);
                
                using (GUIHelper.Lock.Start(!suitable))
                using (GUIHelper.Color.Start(suitable ? suitableColor : nonSuitableColor))
                    if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(80))) {
                        addItem?.Invoke(item);
                        Close();
                    }
            }
            
            using (scroll.Start()) {
                if (!error.IsNullOrEmpty())
                    EditorGUILayout.HelpBox(error, MessageType.Error, true);
                
                if (item is ISerializable s)
                    ObjectEditor.CopyPaste("Data", s);
                
                #region Type button

                {
                    var toChangeType = false;
                    
                    if (item == null)
                        toChangeType = true;
                    
                    if (types.Count > 1)
                        toChangeType = GUIHelper.Button("Type", item.GetType().Name, EditorStyles.popup);
                    else
                        EditorGUILayout.LabelField("Type", item.GetType().Name);
                    
                    if (toChangeType) {
                        mode = Mode.TypeSelection;
                        return;
                    }
                }

                #endregion
                
                if (item is ISerializableID sid)
                    sid.ID = EditorGUILayout.TextField("ID", sid.ID);
                
                ObjectEditor.Edit(item);
            }
        }
        
        void OnTypeSelectionGUI() {
            using (GUIHelper.Toolbar.Start()) {
                if (item != null && GUILayout.Button("Back", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    mode = Mode.ItemEditing;
                
                searchPanel.OnGUI(typeList.SetSearchFilter);
                
                if (typeList.selected != null) {
                    string label = null;
                    
                    if (item == null)
                        label = "Create New";
                    else if (item.GetType() != typeList.selected)
                        label = "Change Type";

                    if (label != null && GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(80)))
                        AcceptType(typeList.selected);
                }
            }
            
            typeList.OnGUI();
        }
        
        class TypeList : HierarchyList<Type> {
            public Type selected;
            public Type current;
            
            public TypeList(List<Type> collection) : base(collection, new List<TreeFolder>(), new TreeViewState()) {
                onSelectedItemChanged += OnSelect;
                ExpandAll();
            }

            void OnSelect(List<Type> list) {
                if (!list.IsEmpty())
                    selected = list[0];
            }

            public override int GetUniqueID(Type element) {
                return element.GetHashCode();
            }


            public override string GetName(Type element) {
                return element.Name;
            }

            public override Type CreateItem() {
                return null;
            }

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) { }

            protected override bool CanRename(TreeViewItem item) {
                return false;
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                if (selected != null && info.content == selected)
                    Handles.DrawSolidRectangleWithOutline(rect, Color.cyan.Transparent(.1f), Color.cyan);
                
                rect = ItemIconDrawer.DrawAuto(rect, info.content);
                if (current == info.content)
                    rect = ItemIconDrawer.DrawTag(rect, "Current");

                base.DrawItem(rect, info);
            }

            public override string GetPath(Type element) {
                return element.Namespace.Replace('.', '/');
            }
            
            public override void SetPath(Type element, string path) { }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }
        }
    }

    public class StorageSelector<S> where S : class, ISerializable {
        Storage<S> storage;
        S selected = null;

        Func<S, string> getName;
        Func<S, bool> filter;
        
        public int Count => filter == null ? storage.items.Count : storage.items.Count(filter);
        
        public StorageSelector(Storage<S> storage, Func<S, string> getName, Func<S, bool> filter = null) {
            this.storage = storage;
            this.getName = getName;
            SetFilter(filter);
        }

        public void SetFilter(Func<S, bool> filter = null) {
            this.filter = filter;
            Select();
        }
        
        public void Draw(string label, Action<S> onChange, params GUILayoutOption[] options) {
            GUIHelper.Popup(label, selected, storage.items.OrderBy(getName), EditorStyles.popup, getName, s => {
                selected = s;
                onChange?.Invoke(s);
            }, filter, options);
        }

        IEnumerable<S> Values() {
            if (filter == null)
                return storage.items;
            else
                return storage.items.Where(filter);
        }

        public S Select(Func<S, bool> extraFilter = null) {
            if (extraFilter != null)
                selected = Values().FirstOrDefault(extraFilter.Invoke);
            
            if (selected == null && (filter == null || filter.Invoke(selected)))
                return selected;
            
            // if (selected == null)
            //     selected = Values().IDefaultOrFirst();

            return selected;
        }
    }
}