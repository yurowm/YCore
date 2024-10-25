using UnityEditor;
using UnityEngine;
using Yurowm.Nodes.Editor;

namespace Yurowm.Nodes {
    public class LogNodeEditor : NodeEditor<LogNode> {
        public override void OnNodeGUI(LogNode node, NodeSystemEditor editor = null) { 
            node.message = EditorGUILayout.TextArea(node.message, GUILayout.MinHeight(40));
        }

        public override void OnParametersGUI(LogNode node, NodeSystemEditor editor = null) {
            OnNodeGUI(node, editor);
        }
    }
}