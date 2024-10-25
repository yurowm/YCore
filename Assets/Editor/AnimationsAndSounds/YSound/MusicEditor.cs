using UnityEngine;
using Yurowm.ObjectEditors;
using Yurowm.Sounds;
using Yurowm.Spaces;

namespace Yurowm.Editors {
    public class MusicEditor : ObjectEditor<Music> {
        public override void OnGUI(Music obj, object context = null) {
            BaseTypesEditor.SelectAsset<AudioClip>(obj, nameof(obj.clipName));
        }
    }
}