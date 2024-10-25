using System.Linq;
using UnityEditor;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;

namespace Yurowm.Serialization {
    public class SerializableIDEditor : ObjectEditor<ISerializableID> {
        public SerializableIDEditor() {
            depth = -100;
        }

        public override void OnGUI(ISerializableID serializable, object context = null) {
            if (context is IStorageEditor storageEditor) {
                var ID = serializable.ID;
                
                ID = EditorGUILayout.TextField("ID", ID);
                
                if (ID != serializable.ID)
                    if (storageEditor.GetStoredItems().CastIfPossible<ISerializableID>()
                        .All(s => s == serializable || s.ID != ID))
                        serializable.ID = ID;
            } else 
                EditorGUILayout.LabelField("ID", serializable.ID);
        }
    }
}