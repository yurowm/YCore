using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Integrations;

namespace Yurowm.Services {
    public class APIIntegrationEditor : ObjectEditor<APIIntegration> {
        public override void OnGUI(APIIntegration api, object context = null) {
        }
    }
}