using UnityEditor;
using Yurowm.Nodes.Editor;
using Yurowm.UI;

namespace Yurowm.YPlanets.UI {
    public class WaitButtonClickNodeEditor : NodeEditor<WaitButtonClickNode> {
        public override void OnNodeGUI(WaitButtonClickNode node, NodeSystemEditor editor = null) {
            EditorGUILayout.LabelField("Wait", node.buttonID);
            if (node.buttonLock)
                EditorGUILayout.LabelField("Locked");
        }

        public override void OnParametersGUI(WaitButtonClickNode node, NodeSystemEditor editor = null) {
            node.buttonID = EditorGUILayout.TextField("Button ID", node.buttonID);
            node.buttonLock = EditorGUILayout.Toggle("Lock", node.buttonLock);
        }
    }
}