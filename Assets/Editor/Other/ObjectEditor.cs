using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.Icons;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.ObjectEditors {
    public abstract class ObjectEditor {
        
        [MenuItem("Assets/Yurowm/C# Object Editor")]
        static void CreateCustomScript() {
            
            const string template = @"using Yurowm.ObjectEditors;
using UnityEditor;

namespace Yurowm.Editors {
    public class {typeName}Editor : ObjectEditor<{typeName}> {
        public override void OnGUI({typeName} obj, object context = null) {
            
        }
    }
}";
            
            var scripts = Selection.objects.CastIfPossible<MonoScript>().ToArray();
            
            if (scripts.Length == 0) {
                EditorUtility.DisplayDialog("Error",
                    "Select at least one script that will be used as a reference", 
                    "Ok");
                return;
            }
            
            bool DirectoryIsEditor(DirectoryInfo d) {
                if (d == null) return false;
                if (d.Name == "Editor") return true;
                return DirectoryIsEditor(d.Parent);
            }
            
            foreach (var script in scripts) {
                var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
                var directory = new DirectoryInfo(path);
                
                if (!DirectoryIsEditor(directory)) {
                    directory.CreateSubdirectory("Editor");
                    path = Path.Combine(path, "Editor");
                }
                
                var name = script.name + "Editor";
                
                string fullpath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name) + ".cs");
                
                var code = template
                    .FormatReplace("typeName", script.name);
                    
                File.WriteAllText(fullpath, code);                
            }
            
            AssetDatabase.Refresh();
        }
        
        static ObjectEditor[] editors = null;

        static ObjectEditor() {
            editors = Utils.FindInheritorTypes<ObjectEditor>(true)
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
                .Select(Activator.CreateInstance)
                .Cast<ObjectEditor>()
                .OrderBy(e => e.depth)
                .ToArray();
        }
        
        static Dictionary<Type, ObjectEditor[]> references = new Dictionary<Type, ObjectEditor[]>();
        
        static object current;

        #region Edit
        
        public static void Edit<T>(T obj, object context = null, float labelWidth = -1) {
            if (obj == null) return;

            var type = obj.GetType();
            
            if (type.IsValueType) {
                Debug.LogError("Value type variables are not supported for editing.");
                return;
            }
            
            if (!references.ContainsKey(type))
                references.Add(type, ObjectEditor.editors
                    .Where(e => e.IsSuitableType(type))
                    .ToArray());
            
            var editors = references[type];
            
            if (editors.Length == 0) return;
            
            using (GUIHelper.EditorLabelWidth.Start(labelWidth)) {
                current = obj;
                
                for (int i = 0; i < editors.Length; i++)
                    editors[i].OnGUI(obj, context);
            }
        }
        
        public static void Edit<T>(string label, T obj, object context = null) {
            using (GUIHelper.Vertical.Start()) {
                EditorGUILayout.LabelField(label, "");
                using (GUIHelper.IndentLevel.Start()) 
                    Edit(obj, context);
            }
        }
        
        public static void EditSeparate(object target, object context) {
            ObjectEditorWindow.Show(target, context, current, force: true);
        }
        
        static Dictionary<Type, Type[]> editableTypes = new Dictionary<Type, Type[]>();
        
        public static void EditStringList(string title, List<string> list, Action<GenericMenu> addNew = null) {
            using (GUIHelper.Area.Start(title)) {
                for (var i = 0; i < list.Count; i++) {
                    using (GUIHelper.Horizontal.Start()) {
                        list[i] = EditorGUILayout.TextField($"{i + 1}.", list[i]);
                        if (GUILayout.Button("...", GUILayout.Width(30))) {
                            int _i = i;
                            
                            var menu = new GenericMenu();
                            
                            menu.AddItem(new GUIContent("Remove"), false, () => {
                                list.RemoveAt(_i);
                                GUI.FocusControl(""); 
                            });
                            
                            menu.AddItem(new GUIContent("Duplicate"), false, () => {
                                list.Insert(_i, list[_i]);
                                GUI.FocusControl(""); 
                            });
                            
                            menu.ShowAsContext();
                        }
                    }
                }
                
                using (GUIHelper.Horizontal.Start()) {
                    var newLine = EditorGUILayout.TextField($"Add", "");
                    if (!newLine.IsNullOrEmpty())
                        list.Add(newLine);
                    if (addNew != null && GUILayout.Button("+", GUILayout.Width(30))) {
                        var menu = new GenericMenu();
                        addNew.Invoke(menu);
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
                }
            }    
        }
        
        public static void EditFloatList(string title, List<float> list) {
            using (GUIHelper.Area.Start(title)) {
                for (var i = 0; i < list.Count; i++) {
                    using (GUIHelper.Horizontal.Start()) {
                        list[i] = EditorGUILayout.FloatField($"{i + 1}.", list[i]);
                        if (GUILayout.Button("x", GUILayout.Width(30))) {
                            list.RemoveAt(i);
                            GUI.FocusControl(""); 
                        }
                    }
                }
                
                using (GUIHelper.Horizontal.Start()) {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(30)))
                        list.Add(0);
                }
            }    
        }
        
        public static void EditList<T>(string title, List<T> list, 
                object context = null, 
                bool editElements = true) {
            
            if (!editableTypes.TryGetValue(typeof(T), out var types)) {
                types = Utils
                    .FindInheritorTypes<T>(false)
                    .Where(t => !t.IsAbstract)
                    .OrderBy(t => t.FullName)
                    .ToArray();
                editableTypes.Add(typeof(T), types);
            }
            
            void AddNewElement() {
                var menu = new GenericMenu();

                foreach (var type in types) {
                    var _t = type;
                    
                    var path = types.Length > 1 ? type.FullName.Replace('.', '/') : type.Name;
                    
                    GenericMenu.MenuFunction action = () => {
                        try {
                            list.Add((T) Activator.CreateInstance(_t));
                        } catch (Exception e) {
                            UnityEngine.Debug.LogException(e);
                        }
                    };
                    
                    if (Event.current.alt)
                        menu.AddItem(new GUIContent($"Add/{type.Name}"), false, action);
                    else
                        menu.AddItem(new GUIContent($"Add/{path}"), false, action);
                }
                        
                menu.AddItem(new GUIContent($"Clipboard/Copy"), false, () => {
                    var container = new SerializableContainer<ISerializable>();
                    container.items.AddRange(list.CastIfPossible<ISerializable>());
                    EditorGUIUtility.systemCopyBuffer = container.ToRaw();
                });
                
                var reference = Serializator.FromTextData<SerializableContainer<ISerializable>>(EditorGUIUtility.systemCopyBuffer);
                
                if (reference != null)
                    menu.AddItem(new GUIContent($"Clipboard/Paste"), false, () => 
                        list.AddRange(reference.items.CastIfPossible<T>()));
                
                if (menu.GetItemCount() > 0)
                    menu.ShowAsContext();
            }
            
            void Edit(T element) {
                if (editElements)
                    ObjectEditor.Edit(element, context);
            }
            
            EditList(title, list, Edit, AddNewElement);
        }
        
        public static void EditList<T>(string title, List<T> list, Action<T> edit, Action addNewElement) {
            T Edit(T t) {
                edit?.Invoke(t);
                return t;
            }
            EditList(title, list, Edit, addNewElement);
        }     
        
        public static void EditList<T>(string title, List<T> list, Func<T, T> edit, Action addNewElement) {
            
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(title);
                GUILayout.FlexibleSpace();
                if (addNewElement != null && GUILayout.Button("...", GUILayout.Width(30))) addNewElement.Invoke();
            }
            
            using (GUIHelper.IndentLevel.Start()) {
                for (var i = 0; i < list.Count; i++) {
                    var element = list[i];
                    using (GUIHelper.Horizontal.Start()) {
                        // using (GUIHelper.EditorLabelWidth.Start(300)) 
                            EditorGUILayout.PrefixLabel($"{i + 1}. {element.GetType().Name.NameFormat(null, null, true)}");
                        
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button("...", GUILayout.Width(30))) {
                            var _i = i;
                            
                            GenericMenu menu = new GenericMenu();
                            
                            menu.AddItem(new GUIContent("Edit"), false, () => EditSeparate(element, null));
                            menu.AddItem(new GUIContent("Remove"), false, () => list.Remove(element));
                            if (element is ISerializable s)
                                menu.AddItem(new GUIContent("Duplicate"), false, () => list.Insert(_i, (T) s.Clone()));

                            #region Sorting

                            if (_i > 0) {
                                menu.AddItem(new GUIContent("Sort/Set First"), false, () => {
                                    list.Remove(element);
                                    list.Insert(0, element);
                                });
                                
                                menu.AddItem(new GUIContent("Sort/Move Up"), false, () => {
                                    list.Remove(element);
                                    list.Insert(_i - 1, element);
                                });
                            }
                            
                            if (_i < list.Count - 1) {
                                menu.AddItem(new GUIContent("Sort/Move Down"), false, () => {
                                    list.Remove(element);
                                    list.Insert(_i + 1, element);
                                });
                                
                                menu.AddItem(new GUIContent("Sort/Set Last"), false, () => {
                                    list.Remove(element);
                                    list.Insert(list.Count, element);
                                });
                            }
                            
                            #endregion
                            
                            if (menu.GetItemCount() > 0)
                                menu.ShowAsContext();
                        }
                    }
                    
                    if (edit != null)
                        using (GUIHelper.IndentLevel.Start())
                            list[i] = edit.Invoke(element);
                    
                    EditorGUILayout.Space();
                }
            }
        }
        
        #endregion

        public abstract void OnGUI(object obj, object context = null);
        public abstract bool IsSuitableType(Type type);
        
        protected int depth = 0;
        
        static ISerializable clipboard;

        public static void CopyPaste(string title, ISerializable serializable) {
            if (serializable == null)
                return;

            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(title);
                if (GUILayout.Button("Clipboard")) {
                    var menu = new GenericMenu();
                    
                    void Paste(string data) {
                        var ID = (serializable as ISerializableID)?.ID;
                        
                        Serializator.FromTextData(serializable, data);
                        
                        if (serializable is ISerializableID sID)
                            sID.ID = ID;
                    }
                    
                    void AddItem(string path, GenericMenu.MenuFunction action) {
                        if (action != null) 
                            menu.AddItem(new GUIContent(path), false, action);
                        else
                            menu.AddDisabledItem(new GUIContent(path));
                    }
                    
                    AddItem("Copy", () => {
                        clipboard = serializable;
                        EditorGUIUtility.systemCopyBuffer = clipboard.ToRaw();
                    });

                    AddItem("Paste/From Reference",
                        clipboard != null && clipboard.GetType() == serializable.GetType() ? 
                        () => Paste(clipboard.ToRaw()) : default(GenericMenu.MenuFunction));
                    
                    var systemCopyBufferRef = Serializator.FromTextData(EditorGUIUtility.systemCopyBuffer);
                    
                    AddItem("Paste/From System Copy Buffer",
                        systemCopyBufferRef != null && systemCopyBufferRef.GetType() == serializable.GetType() ? 
                            () => Paste(systemCopyBufferRef.ToRaw()) : default(GenericMenu.MenuFunction));

                    menu.ShowAsContext();
                }
            }
        }
    }
    
    public abstract class ObjectEditor<T> : ObjectEditor {
        
        public ObjectEditor() {
            depth = 0;
            
            var type = typeof(T);
            while (type != null) {
                depth++;
                type = type.BaseType;
            }
        }

        public override void OnGUI(object obj, object context = null) {
            OnGUI((T) obj, context);
        }

        public abstract void OnGUI(T obj, object context = null);

        public override bool IsSuitableType(Type type) {
            return typeof(T).IsAssignableFrom(type);
        }
    }
    
    public class ObjectEditorWindow : EditorWindow {
        State state;
        
        GUIHelper.Scroll scroll = new GUIHelper.Scroll();
        
        History<State> history = new History<State>(100);
        
        struct State {
            public object target;
            public object parent;
            public object context;
            public Action onChange;
            
            public bool IsEmpty => target == null;

            public override bool Equals(object obj) {
                return base.Equals(obj);
            }

            #region base

            public bool Equals(State other) {
                return Equals(target, other.target) 
                       && Equals(parent, other.parent)
                       && Equals(context, other.context)
                       && Equals(onChange, other.onChange);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (target != null ? target.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (parent != null ? parent.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (context != null ? context.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (onChange != null ? onChange.GetHashCode() : 0);
                    return hashCode;
                }
            }
            
            #endregion
        }
        
        static ObjectEditorWindow Show(bool force) {
            var window = Resources.FindObjectsOfTypeAll<ObjectEditorWindow>().FirstOrDefault();
            
            if (force && !window)
                window = CreateInstance<ObjectEditorWindow>();
            
            if (window) {
                window.titleContent = new GUIContent("Object Editor",
                    EditorIcons.GetUnityIcon("Settings@2x", "d_Settings@2x")); 
                window.ShowUtility();
            }
            
            return window;
        }
        
        public static void Show(object target, object context, object parent = null, Action onChange = null, bool force = false) {
            var window = Show(force);
            if (!window) return;

            window.state = new State {
                target = target,
                parent = parent,
                context = context,
                onChange = onChange
            };
                
            window.history.Next(window.state);
        }

        public void OnGUI() {
            if (!state.IsEmpty) {
                DrawNavigationBar();
                
                GUILayout.Label($"<{state.target.GetType().FullName}>", Styles.miniLabelBlack);

                using (state.onChange != null ? GUIHelper.Change.Start(state.onChange.Invoke) : null)
                    using (scroll.Start()) {
                        if (state.target is ISerializable serializable)
                            ObjectEditor.CopyPaste("Data", serializable);
                        ObjectEditor.Edit(state.target, state.context);
                    }
            }
            
            Repaint();
        }

        void DrawNavigationBar() {
            if (history.IsEmpty) return;

            using (GUIHelper.Toolbar.Start()) {
                if (history.HasPrevious) {
                    if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(30)))
                        state = history.Back();
                }

                if (history.HasNext) {
                    if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(30)))
                        state = history.Forward();
                }
                
                if (state.parent != null) {
                    if (GUILayout.Button("^", EditorStyles.toolbarButton, GUILayout.Width(30))) {
                        var parent = history.JumpTo(s => s.target == state.parent);
                        if (!parent.IsEmpty)
                            state = parent;
                        
                    }
                }
                
                if (GUILayout.Button(state.target.GetType().Name, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(true))) {
                    var menu = new GenericMenu();

                    foreach (var element in history.GetAll()) {
                        var s = element;
                        
                        menu.AddItem(new GUIContent(s.target.GetType().FullName), 
                            element.target == history.Current.target, () => {
                                if (s.target == history.Current.target)
                                    return;
                                state = history.JumpTo(s);
                            });
                    }
                    
                    menu.ShowAsContext();
                }
            }
        }
    }
    public class ActionWindow : EditorWindow {
        Action action;
        Action onChanged;

        GUIHelper.Scroll scroll = new GUIHelper.Scroll();
        
        public static void Show(Action action, Action onChanged, bool force) {
            var window = GetWindow(force);
            if (!window) return;
            window.action = action;
            window.onChanged = onChanged;
            window.titleContent = new GUIContent("Object Editor",
                EditorIcons.GetUnityIcon("Settings@2x", "d_Settings@2x")); 
            window.ShowUtility();
        }
        
        static ActionWindow GetWindow(bool force) {
            var result = Resources.FindObjectsOfTypeAll<ActionWindow>().FirstOrDefault();
            
            if (force && !result)
                result = CreateInstance<ActionWindow>();

            return result;
        }

        public void OnGUI() {
            if (action != null)
                using (onChanged != null ? GUIHelper.Change.Start(onChanged.Invoke) : null)
                using (scroll.Start())
                    action.Invoke();
        }
    }
}

