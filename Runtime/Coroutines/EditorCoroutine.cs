#if UNITY_EDITOR
using UnityEditor;
using Yurowm.Coroutines;

namespace Yurowm {
    public static class EditorCoroutine {
        static CoroutineCore core;
        
        static EditorCoroutine() {
            core = new CoroutineCore();
            core.playModeOnly = false;
            EditorApplication.update += () => core.Update(CoroutineCore.Loop.Update);
        }

        public static CoroutineCore GetCore() => core;
    }
}
#endif