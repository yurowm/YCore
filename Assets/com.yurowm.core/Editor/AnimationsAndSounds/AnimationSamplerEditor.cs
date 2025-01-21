using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;

namespace Yurowm {
    [CustomEditor(typeof(AnimationSampler))]
    public class AnimationSamplerEditor : UnityEditor.Editor {
        AnimationSampler sampler;
        Animation animation;
        
        void OnEnable() {
            sampler = target as AnimationSampler;
            animation = sampler.GetComponent<Animation>();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            if (animation != null && GUIHelper.Button(null, "Remove Temp Component")) 
                DestroyImmediate(animation);
            
            if (GUIHelper.Button(null, "Edit")) {
                if (animation != null)
                    DestroyImmediate(animation);
                animation = sampler.gameObject.AddComponent<Animation>();
                SetupAnimation();
                EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
            }
                
            SetupAnimation();
        }
        
        void SetupAnimation() {
            if (animation == null) return;
            animation.hideFlags = HideFlags.HideAndDontSave;
            animation.playAutomatically = false;
            if (sampler.clip)
                animation.AddClip(sampler.clip, sampler.clip.name);
        }
    }
}