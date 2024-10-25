using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;
using Yurowm.Serialization;

namespace Yurowm.InUnityReporting {
    public class SerializableReportEditor : ReportEditor<SerializableReport> {
        int indexer;
        Tree tree = null;

        public float TreeHeight => tree?.totalHeight ?? 0;

        public override void SetProvider(SerializableReport report) {
            if (report == null) {
                tree = null;
                return;
            }
            
            indexer = 1;
            
            Unit root = Convert(report.GetEntry());
            List<Unit> units = new List<Unit>();
            List<Container> containers = new List<Container>();

            foreach (Unit unit in Unpack(root)) {
                if (unit is Container container)
                    containers.Add(container);
                else 
                    units.Add(unit);
            }
            
            tree = new Tree(units, containers, new TreeViewState());
            tree.folders.Take(3).ForEach(f => tree.SetExpanded(f.Value.ID, true));
        }
        
        Unit Convert(Reader.Entry entry) {
            Unit result = null;
            if (entry.childs.Count > 0)
                result = new Container();
            else
                result = new Unit();
                
            result.ID = indexer++;
            result.Title = entry.key;
            
            
            if (result is Container container) {
                result.Value = "{...}";
                container.Childs = new List<Unit>(entry.childs.Count);
                for (int i = 0; i < entry.childs.Count; i++) {
                    var e = entry.childs[i];
                    if (e.childs.Count == 0 && e.key == "type") {
                        result.Value = $"({e.value})";
                        continue;
                    }
                    var child = Convert(e);
                    child.Parent = container;
                    container.Childs.Add(child);   
                }
            } else 
                result.Value = entry.value;

            return result;
        }

        IEnumerable<Unit> Unpack(Unit unit) {
            yield return unit;
            if (unit is Container container) {
                foreach (Unit child in container.Childs)
                  using (var unpack = Unpack(child).GetEnumerator())
                      while (unpack.MoveNext())
                          yield return unpack.Current;
            }
        }
        
        GUIHelper.SearchPanel searchPanel = new GUIHelper.SearchPanel(""); 
        public override bool OnGUI(params GUILayoutOption[] layoutOptions) {
            if (tree == null)
                return false;
            
            using (GUIHelper.Vertical.Start()) {
                searchPanel.OnGUI(tree.SetSearchFilter);
                tree.OnGUI(layoutOptions);
            }
            
            return true;
        }
        
        class Unit {
            public int ID;
            public Container Parent;
            public string Path {
                get {
                    if (Parent != null) {
                        var path = Parent.Path;
                        if (!path.IsNullOrEmpty())
                            path += "/";
                        path += Parent.ID;
                        return path;
                    }
                    return "";
                }
            }

            public string Title;
            public string Value;
        }
        
        class Container : Unit {
            public List<Unit> Childs;
        }
        
        class Tree : HierarchyList<Unit, Container> {
            public Tree(List<Unit> collection, List<Container> folders, TreeViewState state) 
                : base(collection, folders, state) {}

            public override void DrawItem(Rect rect, ItemInfo info) {
                if (searchFilter.IsNullOrEmpty())
                    GUI.Label(rect, GetLabel(info), labelStyle);
                else
                    GUI.Label(rect, GUIHelper.SearchPanel.Format(GetLabel(info), searchFilter), labelStyle);
            }
            
            public override void DrawFolder(Rect rect, FolderInfo info) {
                if (searchFilter.IsNullOrEmpty())
                    GUI.Label(rect, GetLabel(info), labelStyle);
                else
                    GUI.Label(rect, GUIHelper.SearchPanel.Format(GetLabel(info), searchFilter), labelStyle);
            }

            public override string GetLabel(ItemInfo info) {
                var title = info.content.Title ?? "";
                if (title.Length > 0)
                    title += ": ";
                title += info.content.Value;
                return title;
            }

            public override string GetLabel(FolderInfo info) {
                var title = info.content.Title ?? "";
                if (title.Length > 0)
                    title += " ";
                title += info.content.Value;
                return title;
            }

            public override string GetPath(Unit element) {
                return element.Path;
            }

            public override string GetPath(Container element) {
                return element.Path;
            }

            public override string GetName(Unit element) {
                return element.ID.ToString();
            }
            
            public override string GetName(Container element) {
                return element.ID.ToString();
            }
            
            public override void SetPath(Unit element, string path) {}

            public override void SetPath(Container element, string path) {}

            public override int GetUniqueID(Unit element) {
                return element.ID;
            }

            public override int GetUniqueID(Container element) {
                return element.ID;
            }

            public override Container CreateFolder() {
                return null;
            }

            public override Unit CreateItem() {
                return null;
            }

            protected override bool CanRename(TreeViewItem item) {
                return false;
            }

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }
        }
    }
}