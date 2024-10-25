using UnityEditor;

namespace Utilities.RnD {
    public abstract class TestSection {
        public EditorWindow editor;
        public virtual void Initialize() {}
        public abstract void OnGUI();
        public void Repaint() {
            editor.Repaint();
        }
    }
}