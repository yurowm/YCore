using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Utilities.RnD {
    public abstract class SpeedTestSection : TestSection {
        protected int testCount = 10000;
    
        string result = "";
    
        public override void OnGUI() {
            
            testCount = Mathf.Clamp(EditorGUILayout.IntField("Tests Count", testCount), 1, 1000000);
            
            if (GUILayout.Button("Run Test")) {
                
                StringBuilder builder = new StringBuilder();

                foreach (var script in Scripts()) {
                    DateTime startTime = DateTime.Now;
                
                    script.action.Invoke(testCount);
                    
                    builder.AppendLine($"{script.name}: {(DateTime.Now - startTime).TotalMilliseconds} ms.");
                }
                
                result = builder.ToString();
            }
        
            GUILayout.Label(result);
        }
        
        protected abstract IEnumerable<Script> Scripts();

        protected class Script {
            public string name;
            public Action<int> action;
        
            public Script(string name, Action<int> action) {
                this.name = name;
                this.action = action;
            }
        }
    }
}