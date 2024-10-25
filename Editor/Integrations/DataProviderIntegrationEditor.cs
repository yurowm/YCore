using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.Integrations {
    public class DataProviderIntegrationDataEditor : ObjectEditor<DataProviderIntegration.Data> {

        public override void OnGUI(DataProviderIntegration.Data data, object context = null) {
            using (GUIHelper.Horizontal.Start()) {
                data.type = (DataProviderIntegration.Data.Type) EditorGUILayout.EnumPopup(data.type, GUILayout.Width(100));
                using (GUIHelper.IndentLevel.Zero()) {
                    data.ID = EditorGUILayout.TextField(data.ID, GUILayout.Width(150));
                    switch (data.type) {
                        case DataProviderIntegration.Data.Type.Bool: {
                            if (data.value is not bool)
                                data.value = default(bool);
                            data.value = EditorGUILayout.Toggle((bool) data.value);
                            break;
                        }
                        case DataProviderIntegration.Data.Type.Float: {
                            if (data.value is not float)
                                data.value = 0f;
                            data.value = EditorGUILayout.FloatField((float) data.value);
                            break;
                        }
                        case DataProviderIntegration.Data.Type.Int: {
                            if (data.value is not int)
                                data.value = 0;
                            data.value = EditorGUILayout.IntField((int) data.value);
                            break;
                        }
                        case DataProviderIntegration.Data.Type.String: {
                            if (data.value is not string)
                                data.value = string.Empty;
                            data.value = EditorGUILayout.TextField((string) data.value);
                            break;
                        }
                    }
                }
            }
        }
    }
}