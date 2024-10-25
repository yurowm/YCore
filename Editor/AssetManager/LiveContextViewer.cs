using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;
using Yurowm.Icons;
using Yurowm.InUnityReporting;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm.Spaces {
    public class LiveContextViewer : EditorWindow {
        
        LiveContext currentContext;
        
        SerializableReportEditor rawView = new SerializableReportEditor();
        
        YList<object> listDrawer = new YList<object>();
        List<object> list = new List<object>();
        
        GUIHelper.LayoutSplitter splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200);

        object selectedItem = null;
        

        enum ViewMode {
            Arguments,
            Items
        }       
        
        enum EditMode {
            None,
            Raw,
            ObjectEditor
        }
        
        ViewMode viewMode = ViewMode.Items;
        EditMode editMode = EditMode.None;
        
        [MenuItem("Yurowm/Tools/Live Context")]
        public static LiveContextViewer Create() {
            var window = GetWindow<LiveContextViewer>();
            window.Show();
            window.OnEnable();
            return window;
        }

        void OnEnable() {

            titleContent.text = "Live Context";
            
            listDrawer.allowToRemove = false;

            listDrawer.onElementContextMenu = (menu, item) => {
                if (item is ILiveContextHolder contextHolder)
                    menu.AddItem(new GUIContent("Show Context"), false, () => 
                        SetCurrentContext(contextHolder.GetContext()));
            };
            
            listDrawer.onChangeSelection += selected => {
                selectedItem = selected.FirstOrDefault();
                SerializableReport report = null;
                
                if (selectedItem != null && selectedItem is ISerializable serializable) {
                    report = new SerializableReport(serializable);
                    report.Refresh();
                    rawView.SetProvider(report);
                } 
                
                rawView.SetProvider(report);
                Repaint();
                
                var unityObjects = selected
                    .CastIfPossible<Object>()
                    .Select(uo => {
                        if (uo is Component component)
                            return component.gameObject;
                        return uo;
                    })
                    .ToArray();
                
                if (unityObjects.Length > 0) 
                    Selection.objects = unityObjects;
            };
            
            
            var unityIcon = EditorIcons.GetIcon("Unity");
            var serializableIcon = EditorIcons.GetIcon("Serializable");
            var containerIcon = EditorIcons.GetIcon("Container");
            
            listDrawer.drawIcon = (rect, o) => {
                rect = ItemIconDrawer.DrawAuto(rect, o);
                if (o is Component)
                    rect = ItemIconDrawer.DrawSolid(rect, unityIcon);
                if (o is ISerializable)
                    rect = ItemIconDrawer.DrawSolid(rect, serializableIcon);
                if (o is ILiveContextHolder)
                    rect = ItemIconDrawer.DrawSolid(rect, containerIcon);
                return rect;
            };
        }

        void SetCurrentContext(LiveContext context) {
            currentContext = context;
            rawView.SetProvider(null);
        }
        
        void OnGUI() {
            if (!Application.isPlaying) return;
            
            if (currentContext == null) 
                SetCurrentContext(LiveContext.Contexts.FirstOrDefault());

            if (currentContext == null)
                return;
            
            using (GUIHelper.Toolbar.Start()) {
                if (GUILayout.Button(currentContext.Name, EditorStyles.toolbarDropDown, GUILayout.Width(150))) {
                    ContextMenu menu = new ContextMenu();

                    foreach (var context in LiveContext.Contexts) {
                        var c = context;
                        menu.Add(context.Name, () => SetCurrentContext(c));
                    }
                
                    menu.Show();
                }
                
                GUILayout.FlexibleSpace();
                
                viewMode = (ViewMode) EditorGUILayout.EnumPopup(viewMode, EditorStyles.toolbarPopup, GUILayout.Width(100)); 
                editMode = (EditMode) EditorGUILayout.EnumPopup(editMode, EditorStyles.toolbarPopup, GUILayout.Width(100));
            }

            list.Clear();
            
            switch (viewMode) {
                case ViewMode.Items:
                    list.AddRange(currentContext.GetAll<ILiveContexted>());
                    break;
                case ViewMode.Arguments:  
                    list.AddRange(currentContext.GetAllArguments());
                    break;
            }

            using (splitter.Start(true, selectedItem != null && editMode != EditMode.None)) {
                if (splitter.Area())
                    listDrawer.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)), list);
                if (splitter.Area())
                    switch (editMode) {
                        case EditMode.Raw:
                            rawView.OnGUI(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                            break;
                        case EditMode.ObjectEditor:
                            ObjectEditor.Edit(selectedItem);
                            break;
                    }
            }
        }
    }
}
