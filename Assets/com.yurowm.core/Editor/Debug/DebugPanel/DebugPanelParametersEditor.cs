using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.DebugTools {
    public class DebugPanelParametersEditor : ObjectEditor<DebugPanelParameters> {
        GUIHelper.Password password = new ();
        
        public override void OnGUI(DebugPanelParameters parameters, object context = null) {
            parameters.password = password.Edit("Password", parameters.password);
        }
    }
}