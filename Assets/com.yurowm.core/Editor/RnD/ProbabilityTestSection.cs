using UnityEditor;
using UnityEngine;

namespace Utilities.RnD {
    public abstract class ProbabilityTestSection : TestSection {
    
        int testCount = 100;
    
        string result = "";
    
        public override void OnGUI() {
            
            testCount = Mathf.Clamp(EditorGUILayout.IntField("Tests Count", testCount), 10, 100000);
        
            if (GUILayout.Button("Run Test")) {
                int wins = 0;
                for (int i = 0; i < testCount; i++)
                    if (Case())
                        wins++;
                result = $"{100f * wins / testCount}%";
            }
        
            GUILayout.Label(result);
        }
        
        protected abstract bool Case();
    }
}