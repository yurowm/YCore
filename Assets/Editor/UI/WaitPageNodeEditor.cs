using Yurowm.ObjectEditors;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Nodes.Editor;
using Yurowm.UI;

namespace Yurowm.YPlanets.UI {
    public class WaitPageNodeEditor : NodeEditor<WaitPageNode> {
        public override void OnNodeGUI(WaitPageNode node, NodeSystemEditor editor = null) {
            EditorGUILayout.LabelField("Pages", node.pageNames.Join(", "));
        }

        public override void OnParametersGUI(WaitPageNode node, NodeSystemEditor editor = null) {
            void EditPage(string p) {
                EditorGUILayout.LabelField(p);
            }
            
            void AddNewPage() {
                var menu = new GenericMenu();

                foreach (var page in Page.storage.items) {
                    var pageName = page.ID;
                    if (node.pageNames.Contains(pageName))
                        continue;
                    menu.AddItem(new GUIContent(pageName), false, () => {
                        node.pageNames.Add(pageName);
                        node.pageNames.Sort();
                    });
                }
                
                if (menu.GetItemCount() > 0)
                    menu.ShowAsContext();
            }
            
            node.immediate = EditorGUILayout.Toggle("Immediate", node.immediate);
            
            ObjectEditor.EditList("Pages", node.pageNames, EditPage, AddNewPage);
        }
    }
}