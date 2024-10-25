using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.UI {
    public class PageTagEditor : ObjectEditor<PageTag> {
        public override void OnGUI(PageTag extension, object context = null) {
            extension.tag = EditorGUILayout.TextField("Tag", extension.tag);
        }
    }
}