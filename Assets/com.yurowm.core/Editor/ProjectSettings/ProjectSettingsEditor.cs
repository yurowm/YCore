using Yurowm.ObjectEditors;
using UnityEditor;
using Yurowm.Core;
using Yurowm.Dashboard;
using Yurowm.Serialization;

namespace Yurowm.YPlanets.Editor {
    public class ProjectSettingsEditor : ObjectEditor<ProjectSettings> {
        
        public override void OnGUI(ProjectSettings settings, object context = null) {
            settings.autoVersionName = EditorGUILayout.Toggle("Auto Version Name", settings.autoVersionName);
            settings.increaseBuildCode = EditorGUILayout.Toggle("Increase Build Code", settings.increaseBuildCode);
            EditorGUILayout.LabelField("Version", settings.versionName);
        }
    }
    
    [DashboardGroup("Content")]
    [DashboardTab("Project", "Puzzle")]
    public class ProjectSettingsStorageEditor : PropertyStorageEditor {
        
        protected override IPropertyStorage EmitNew() {
            return new ProjectSettings();
        }
    }
}