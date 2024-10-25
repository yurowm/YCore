using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Icons;
using Object = UnityEngine.Object;

namespace Yurowm.HierarchyLists {
    public abstract class HierarchyList<I, F> : TreeView {
        protected FolderInfo root;

        public List<I> itemCollection = null;
        public List<F> folderCollection = null;
        public bool dragToAnotherList = false;
        
        bool hasChildren = false;

        public Dictionary<int, IInfo> info = new();
        public Dictionary<string, F> folders = new();

        public Action<List<I>> onSelectedItemChanged = delegate { };
        public Action<List<IInfo>> onSelectionChanged = delegate { };
        public Action<I> onDoubleClick = delegate { };
        public Action onRebuild = delegate { };
        public Action onChanged = delegate { };
        public Action<IEnumerable<IInfo>> onRemove = delegate { };
        
        protected string listName = null;
        public HierarchyList(List<I> collection, List<F> folders, TreeViewState state, string name = null) : base(state) {
            listName = name;

            useScrollView = true;
            
            itemCollection = collection;
            folderCollection = folders ?? new List<F>();
            
            Reload();
        }
        
        GUIStyle _labelStyle = null;
        protected GUIStyle labelStyle {
            get {
                if (_labelStyle == null)
                    _labelStyle = GetLabelStyle();
                return _labelStyle;
            }
        }

        protected virtual GUIStyle GetLabelStyle() {
            return new GUIStyle(GUIHelper.SearchPanel.keyItemStyle);
        }
        
        protected override TreeViewItem BuildRoot() {
            info.Clear();
            root = new FolderInfo(-1, null);

            folders = folderCollection.GroupBy(GetFullPath).ToDictionary(x => x.Key, x => x.First());

            foreach (I element in itemCollection) {
                FolderInfo folder = AddFolder(GetPath(element));
                ItemInfo i = new ItemInfo(folder.item.depth + 1, folder);
                i.content = element;
                i.item.displayName = GetName(element);
                folder.items.Add(i);
            }

            folderCollection.Clear();
            folderCollection.AddRange(folders.Values);
            foreach (F folder in folderCollection.ToList())
                AddFolder(GetFullPath(folder));

            if (!_searchFilter.IsNullOrEmpty() || _itemFilter != null) {
                var filter = _searchFilter.ToLower().Trim();
                var filteredRoot = new FolderInfo(-1);
                foreach (var item in root.GetAllChild()) {
                    if (_itemFilter != null && (!(item is ItemInfo i) || !_itemFilter(i)))
                        continue;

                    if (filter.IsNullOrEmpty() || ItemSearchFilter(item, filter)) {
                        item.item.parent = filteredRoot.item;
                        item.item.depth = 0;
                        filteredRoot.items.Add(item);
                        if (item is FolderInfo folderInfo) 
                            folderInfo.items.Clear();
                    }
                }
                root = filteredRoot;
            }

            root.GetAllChild().ForEach(x => x.item.id = GetUniqueID(x));

            info.Clear();
            foreach (IInfo iinfo in root.GetAllChild()) {
                if (info.ContainsKey(iinfo.item.id)) {
                    Debug.LogError($"These two elements has the same ID ({iinfo.item.id})\n{GetFullPath(iinfo)}\n{GetFullPath(info[iinfo.item.id])}");
                    continue;
                }
                info.Add(iinfo.item.id, iinfo);
            }
            SetupParentsAndChildrenFromDepths(root.item, root.GetAllChild().Select(x => x.item).ToList());

            onRebuild();
            
            SelectionChanged(state.selectedIDs);
            
            hasChildren = root.GetAllChild().Any(c => c.item.depth > 0);
            
            return root.item;
        }

        string _searchFilter = "";
        Func<ItemInfo, bool> _itemFilter = null;
        
        public string searchFilter => _searchFilter;

        public void SetSearchFilter(string filter) {
            if (_searchFilter == filter) return;
            
            _searchFilter = filter;
            
            OnFilterUpdate();
        }
        
        protected virtual bool ItemSearchFilter(IInfo item, string filter) {
            return Search(
                filter.Contains('/') ? GetFullPath(item) : GetLabel(item),
                filter);
        }
        
        protected bool Search(string text, string piece) {
            if (text.IsNullOrEmpty() || piece.IsNullOrEmpty())
                return false;
            
            return text.IndexOf(piece, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void SetFilter(Func<ItemInfo, bool> filter) {
            _itemFilter = filter;
            
            OnFilterUpdate();
        }
        
        void OnFilterUpdate() {
            var selected = state.selectedIDs.ToList();
                
            Reload();
            
            SetSelection(selected);
            selected.ForEach(s => {
                var item = FindItem(s, rootItem);
                while (item != null) {
                    SetExpanded(item.id, true);
                    item = item.parent;
                }
            });
        }

        public IInfo GetInfo(I item) {
            return info.Values.FirstOrDefault(x => x.isItemKind && x.asItemKind.content.Equals(item));
        }
        
        public IInfo GetInfo(int index) {
            return info.Get(index);
        }

        protected override void SelectionChanged(IList<int> selectedIds) {
            if (onSelectedItemChanged.GetInvocationList().Length == 0)
                return;
            List<I> result = new List<I>();
            foreach (int id in selectedIds) {
                if (info.ContainsKey(id)) {
                    IInfo item = info[id];
                    if (item != null && item.isItemKind)
                        result.Add(item.asItemKind.content);
                }
            }
            onSelectedItemChanged(result);
                onSelectionChanged(selectedIds
                .Where(x => info.ContainsKey(x))
                .Select(x => info[x]).ToList());
        }

        public virtual bool ObjectToItem(Object o, out IInfo result) {
            result = null;
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args) {
            return true;
        }

        protected override void RowGUI(RowGUIArgs args) {
            if (!info.ContainsKey(args.item.id)) return;
            
            var i = info[args.item.id];
            var rect = new Rect(args.rowRect);
            
            if (hasChildren) {
                var offset = GetContentIndent(args.item);
                rect.x += offset;
                rect.width -= offset;
            }
            
            if (i.isItemKind) 
                DrawItem(rect, i.asItemKind);
            else 
                DrawFolder(rect, i.asFolderKind);
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            return info[item.id].isItemKind ? ItemRowHeight() : FolderRowHeight();
        }

        protected override bool CanRename(TreeViewItem item) {
            if (!info.ContainsKey(item.id)) return false;
            IInfo iinfo = info[item.id];
            return iinfo.isItemKind ? CanRename(iinfo.asItemKind) : CanRename(iinfo.asFolderKind);
        }
        public virtual bool CanRename(ItemInfo info) {
            return true;
        }
        public virtual bool CanRename(FolderInfo info) {
            return true;
        }
        protected override void RenameEnded(RenameEndedArgs args) {
            if (args.originalName == args.newName || args.newName.Contains('/')) return;
            IInfo iinfo = info[args.itemID];
            if (iinfo.parent.items.Any(x => GetName(x) == args.newName)) return;

            SetName(iinfo, args.newName);
            iinfo.item.displayName = args.newName;
            UpdatePath(new List<IInfo>() { iinfo });
            Reload();
            onChanged();
        }

        public virtual float FolderRowHeight() {
            return 16f;
        }
        
        public virtual float ItemRowHeight() {
            return 16f;
        }

        public virtual bool CanBeChild(IInfo parent, IInfo child) {
            if (!parent.isFolderKind)
                return false;
            
            if (child.parent == parent)
                return true;
            
            return parent.asFolderKind.items.All(x => GetUniqueID(x) != GetUniqueID(child));
        }

        public virtual void DrawItem(Rect rect, ItemInfo info) {
            if (searchFilter.IsNullOrEmpty()) 
                GUI.Label(rect, GetLabel(info), labelStyle);
            else
                GUI.Label(rect, GUIHelper.SearchPanel.Format(GetLabel(info), searchFilter), labelStyle);
        }
        
        public virtual void DrawFolder(Rect rect, FolderInfo info) {
            Texture2D icon; 
            
            if (!info.item.hasChildren)
                icon = folderEmptyIcon;
            else if (state.expandedIDs.Contains(info.item.id))
                icon = folderOpenedIcon;
            else
                icon = folderIcon;
            
            rect = ItemIconDrawer.Draw(rect, icon);
            
            if (searchFilter.IsNullOrEmpty()) 
                GUI.Label(rect, GetLabel(info), labelStyle);
            else
                GUI.Label(rect, GUIHelper.SearchPanel.Format(GetLabel(info), searchFilter), labelStyle);
        }
        
        public string GetLabel(IInfo info) {
            return info.isItemKind ? GetLabel(info.asItemKind) : GetLabel(info.asFolderKind);
        }
        
        public virtual string GetLabel(ItemInfo info) {
            return searchFilter.IsNullOrEmpty() ? GetName(info) : GetFullPath(info);
        }

        public virtual string GetLabel(FolderInfo info) {
            return searchFilter.IsNullOrEmpty() ? GetName(info) : GetFullPath(info);
        }

        public string GetPath(IInfo info) {
            return info.isItemKind ? GetPath(info.asItemKind.content) : GetPath(info.asFolderKind.content);
        }
        public abstract string GetPath(I element);
        public abstract string GetPath(F element);
        string GetFullPath(IInfo info) {
            return info.isItemKind ? GetFullPath(info.asItemKind.content) : GetFullPath(info.asFolderKind.content);
        }
        string GetFullPath(I element) {
            string path = GetPath(element);
            string name = GetName(element);
            if (path.Length > 0) return path + '/' + name;
            else return name;
        }
        string GetFullPath(F element) {
            string path = GetPath(element);
            string name = GetName(element);
            if (path.Length > 0) return path + '/' + name;
            else return name;
        }

        public void SetPath(IInfo info, string path) {
            if (info.isItemKind) SetPath(info.asItemKind.content, path);
            else SetPath(info.asFolderKind.content, path);
        }
        public abstract void SetPath(I element, string path);
        public abstract void SetPath(F element, string path);
        
        public void SetName(IInfo info, string name) {
            if (info.isItemKind) SetName(info.asItemKind.content, name);
            else SetName(info.asFolderKind.content, name);
        }
        
        public virtual void SetName(I element, string name) { }
        
        public virtual void SetName(F element, string name) { }

        public string GetName(IInfo info) {
            return info.isItemKind ? GetName(info.asItemKind.content) : GetName(info.asFolderKind.content);
        }
        
        public virtual string GetName(I element) {
            return "Item";
        }
        
        public virtual string GetName(F element) {
            string path = GetPath(element);
            int sep = path.IndexOf('/');
            if (sep >= 0) return path.Substring(sep + 1, path.Length - sep - 1);
            return path;
        }

        int GetUniqueID(IInfo element) {
            return element.isItemKind ? GetUniqueID(element.asItemKind.content) : GetUniqueID(element.asFolderKind.content);
        }
        
        public abstract int GetUniqueID(I element);
        
        public abstract int GetUniqueID(F element);

        protected FolderInfo AddFolder(string fullPath) {
            FolderInfo currentFolder = root;
            if (!string.IsNullOrEmpty(fullPath)) {
                foreach (string name in fullPath.Split('/')) {
                    if (name.IsNullOrEmpty()) continue;
                    FolderInfo f = (FolderInfo) currentFolder.items
                        .Find(x => x.isFolderKind && x.asFolderKind.content != null && GetName(x.asFolderKind.content) == name);
                    if (f == null) {
                        f = new FolderInfo(currentFolder.item.depth + 1, currentFolder);
                        currentFolder.items.Add(f);
                        f.parent = currentFolder;
                        f.item.displayName = name;

                        string path = f.fullPath;
                        if (!folders.ContainsKey(path)) {
                            F folder = CreateFolder();
                            SetPath(folder, currentFolder.fullPath);
                            SetName(folder, name);
                            folderCollection.Add(folder);
                            folders.Add(path, folder);
                        }
                        f.content = folders[path];
                    }
                    currentFolder = f;
                }
            }
            return currentFolder;
        }
        
        public void AddNewFolder(FolderInfo folder, string nameFormat) {
            if (nameFormat == null || !nameFormat.Contains("{0}"))
                nameFormat = "Untitled{0}";

            if (folder == null) folder = root;

            string name = string.Format(nameFormat, "");
            for (int i = 1; true; i++) {
                if (folder.items.All(x => GetName(x) != name))
                    break;
                name = string.Format(nameFormat, i);
            }

            string path = folder.fullPath;
            F newFolder = CreateFolder();
            SetName(newFolder, name);
            SetPath(newFolder, path);
            int id = GetUniqueID(newFolder);

            folderCollection.Add(newFolder);
            Reload();
            onChanged();

            var treeItem = FindItem(id, root.item);
            if (CanRename(treeItem))
                BeginRename(treeItem);
        }
        
        public void Group(List<IInfo> items, FolderInfo parent, string nameFormat) {
            if (!nameFormat.Contains("{0}"))
                nameFormat = "Untitled{0}";

            if (parent == null) parent = root;

            string name = string.Format(nameFormat, "");
            for (int i = 1; true; i++) {
                if (parent.items.All(x => GetName(x) != name))
                    break;
                name = string.Format(nameFormat, i);
            }

            FolderInfo group = AddFolder(parent.fullPath + "/" + name);
            PutInFolder(group, parent, items.Min(x => x.index));
            int id = GetUniqueID(group);

            foreach (IInfo item in items)
                PutInFolder(item, group);

            UpdatePath(items);
            Reload();
            onChanged();

            var treeItem = FindItem(id, root.item);
            if (CanRename(treeItem))
                BeginRename(treeItem);
        }

        public abstract F CreateFolder();

        public void Remove(params IInfo[] items) {
            Remove(false, items);
        }

        public void Remove(bool silent, params IInfo[] items) {
            if (items == null || items.Length == 0)
                return;
            if (!silent && !EditorUtility.DisplayDialog("Remove", "Are you sure want to remove these items", "Remove", "Cancel"))
                return;

            var toRemove = new HashSet<IInfo>();
            foreach (IInfo iinfo in items) {
                toRemove.Add(iinfo);
                if (iinfo.isFolderKind)
                    toRemove.UnionWith(iinfo.asFolderKind.GetAllChild());
            }
            
            onRemove(toRemove);
            foreach (IInfo iinfo in toRemove) {
                if (iinfo.isItemKind) itemCollection.Remove(iinfo.asItemKind.content);
                if (iinfo.isFolderKind) folderCollection.Remove(iinfo.asFolderKind.content);
            }

            MarkAsChanged();
        }

        bool reloadRequired = false;
        public void MarkAsChanged() {
            reloadRequired = true;
        }
        
        public void OnGUI(Rect rect, GUIStyle style) {
            if (style != null) {
                GUI.Box(rect, string.Empty, style);
                rect.x += style.padding.left;
                rect.width -= style.padding.horizontal;
                rect.y += style.padding.bottom;
                rect.height -= style.padding.vertical;
            }
            OnGUI(rect);
        }
        
        protected static Texture2D folderIcon;
        protected static Texture2D folderOpenedIcon;
        protected static Texture2D folderEmptyIcon;
        public override void OnGUI(Rect rect) {
            if (folderIcon == null)
                folderIcon = EditorIcons.GetUnityIcon("Folder Icon", "d_Folder Icon");
            if (folderOpenedIcon == null) 
                folderOpenedIcon = EditorIcons.GetUnityIcon("FolderOpened Icon", "d_FolderOpened Icon");
            if (folderEmptyIcon == null)
                folderEmptyIcon = EditorIcons.GetUnityIcon("FolderEmpty Icon", "d_FolderEmpty Icon");
            base.OnGUI(rect);
            if (reloadRequired) {
                Reload();
                onChanged();
                reloadRequired = false;
            }
        }

        public Action<Rect> drawOverlay;
        public GUIStyle style;

        public void OnGUI(params GUILayoutOption[] options) {
            var rect = EditorGUILayout.GetControlRect(options);
            OnGUI(rect, style);
            if (Event.current.type == EventType.Repaint) 
                drawOverlay?.Invoke(rect);
        }

        public void OnGUI(float maxHeight) {
            OnGUI(GUILayout.ExpandWidth(true), GUILayout.MaxHeight(maxHeight));
        }
        
        public void OnGUI() {
            OnGUI(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        public void AddNewItem(FolderInfo folder, string nameFormat) {
            if (nameFormat == null || !nameFormat.Contains("{0}"))
                nameFormat = "Untitled{0}";

            if (folder == null) folder = root;

            string name = string.Format(nameFormat, "");
            for (int i = 1; true; i++) {
                if (folder.items.All(x => GetName(x) != name))
                    break;
                name = string.Format(nameFormat, i);
            }

            string path = folder.fullPath;
            I newItem = CreateItem();
            if (newItem == null) return;
            SetName(newItem, name);
            SetPath(newItem, path);
            int id = GetUniqueID(newItem);

            itemCollection.Add(newItem);
            Reload();
            onChanged();

            var treeItem = FindItem(id, root.item);
            if (CanRename(treeItem))
                BeginRename(treeItem);
        }
        public abstract I CreateItem();

        List<IInfo> drag = new List<IInfo>();
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args) {
            DragAndDrop.PrepareStartDrag();

            drag.Clear();
            foreach (var id in args.draggedItemIDs)
                drag.Add(root.Find(id));

            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = Array.Empty<Object>();
            if (dragToAnotherList) {
                DragAndDrop.SetGenericData(typeof(I).FullName, drag.Where(x => x.isItemKind).ToArray());
                DragAndDrop.SetGenericData(GetType().FullName, this);
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag("HierarchyList");
            _isDrag = true;
        }

        bool _isDrag = false;
        public bool isDrag => _isDrag;

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args) {
            if (!_isDrag) {
                drag.Clear();
                _isDrag = true;
                foreach (var o in DragAndDrop.objectReferences) {
                    IInfo item;
                    if (ObjectToItem(o, out item) && item != null)
                        drag.Add(item);
                }

                if (DragAndDrop.GetGenericData(typeof(I).FullName) is IInfo[] selected) 
                    drag.AddRange(selected);
            }

            var visualMode = DragAndDropVisualMode.None;

            if (args.performDrop || drag.Count == 0)
                _isDrag = false;

            if (drag.Count == 0)
                return DragAndDropVisualMode.Rejected;

            if (args.parentItem == null)
                args.parentItem = root.item;

            IInfo parent = root.item.id == args.parentItem.id ? root : root.Find(args.parentItem.id);

            if (parent != null && !drag.All(x => CanBeChild(parent, x)))
                return DragAndDropVisualMode.Rejected;

            switch (args.dragAndDropPosition) {
                case DragAndDropPosition.UponItem:
                    if (parent.isItemKind || drag.Any(x => x.isFolderKind && x.asFolderKind.IsParentOf(parent.asFolderKind)))
                        return DragAndDropVisualMode.Rejected;

                    if (args.performDrop) {
                        if (parent != null && parent.isFolderKind && drag.All(x => CanBeChild(parent, x))) {
                            int index = parent.asFolderKind.items.Count;

                            AcceptDrag();
                            foreach (IInfo d in drag)
                                PutInFolder(d, parent.asFolderKind, index++);
                            UpdatePath(drag);
                            BakeCollections();
                            onChanged();
                            Reload();
                        }
                    }
                    return DragAndDropVisualMode.Move;
                case DragAndDropPosition.OutsideItems:
                case DragAndDropPosition.BetweenItems:
                    if (parent.isFolderKind) {
                        if (args.performDrop) {
                            if (parent != null && parent.isFolderKind && drag.All(x => CanBeChild(parent, x))) {
                                int index = 0;
                                AcceptDrag();
                                foreach (IInfo d in drag)
                                    PutInFolder(d, parent.asFolderKind, args.insertAtIndex + (index++));
                                UpdatePath(drag);
                                BakeCollections();
                                onChanged();
                                Reload();
                            }
                        }
                    }
                    return DragAndDropVisualMode.Move;
            }
            return visualMode;
        }

        void AcceptDrag() {
            if (!dragToAnotherList) return;
            if (DragAndDrop.GetGenericData(GetType().FullName) is HierarchyList<I, F> source && source != this)
                source.Remove(true, drag.ToArray());
        }

        protected bool PutInFolder(IInfo item, FolderInfo folder) {
            if (item != folder) {
                item.parent?.items.Remove(item);
                item.parent = folder;
                folder.items.Add(item);
                return true;
            }
            return false;
        }
        
        protected bool PutInFolder(IInfo item, FolderInfo folder, int index) {
            if (item != folder) {
                if (item.parent == folder && folder.items.IndexOf(item) < index)
                    index--;

                item.parent?.items.Remove(item);
                item.parent = folder;

                folder.items.Insert(Mathf.Clamp(index, 0, folder.items.Count), item);
                return true;
            }

            return false;
        }

        protected override void DoubleClickedItem(int id) {
            var info = GetInfo(id);
            if (info != null && info.isItemKind)
                onDoubleClick?.Invoke(info.asItemKind.content);
        }

        public FolderInfo FindFolder(string path) {
            if (string.IsNullOrEmpty(path))
                return null;

            FolderInfo result = root;
            foreach (string folder in path.Split('/')) {
                if (string.IsNullOrEmpty(folder))
                    return null;
                result = (FolderInfo) result.items.Find(x => x is FolderInfo f && f.item.displayName == folder);
                if (result == null)
                    return null;
            }

            return result;
        }

        void BakeCollections() {
            itemCollection.Clear();
            folderCollection.Clear();
            foreach (IInfo i in root.GetAllChild()) {
                if (i.isItemKind) itemCollection.Add((i.asItemKind).content);
                else folderCollection.Add((i.asFolderKind).content);
            }
        }

        protected void UpdatePath(List<IInfo> items) {
            List<IInfo> toUpdatePath = new List<IInfo>();
            foreach (IInfo item in items) {
                toUpdatePath.Add(item);
                if (item.isFolderKind) toUpdatePath.AddRange(item.asFolderKind.GetAllChild());
            }
            foreach (var x in toUpdatePath.Distinct())
                if (x.parent != null)
                    SetPath(x, x.parent.fullPath);
        }

        bool isContextOnItem = false;
        protected override void ContextClicked() {
            if (isContextOnItem) {
                isContextOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();

            ContextMenu(menu, new List<IInfo>());
            menu.ShowAsContext();
        }

        protected override void ContextClickedItem(int id) {
            isContextOnItem = true;

            GenericMenu menu = new GenericMenu();
            List<IInfo> selection = GetSelection()
                .Where(x => info.ContainsKey(x))
                .Select(x => info[x])
                .Where(x => x != null).ToList();
            ContextMenu(menu, selection);

            if (menu.GetItemCount() > 0)
                menu.ShowAsContext();
        }

        public abstract void ContextMenu(GenericMenu menu, List<IInfo> selected);

        public class ItemInfo : IInfo {
            public I content;

            public ItemInfo(int depth, FolderInfo parent = null) {
                item = new TreeViewItem(0, depth, "Item");
                this.parent = parent;
            }
        }

        public class FolderInfo : IInfo {
            public F content;

            public FolderInfo(int depth, FolderInfo parent = null) {
                item = new TreeViewItem(0, depth, "Folder");
                this.parent = parent;
            }

            public List<IInfo> items = new List<IInfo>();

            public List<IInfo> GetAllChild() {
                List<IInfo> result = new List<IInfo>();
                foreach (IInfo item in items) {
                    result.Add(item);
                    if (item is FolderInfo folder)
                        result.AddRange(folder.GetAllChild());
                }
                return result;
            }

            public IInfo Find(int id) {
                foreach (IInfo item in items) {
                    if (item.item.id == id)
                        return item;
                    if (item is FolderInfo) {
                        IInfo i = (item as FolderInfo).Find(id);
                        if (i != null)
                            return i;
                    }
                }
                return null;
            }

            public bool IsChildOf(FolderInfo folder) {
                FolderInfo current = parent;
                while (current != null) {
                    if (current == folder)
                        return true;
                    current = current.parent;
                }
                return false;
            }

            public bool IsParentOf(FolderInfo folder) {
                return folder.IsChildOf(this);
            }
        }

        public abstract class IInfo {
            public TreeViewItem item;
            public FolderInfo parent;

            public string path => parent != null ? parent.fullPath : "";

            public string fullPath {
                get {
                    if (parent == null) return "";
                    string path = this.path;
                    if (path.Length > 0)
                        return path + '/' + name;
                    return name;
                }
            }
            public string name => item.displayName;

            public int index {
                get {
                    if (parent != null)
                        return parent.items.IndexOf(this);
                    return -1;
                }
            }

            public bool isItemKind => this is ItemInfo;

            public bool isFolderKind => this is FolderInfo;

            public ItemInfo asItemKind => this as ItemInfo;

            public FolderInfo asFolderKind => this as FolderInfo;

            public override string ToString() {
                return (isItemKind ? "I:" : "F:") + fullPath;
            }
        }
    }

    public abstract class HierarchyList<I> : HierarchyList<I, TreeFolder> {

        public HierarchyList(List<I> collection, List<TreeFolder> folders, TreeViewState state) : base(collection, folders, state) { }

        public override TreeFolder CreateFolder() {
            return new TreeFolder();
        }

        public override string GetPath(TreeFolder element) {
            return element.path;
        }

        public override int GetUniqueID(TreeFolder element) {
            return element.GetHashCode();
        }

        public override void SetPath(TreeFolder element, string path) {
            element.path = path;
        }

        public override string GetName(TreeFolder element) {
            return element.name;
        }

        public override void SetName(TreeFolder element, string name) {
            element.name = name;
        }
    }

    public abstract class NonHierarchyList<I> : HierarchyList<I, TreeFolder> {
        public NonHierarchyList(List<I> collection, string name = null) :
            base(collection, null, new TreeViewState(), name) { }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            menu.AddItem(new GUIContent("New Item"), false, () => AddNewItem(headFolder, null));
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        public FolderInfo rootFolder => AddFolder("");

        public FolderInfo headFolder => listName.IsNullOrEmpty() ? rootFolder : AddFolder("root");

        public override TreeFolder CreateFolder() {
            return new TreeFolder();
        }

        public override void DrawFolder(Rect rect, FolderInfo info) {
            rect = ItemIconDrawer.Draw(rect, folderIcon);
            GUI.Label(rect, listName);
        }

        protected override void RowGUI(RowGUIArgs args) {
            if (listName.IsNullOrEmpty()) {
                if (!info.ContainsKey(args.item.id)) return;
                IInfo i = info[args.item.id];
                if (i.isItemKind) DrawItem(args.rowRect, i.asItemKind);
            } else
                base.RowGUI(args);
        }

        public override string GetPath(TreeFolder element) {
            return "";
        }

        public override string GetPath(I element) {
            return string.IsNullOrEmpty(listName) ? "" : "root";
        }

        public override string GetName(TreeFolder element) {
            return string.IsNullOrEmpty(listName) ? "" : "root";
        }

        public override string GetName(I element) {
            return element.GetHashCode().ToString();
        }

        public override int GetUniqueID(TreeFolder element) {
            return element.path.GetHashCode();
        }

        public override void SetPath(TreeFolder element, string path) {}

        public override void SetPath(I element, string path) {}
    }
}