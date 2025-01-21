using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm {
    public static class ClipComponent {
        public static void PlayClip(this GameObject gameObject, string clipName) {
            if (!gameObject || clipName.IsNullOrEmpty())
                return;
            gameObject
                .GetComponents<IClipComponent>()
                .ForEach(c => c.Play(clipName));
        } 
        
        public static void PlayClip(this Component component, string clipName) {
            if (!component) return;
            using (ExtensionsUnityEngine.ProfileSample("PlayClip"))
                PlayClip(component?.gameObject, clipName);
        } 
    }
    
    public interface IClipComponent {
        void Play(string clipName);
    }
}