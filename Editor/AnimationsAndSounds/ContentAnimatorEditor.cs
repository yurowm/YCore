using UnityEngine;
using UnityEditor;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Clip = Yurowm.ContentAnimator.Clip;

namespace Yurowm {
    [CustomEditor(typeof(ContentAnimator))]
    [CanEditMultipleObjects]
    public class ContentAnimatorEditor : UnityEditor.Editor {

        ContentAnimator animator;
        Animation animation;

        void OnEnable () {
            animator = (ContentAnimator) target;
            animation = animator.GetComponent<Animation>();

            RemoveEmpty();
            
            Undo.RecordObject(animator, null);
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            if (!serializedObject.isEditingMultipleObjects) {
                if (animation != null && GUIHelper.Button(null, "Remove Temp Component")) 
                    DestroyImmediate(animation);
            
                Undo.RecordObject(target, null);

                foreach (Clip clip in animator.clips)
                    ClipSelector(clip);

                NewClip();
                
                RemoveEmpty();
            }
        }

        void RemoveEmpty() {
            if (animator.clips.RemoveAll(c => c.name.IsNullOrEmpty()) > 0)
                GUI.FocusControl("");
        }

        void ClipSelector(Clip selected) {
            using (GUIHelper.Horizontal.Start()) {
                selected.name = EditorGUILayout.TextField(selected.name, GUILayout.Width(EditorGUIUtility.labelWidth));
                
                selected.clip = (AnimationClip) EditorGUILayout.ObjectField(selected.clip, typeof(AnimationClip), false);

                if (GUILayout.Button(selected.reverse ? "<" : ">", EditorStyles.miniButton, GUILayout.Width(25)))
                    selected.reverse = !selected.reverse;

                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(40))) {
                    if (animation != null)
                        DestroyImmediate(animation);
                    animation = animator.gameObject.AddComponent<Animation>();
                    SetupAnimation(selected.clip);
                    EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
                }
            }
            
            SetupAnimation(null);
        }
        
        void NewClip() {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            
            if (Event.current.type == EventType.Layout) return;

            rect.xMin += EditorGUIUtility.labelWidth;
            
            var newClip = (AnimationClip) EditorGUI.ObjectField(rect, null, typeof(AnimationClip), false);
            
            if (newClip)
                animator.clips.Add(new Clip(newClip.name) {
                    clip = newClip
                });
        }
        
        void SetupAnimation(AnimationClip clip) {
            if (animation == null) return;
            animation.hideFlags = HideFlags.HideAndDontSave;
            animation.playAutomatically = false;
            if (clip)
                animation.AddClip(clip, clip.name);
        }
    }
}