using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.Integrations;
using Yurowm.ObjectEditors;

namespace YMatchThree.Services {
    public class DataStorageEditor : ObjectEditor<DataStorage> {
        public override void OnGUI(DataStorage integration, object context = null) {
            EditList("Data", integration.data);
        }
        
        public static void KeyInfo(string label, DataProviderIntegration.Data.Type type, string key) {
            using (GUIHelper.SingleLine.Start(label)) {
                GUILayout.Label($"{key} ({type.ToString()})", EditorStyles.boldLabel, GUILayout.ExpandHeight(true));
                if (GUILayout.Button("Copy", GUILayout.Width(40)))
                    EditorGUIUtility.systemCopyBuffer = key;
            }
        }
    }
}