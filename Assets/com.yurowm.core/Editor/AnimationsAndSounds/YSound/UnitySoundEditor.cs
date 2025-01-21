using UnityEngine;
using Yurowm.ObjectEditors;
using Yurowm.Sounds;
using Yurowm.Spaces;

namespace Yurowm.Editors {
    public class UnitySoundEditor : ObjectEditor<UnitySound> {
        public override void OnGUI(UnitySound obj, object context = null) {
            BaseTypesEditor.SelectAsset<AudioClip>(obj, nameof(obj.clipName));
        }
    }
}