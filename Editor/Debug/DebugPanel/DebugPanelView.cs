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

namespace Yurowm.DebugTools {
    [DashboardGroup("Debug")]
    [DashboardTab("Debug Panel", null)]
    public class DebugPanelView : DashboardEditor {
        
        List<DebugPanel.Entry> collection = new();
        LogList logList;
        
        public override bool Initialize() {
            DebugPanel.AddListener(this);
            
            DebugPanel.onLog += OnLog;
            
            return true;
        }
        
        #region Repaint

        bool dirty = true;
        
        void OnLog() {
            dirty = true;
        }
        
        void OnUpdate() {
            if (dirty) {
                dirty = false;
                
                collection.Reuse(DebugPanel.GetEntries());
                
                if (logList == null) {
                    logList = new LogList(collection);
                    logList.drawItem = DrawEntry;
                    logList.onDoubleClick = OnDoubleClick;
                    logList.onSelectedItemChanged += OnSelectedItemChanged;
                }
                else
                    logList.Reload();
            }
        }

        #endregion

        public override void OnGUI() {
            using (GUIHelper.Vertical.Start(GUILayout.ExpandHeight(true)))
                logList?.OnGUI();
            
            DrawSelected();
            
            if (Event.current.type == EventType.Repaint) {
                OnUpdate();
                Repaint();
            }
        }

        public override void OnToolbarGUI() {
            base.OnToolbarGUI();
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50))) {
                DebugPanel.Clear();
                collection.Clear();
                logList.folders.Clear();
                logList.Reload();
            }
            
            if (GUILayout.Button("Show all", EditorStyles.toolbarButton, GUILayout.Width(60)))
                logList.ExpandAll();
            
            if (GUILayout.Button("Hide all", EditorStyles.toolbarButton, GUILayout.Width(60))) 
                logList.CollapseAll();
        }

        void DrawEntry(Rect rect, DebugPanel.Entry entry) {
            using (GUIHelper.Color.Start(DebugPanel.GroupToColor(entry.group))) {
                if (!entry.name.StartsWith("~")) {
                    var prefixRect = rect;
                    
                    if (EditorGUIUtility.labelWidth > 0)
                        prefixRect.width = EditorGUIUtility.labelWidth;
                    else
                        prefixRect.width = 200;
                    
                    GUI.Label(prefixRect, entry.name);
                        
                    rect.xMin = prefixRect.xMax;
                }
                
                DrawMessage(rect, entry.message);
            }
        }
        
        void OnDoubleClick(DebugPanel.Entry entry) {
            MessageDrawer.Get(entry.message.GetType())?.DoubleClick(entry.message);
        }
        
        void DrawMessage(Rect rect, DebugPanel.IMessage message) {
            if (message != null)
                MessageDrawer.Get(message.GetType())?.Draw(rect, message);
        }

        #region Selected

        DebugPanel.Entry selected = null;
        GUIHelper.Scroll scroll = new GUIHelper.Scroll();
        
        GUIHelper.VerticalSplit splitter = new GUIHelper.VerticalSplit();
        
        void OnSelectedItemChanged(List<DebugPanel.Entry> list) { 
            var first = list.FirstOrDefault();
            selected = first;
        }

        void DrawSelected() {
            var message = selected?.message;
            if (message == null)
                return;
            
            var drawer = MessageDrawer.Get(message.GetType());
            if (drawer == null || drawer.IsEmpty(message))
                return;
            
            using (splitter.Start())
            using (scroll.Start())
                drawer.DrawFull(message);
        }
        
        #endregion
        
        class LogList : HierarchyList<DebugPanel.Entry> {
            public Action<Rect, DebugPanel.Entry> drawItem;
            
            static TreeViewState treeViewState = new ();

            public LogList(List<DebugPanel.Entry> collection) : base(collection, new List<TreeFolder>(), treeViewState) { }

            public override string GetPath(DebugPanel.Entry element) {
                return element.group;
            }

            public override void SetPath(DebugPanel.Entry element, string path) { }

            public override int GetUniqueID(DebugPanel.Entry element) {
                return element.hash;
            }

            public override int GetUniqueID(TreeFolder element) {
                return element.fullPath.GetHashCode();
            }

            public override DebugPanel.Entry CreateItem() {
                return null;
            }

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                var entry = selected.FirstOrDefault()?.asItemKind?.content;
                
                if (entry == null) return;
                
                menu.AddItem(new GUIContent("Go to Declaration"), false, () =>
                    UnityEditorInternal.InternalEditorUtility
                        .OpenFileAtLineExternal(entry.logPoint.path, entry.logPoint.line, 0));
            }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }

            protected override bool CanRename(TreeViewItem item) {
                return false;
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                drawItem?.Invoke(rect, info.content);
            }

            public override void DrawFolder(Rect rect, FolderInfo info) {
                using (GUIHelper.ContentColor.Start(DebugPanel.GroupToColor(info.name))) 
                    base.DrawFolder(rect, info);
            }
        }
    }
}