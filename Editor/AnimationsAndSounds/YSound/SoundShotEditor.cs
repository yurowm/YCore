using UnityEngine;
using Yurowm.ObjectEditors;
using Yurowm.Sounds;
using Yurowm.Spaces;

namespace Yurowm.Editors {
    public class SoundShotEditor : ObjectEditor<SoundShot> {
        public override void OnGUI(SoundShot obj, object context = null) {
            BaseTypesEditor.SelectAsset<AudioClip>(obj, nameof(obj.clipName));
        }
    }
}