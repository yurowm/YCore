using UnityEditor;
using UnityEngine;

namespace Yurowm.Shaders {
    public class WrapModeProperty : MaterialPropertyDrawer {
        
        enum WrapMode {
            Default = -1,
            Clamp = 0,
            Loop = 1,
            Mirror = 2,
            MirrorOnce = 3,
        }
        
        public override void OnGUI (Rect position, MaterialProperty prop, string label, MaterialEditor editor) {
            WrapMode mode = (WrapMode) prop.floatValue.RoundToInt();

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            mode = (WrapMode) EditorGUI.EnumPopup(position, label, mode);

            EditorGUI.showMixedValue = false;
            
            if (EditorGUI.EndChangeCheck()) 
                prop.floatValue = (float) mode;
        }
    }
}