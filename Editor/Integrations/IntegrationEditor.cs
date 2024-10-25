using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.Integrations {
    public class IntegrationEditor : ObjectEditor<Integration> {

        public override void OnGUI(Integration integration, object context = null) {
            integration.active = EditorGUILayout.Toggle("Active", integration.active);
            var issues = integration.GetIssues();
            if (issues.HasFlag(Integration.Issue.SDK))
                EditorGUILayout.HelpBox("SDK isn't installed", MessageType.Error, false);
            if (issues.HasFlag(Integration.Issue.Platform))
                EditorGUILayout.HelpBox("Unsupported platform", MessageType.Error, false);
        }
    }
}