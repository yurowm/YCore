using UnityEditor;
using Yurowm.ObjectEditors;

namespace Yurowm.Serialization {
  
    public class StorageElementExtraDataEditor : ObjectEditor<IStorageElementExtraData> {
        public override void OnGUI(IStorageElementExtraData data, object context = null) {
            data.storageElementFlags = (StorageElementFlags) EditorGUILayout
                .EnumFlagsField("Element Flags", data.storageElementFlags);

            if (data.storageElementFlags.HasFlag(StorageElementFlags.WorkInProgress))
                EditorGUILayout.HelpBox("Work In Progress", MessageType.Warning, false);
        }
    }
}