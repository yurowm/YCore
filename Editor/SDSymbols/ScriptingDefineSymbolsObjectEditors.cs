using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.ObjectEditors;
using Yurowm.Utilities;

namespace Yurowm.EditorCore {
    public class ScriptingDefineSymbolEditor : ObjectEditor<ScriptingDefineSymbol> {
        
        readonly Color green = new Color(.5f, 1f, .5f);
        readonly Color red = new Color(1f, .5f, .5f);
        
        public override void OnGUI(ScriptingDefineSymbol sds, object context = null) {
            using (GUIHelper.Color.Start(sds.enable ? red : green))
                if (GUILayout.Button(sds.enable ? "Disable" : "Enable", GUILayout.Width(100)))
                    sds.enable = !sds.enable;
        }
    }
        
    public class ScriptingDefineSymbolAutoEditor : ObjectEditor<ScriptingDefineSymbolAuto> {
        
        readonly Color green = new Color(.5f, 1f, .5f);
        readonly Color red = new Color(1f, .5f, .5f);

        public override void OnGUI(ScriptingDefineSymbolAuto sds, object context = null) {
            var state = sds.IsEnabled();
            using (GUIHelper.Color.Start(state ? green : red))
                GUILayout.Label(state ? "Enabled" : "Disabled", Styles.whiteBoldLabel);    
        }
    }
}