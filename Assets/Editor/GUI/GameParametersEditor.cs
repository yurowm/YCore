using System.IO;
using Yurowm.ObjectEditors;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.Serialization;
using Yurowm.UI;

namespace Yurowm.YPlanets.Editor {
    public class GameParametersEditor : ObjectEditor<GameParameters> {
        public override void OnGUI(GameParameters parameters, object context = null) {
            foreach (var module in parameters.GetModules()) {
                GUILayout.Label(module.GetName(), Styles.miniTitle);
                using (GUIHelper.IndentLevel.Start()) 
                    Edit(module);
            }
        }

        [MenuItem("Yurowm/Tools/Wipe Data")]
        public static void WipeData() {
            var directory = new DirectoryInfo(Application.persistentDataPath);
            if (directory.Exists)
                directory.Delete(true);
            PlayerPrefs.DeleteAll();
        }
    }
    
    public class GameParametersGeneralEditor : ObjectEditor<GameParametersGeneral> {
        public override void OnGUI(GameParametersGeneral parameters, object context = null) {
            parameters.supportEmail = EditorGUILayout.TextField("Support Email", parameters.supportEmail);
            parameters.privacyPolicyURL = EditorGUILayout.TextField("Private policy URL", parameters.privacyPolicyURL);
            parameters.termsOfUseURL = EditorGUILayout.TextField("Terms of use URL", parameters.termsOfUseURL);

            parameters.maxDeltaTime = EditorGUILayout.FloatField("Delta Time (Max)", parameters.maxDeltaTime).ClampMin(1f / 60);
            parameters.userRestoreInEditor = EditorGUILayout.Toggle("User restore (Editor)", parameters.userRestoreInEditor);
            
            parameters.fakeDeviceID = EditorGUILayout.TextField("Fake Device ID", parameters.fakeDeviceID);
            
            parameters.forceLayout = (LayoutPreset.Layout) EditorGUILayout.EnumFlagsField("Force Layout", parameters.forceLayout);
        }
    }

    [DashboardGroup("Content")]
    [DashboardTab("Game", null)]
    public class GameParametersStorageEditor : PropertyStorageEditor {
        protected override IPropertyStorage EmitNew() {
            return new GameParameters();
        }
    }
}