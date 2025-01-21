using System.Linq;
using Yurowm.ObjectEditors;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;

namespace Yurowm.UI {
    public class PageEditor : ObjectEditor<Page> {
        
        public override void OnGUI(Page page, object context = null) {
            #region Panels

            foreach (var panelInfo in page.panels) {
                using (GUIHelper.Horizontal.Start()) {
                    var panel = PageStorageEditor.GetPanel(panelInfo.linkID);
                    
                    if (panel) {
                        var label = panel.name;
                        
                        EditorGUILayout.LabelField(label, Styles.richLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                        
                        if (Event.current.type == EventType.Repaint)
                            Handles.DrawSolidRectangleWithOutline(
                                GUILayoutUtility.GetLastRect(),
                                GetModeColor(panelInfo.mode).Transparent(.1f),
                                Color.clear);
                            
                        panelInfo.mode = (Page.PanelInfo.Mode) EditorGUILayout.EnumPopup(panelInfo.mode);
                    } else {
                        using (GUIHelper.Color.Start(Color.red))
                            if (GUIHelper.Button(panelInfo.linkID.ToString(), "Copy ID"))
                                EditorGUIUtility.systemCopyBuffer = panelInfo.linkID.ToString();
                        GUILayout.FlexibleSpace();
                    }
                    if (GUILayout.Button("X", GUILayout.Width(30))) {
                        page.panels.Remove(panelInfo);
                        break;
                    }
                }
            }
            
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel("Other");
                    
                if (Event.current.type == EventType.Repaint)
                    Handles.DrawSolidRectangleWithOutline(
                        GUILayoutUtility.GetLastRect(),
                        GetModeColor(page.defaultMode).Transparent(.1f),
                        Color.clear);
                        
                page.defaultMode = (Page.PanelInfo.Mode) EditorGUILayout.EnumPopup(page.defaultMode);
                if (GUILayout.Button("+", GUILayout.Width(30))) {
                    var panels = PageStorageEditor.GetPanels()
                        .Where(p => page.panels.All(u => p.linkID != u.linkID))
                        .OrderBy(p => p.name)
                        .ToArray();
                    
                    if (panels.Length > 0) {
                        var menu = new GenericMenu();
                        
                        if (panels.Length > 1) 
                            menu.AddItem(new GUIContent("<All>"), false, () => {
                                panels.ForEach(p => page.panels.Add(new Page.PanelInfo {
                                    linkID = p.linkID
                                }));
                            });

                        foreach (var panel in panels) {
                            var p = panel;
                            
                            menu.AddItem(new GUIContent(panel.name), false, () => {
                                page.panels.Add(new Page.PanelInfo {
                                    linkID = p.linkID
                                });
                            });
                        }
                        
                        menu.ShowAsContext();
                    }
                        
                }
            }

            #endregion

            EditList("Extensions", page.extensions);
        }

        static Color GetModeColor(Page.PanelInfo.Mode mode) {
            switch (mode) {
                case Page.PanelInfo.Mode.Ignore: return Color.yellow;
                case Page.PanelInfo.Mode.Disable: return Color.red;
                case Page.PanelInfo.Mode.Enable: return Color.green;
            }
            return Color.clear;
        }
        
        
        public static void SelectPageProperty(SerializedProperty property) {
            
            var position = EditorGUILayout.GetControlRect(true);
            
            var label = new GUIContent(property.displayName);
            
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var value = property.hasMultipleDifferentValues ? "..." : property.stringValue;
                    
            if (GUI.Button(position, value, EditorStyles.popup)) {
                var menu = new GenericMenu();
                        
                foreach (var pageID in Page.storage.items.Select(p => p.ID).OrderBy(i => i)) {
                    var i = pageID;
                    var current = !property.hasMultipleDifferentValues && pageID == value;
                    menu.AddItem(new GUIContent(pageID), current, () => {
                        if (current) return;
                        property.stringValue = i;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                
                if (menu.GetItemCount() > 0)
                    menu.ShowAsContext();
            }
            
            EditorGUI.EndProperty();
        }
    }
}