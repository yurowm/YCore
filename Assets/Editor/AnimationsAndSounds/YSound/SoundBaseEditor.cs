using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.Sounds {
    public class SoundBaseEditor : ObjectEditor<SoundBase> {
        public override void OnGUI(SoundBase obj, object context = null) {
            obj.tag = (SoundBase.Tag)EditorGUILayout.EnumFlagsField("Tag", obj.tag);
        }
    }
    
    public class SoundEditor : ObjectEditor<Sound> {
        public override void OnGUI(Sound obj, object context = null) {
            EditList("Modules", obj.modules);
        }
    }
}