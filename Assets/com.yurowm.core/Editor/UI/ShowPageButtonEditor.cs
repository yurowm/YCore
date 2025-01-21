using UnityEditor;

namespace Yurowm.UI {
    [CustomEditor(typeof(ShowPageButton))]
    [CanEditMultipleObjects]
    public class ShowPageButtonEditor : UnityEditor.Editor {
        
        SerializedProperty typeProp;
        SerializedProperty pageIDProp;
        
        void OnEnable() {
            var provider = target as ShowPageButton;
            
            typeProp = serializedObject.FindProperty(nameof(provider.type));    
            pageIDProp = serializedObject.FindProperty(nameof(provider.pageID));
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            Undo.RecordObject(target, null);
            
            if (typeProp.hasMultipleDifferentValues || typeProp.enumValueIndex == (int) ShowPageButton.Type.ByName)
                PageEditor.SelectPageProperty(pageIDProp);
        }
    }
}