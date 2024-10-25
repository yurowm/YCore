using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.Editors {
    public class ReserveParamteresEditor : ObjectEditor<ReserveParameters> {
        public override void OnGUI(ReserveParameters parameters, object context = null) {
            parameters.active = EditorGUILayout.Toggle("Active", parameters.active);
            if (parameters.active)
                parameters.levelPoolManagment = EditorGUILayout.Toggle("Level pool managment", parameters.levelPoolManagment);
        }
    }
}
