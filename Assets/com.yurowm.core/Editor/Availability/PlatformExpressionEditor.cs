using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.Editors {
    public class IPlatformExpressionEditor: ObjectEditor<IPlatformExpression> {
        public override void OnGUI(IPlatformExpression ipe, object context = null) {
            ipe.platformExpression = PlatformExpressionEditor.Edit(ipe.platformExpression);
        }
    }
    
    public static class PlatformExpressionEditor {
        
        public static string Edit(string expression) {
            using (GUIHelper.Horizontal.Start()) {
                expression = EditorGUILayout.TextField("Platform Expression", expression);
                if (GUILayout.Button("?", GUILayout.Width(25))) {
                    var message = $"Arguments:\n\n{AllValues().ToArray().Join(", ")}";
                    EditorUtility.DisplayDialog("Platform Expression", message, "Ok");
                }                
                if (GUILayout.Button("v", GUILayout.Width(25))) 
                    EditorUtility.DisplayDialog("Platform Expression Result", PlatformExpression.Evaluate(expression).ToString(), "Ok");
            }
            
            return expression;
        }
        
        public static IEnumerable<string> AllValues() {
            foreach (var value in PlatformExpression.AllValues())
                yield return value;

            bool IsDeprecated(BuildTarget buildTarget) {
                var fieldInfo = typeof(BuildTarget).GetField(buildTarget.ToString());
                return fieldInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false).Any();
            }
            
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
                if (!IsDeprecated(target))
                    yield return target.ToString();
        }
        
    }
}