using Yurowm.ObjectEditors;
using UnityEditor;
using UnityEngine;
using Yurowm.Nodes.Editor;

namespace Yurowm.Core {
    public class UserPathEditor : ObjectEditor<UserPath> {
        public override void OnGUI(UserPath path, object context = null) {
            if (GUILayout.Button("Edit"))
                NodeSystemEditorWindow.Show(path);
        }
    }
    
    public class AppEventEditor : NodeEditor<UserPath.AppEvent> {
        public override void OnNodeGUI(UserPath.AppEvent node, NodeSystemEditor editor = null) {
            node.events = (UserPath.AppEvent.Event) EditorGUILayout.EnumFlagsField("Events", node.events);
        }

        public override void OnParametersGUI(UserPath.AppEvent node, NodeSystemEditor editor = null) {
            OnNodeGUI(node, editor);
        }
    }
}