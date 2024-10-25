using UnityEditor;
using Yurowm.Nodes.Editor;

namespace Yurowm.Core {
    public class WaitNodeEditor : NodeEditor<WaitNode> {
        public override void OnNodeGUI(WaitNode node, NodeSystemEditor editor = null) {
            node.seconds = EditorGUILayout.FloatField("Duration (sec.)", node.seconds).ClampMin(0);
        }

        public override void OnParametersGUI(WaitNode node, NodeSystemEditor editor = null) {
            OnNodeGUI(node, editor);
        }
    }
}