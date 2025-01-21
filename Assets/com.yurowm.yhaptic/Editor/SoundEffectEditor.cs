using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Integrations;
using Yurowm.ObjectEditors;
using Yurowm.Sounds;
using Yurowm.Utilities;

namespace Yurowm.Editors {
    public class SoundHapticEditor : ObjectEditor<SoundHapticIOS> {
        public override void OnGUI(SoundHapticIOS sound, object context = null) {
            sound.type = (HapticType)EditorGUILayout.EnumPopup("Type", sound.type);
        }
    }
    
    public class SoundHapticAHAPEditor : ObjectEditor<SoundHapticAHAP> {
        public override void OnGUI(SoundHapticAHAP sound, object context = null) {
            using (GUIHelper.Horizontal.Start()) {
                if (GUIHelper.Button("Haptic", sound.path)) {
                    var menu = new GenericMenu();

                    foreach (var p in HapticHelpers.GetAllHapticPaths()) {
                        var _p = p;
                        menu.AddItem(new GUIContent(p), p == sound.path, () => 
                            sound.path = _p);
                    }
                    
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }
        }
    }
}