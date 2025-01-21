using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Integrations;
using Yurowm.ObjectEditors;
using Yurowm.Sounds;

namespace Yurowm.Editors {
    public class SoundHapticWaveFormEditor : ObjectEditor<SoundHapticWaveForm> {
        public override void OnGUI(SoundHapticWaveForm waveForm, object context = null) {
            // waveForm.curve = EditorGUILayout.CurveField("Pattern", waveForm.curve);
            using (GUIHelper.Horizontal.Start()) {
                waveForm.pattern = EditorGUILayout.TextArea(waveForm.pattern);
                if (GUILayout.Button("<", GUILayout.Width(30))) {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Constant"), false, () => {
                        waveForm.pattern = AddPattern(waveForm.pattern, "constant:dur,ampl");
                    });
                    menu.AddItem(new GUIContent("Pause"), false, () => {
                        waveForm.pattern = AddPattern(waveForm.pattern, "pause:dur");
                    });
                    menu.AddItem(new GUIContent("Linear"), false, () => {
                        waveForm.pattern = AddPattern(waveForm.pattern, "linear:dur,ampl,ampl");
                    });
                    menu.ShowAsContext();
                }
            }
            // if (GUILayout.Button("Build"))
            //     waveForm.Build(.03f, 16);
        }
        
        string AddPattern(string pattern, string add) {
            var list = pattern?
                    .Split("\n")
                    .ToList() ?? new List<string>();
            list.Add(add);
            return list
                .Select(l => l.Trim())
                .Where(l => !l.IsNullOrEmpty())
                .Join(";");
        }
    }
}