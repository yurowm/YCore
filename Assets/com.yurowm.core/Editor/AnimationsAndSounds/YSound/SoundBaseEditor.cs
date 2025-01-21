using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.Sounds {
    public class SoundBaseEditor : ObjectEditor<SoundBase> {
        public override void OnGUI(SoundBase obj, object context = null) {
            obj.tag = (SoundBase.Tag)EditorGUILayout.EnumFlagsField("Tag", obj.tag);
        }
    }
    
    public class SoundEditor : ObjectEditor<SoundEffect> {
        public override void OnGUI(SoundEffect obj, object context = null) {
            EditList("Modules", obj.modules);
        }
    }
}