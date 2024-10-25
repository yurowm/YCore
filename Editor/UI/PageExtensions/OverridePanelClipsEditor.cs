using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.UI {
    public class OverridePanelClipsEditor : ObjectEditor<OverridePanelClips> {
        public override void OnGUI(OverridePanelClips extension, object context = null) {
            var currentPanel = PageStorageEditor.GetPanel(extension.panelLinkID);
            
            if (GUIHelper.Button("Panel", currentPanel == null ? "<NULL>" : currentPanel.name)) {
                var panels = PageStorageEditor.GetPanels()
                    .OrderBy(p => p.name)
                    .ToArray();
                    
                if (panels.Length > 0) {
                    var menu = new GenericMenu();

                    foreach (var panel in panels) {
                        var p = panel;
                            
                        menu.AddItem(new GUIContent(panel.name), 
                            currentPanel && currentPanel.linkID == panel.linkID, 
                            () => extension.panelLinkID = p.linkID);
                    }
                        
                    menu.ShowAsContext();
                }
            }
            
            extension.showClip = EditorGUILayout.TextField("Show Clip", extension.showClip);
            extension.hideClip = EditorGUILayout.TextField("Hide Clip", extension.hideClip);
        }
    }
}