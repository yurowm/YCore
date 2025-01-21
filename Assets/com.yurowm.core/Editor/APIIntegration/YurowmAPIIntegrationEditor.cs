using UnityEditor;
using UnityEngine;
using YMatchThree.Services;
using Yurowm.ObjectEditors;
using Yurowm.GUIHelpers;
using Yurowm.Integrations;

namespace Yurowm.Services {
    public class YurowmAPIIntegrationEditor : ObjectEditor<YurowmAPIIntegration> {
        GUIHelper.Password secretEditor = new ();
        
        public override void OnGUI(YurowmAPIIntegration api, object context = null) {
            using (GUIHelper.SingleLine.Start("Debug Host")) {
                api.debug = EditorGUILayout.Toggle(api.debug, GUILayout.Width(30));
                if (api.debug)
                    api.hostDebug = EditorGUILayout.TextField(api.hostDebug);
            }
            DataStorageEditor.KeyInfo("Host Data Key", DataProviderIntegration.Data.Type.String, YurowmAPIIntegration.hostDataKey);
            
            api.secret = secretEditor.Edit("Secret", api.secret);
        }
    }
}