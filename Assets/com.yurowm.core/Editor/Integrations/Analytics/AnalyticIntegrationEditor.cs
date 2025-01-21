using UnityEditor;
using Yurowm.Analytics;
using Yurowm.ObjectEditors;

namespace Yurowm.Advertising {
    public class AnalyticIntegrationEditor : ObjectEditor<AnalyticIntegration> {
        public override void OnGUI(AnalyticIntegration integration, object context = null) {  
            integration.trackAll = EditorGUILayout.Toggle("Track All", integration.trackAll);
        }
    }
}