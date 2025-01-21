using System;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Icons;

namespace Yurowm.Dashboard {
    public interface IDashboardEditor {
        EditorWindow window { get; set; }
        bool isScrollable { get; }
        void OnGUI();
        bool Initialize();
        void OnFocus();
        void OnToolbarGUI();
    }

    public abstract class DashboardEditorSO<S> : IDashboardEditor where S : ScriptableObject {

        public EditorWindow window { get; set; }

        public virtual bool isScrollable => true;

        S target = null;

        public S metaTarget {
            get {
                if (target == null)
                    target = AssetDatabase.LoadAssetAtPath<S>(GetAssetPath());
                if (target == null) {
                    target = ScriptableObject.CreateInstance<S>();
                    AssetDatabase.CreateAsset(target, GetAssetPath());
                    AssetDatabase.SaveAssets();
                }
                return target;
            }
        }

        public abstract string GetAssetPath();

        public abstract void OnGUI();
        public abstract bool Initialize();

        public void Repaint() {
            window.Repaint();
        }

        public virtual void OnFocus() { }
        public virtual void OnToolbarGUI() {
            GUILayout.FlexibleSpace();
        }
    }

    public abstract class DashboardEditor<T> : IDashboardEditor where T : UnityEngine.Object {
        T target = null;
        public virtual bool isScrollable => true;
        public EditorWindow window { get; set; }
        public T metaTarget {
            get {
                if (target == null)
                    target = FindTarget();
                return target;
            }
        }

        public abstract T FindTarget();

        public abstract void OnGUI();
        public abstract bool Initialize();

        public void Repaint() {
            window.Repaint();
        }

        public virtual void OnFocus() { }
        public virtual void OnToolbarGUI() {
            GUILayout.FlexibleSpace();
        }
    }

    public abstract class DashboardEditor : DashboardEditor<UnityEngine.Object> {
        public override UnityEngine.Object FindTarget() {
            return null;
        }
    }

    public class RegualarEditor<M> : EditorWindow where M : DashboardEditor, new() {
        M editor;

        public RegualarEditor() { }

        public static E Emit<E>() where E : RegualarEditor<M>, new() {
            E window = GetWindow<E>();
            
            window.titleContent = new GUIContent(typeof(M).Name.NameFormat(null, "Editor", true));
            window.OnEnable();
            window.Show();
            return window;
        }

        void OnFocus() {
            editor?.OnFocus();
        }

        void OnEnable() {
            if (editor == null) {
                editor = (M) Activator.CreateInstance(typeof(M));
                editor.window = this;
                if (!editor.Initialize())
                    editor = null;
            }
        }

        void OnGUI() {
            editor?.OnGUI();
        }
    }

    public class DashboardDefaultAttribute : Attribute { }

    public class DashboardTabAttribute : Attribute {
        float priority;
        string title;
        Texture2D icon = null;

        public DashboardTabAttribute(string title, int priority = 0) {
            this.title = title;
            this.priority = priority;
        }

        public DashboardTabAttribute(string title, string icon, int priority = 0) {
            this.title = title;
            this.priority = priority;
            if (!string.IsNullOrEmpty(icon)) {
                this.icon = EditorIcons.GetIcon(icon);
                if (!this.icon)
                    this.icon = EditorIcons.GetIcon(icon);
            }
        }

        public string Title => title;
        
        public Texture2D Icon => icon;
    }

    public class DashboardGroupAttribute : Attribute {
        string group;

        public DashboardGroupAttribute(string group) {
            this.group = group;
        }

        public string Group => group;
    }

}