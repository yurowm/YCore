using Yurowm.ObjectEditors;
using UnityEditor;

namespace Yurowm.Spaces {
    public class SpacePausePageExtensionEditor : ObjectEditor<SpacePausePageExtension> {
        public override void OnGUI(SpacePausePageExtension extension, object context = null) {
            BaseTypesEditor.SelectType<Space>("Space Type", extension, nameof(extension.spaceType));
            extension.pause = EditorGUILayout.Toggle("Pause", extension.pause);
        }
    }
}