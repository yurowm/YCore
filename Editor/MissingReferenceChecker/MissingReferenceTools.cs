using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.HierarchyLists;
using Yurowm.Serialization;
using Object = UnityEngine.Object;

namespace Yurowm.Utilities {
    [DashboardGroup("Development")]
    [DashboardTab("Missing References", "Hammer")]
    public class MissingReferenceTools : DashboardEditor {
        
        ErrorList list;
        
        List<Error> errors = new();
        
        MissingReferenceFixer fixer = new();
        
        Mode mode = Mode.Scanner;
        enum Mode {
            Scanner,
            Fixer,
        }
        
        public override bool Initialize() {
            list = new ErrorList(errors);
            
            return true;
        }
        
        public override void OnGUI() {
            switch (mode) {
                case Mode.Scanner: list.OnGUI(); break;
                case Mode.Fixer: fixer.OnGUI(); break;
            }
        }

        public override void OnToolbarGUI() {
            mode = (Mode) EditorGUILayout.EnumPopup(mode, EditorStyles.toolbarPopup, GUILayout.Width(100));
            if (mode == Mode.Scanner)
                if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    Scanning();
                    
            GUILayout.Label($"Errors: {errors.Count}", EditorStyles.toolbarButton);
        }
        
        void Scanning() {
            
            float progress = 0;
            
            void ProgressBar(string status) {
                progress += 0.1f;
                progress = progress.Repeat(1);
                EditorUtility.DisplayProgressBar("Scanning", status, progress);
            }
            
            errors.Clear();
            list.Reload();
            
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                ProgressBar(path);
                var asset = AssetDatabase.LoadAssetAtPath<Transform>(path);
                if (asset) {
                    foreach (var child in asset.AndAllChild()) {
                        CheckGameObject(path, child.gameObject);
                    }
                    continue;
                }

                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so) 
                    CheckObject(so, path, so);
            }

            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++) {
                var scene = SceneManager.GetSceneAt(sceneIndex);

                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var child in root.transform.AndAllChild()) {
                        ProgressBar($"@{scene.name}: {child.name}");
                        CheckGameObject("Scene:" + scene.name, child.gameObject);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();

            if (EditorStorage.Instance.GetSerializable("MRT_Ignore") is Ignore settings)
                errors.RemoveAll(e => settings.ignores.Contains(e.message));

            list.folderCollection.Clear();
            list.Reload();
        }

        string GetPath(Transform transform) {
            if (transform == null) return "";
            return GetPath(transform.parent) + "/" + transform.name;
        }
        
        void CheckGameObject(string path, GameObject gameObject) {
            var _path = path + GetPath(gameObject.transform);
            CheckObject(gameObject, _path, gameObject, false);

            foreach (var component in gameObject.GetComponents<Component>().NotNull())
                CheckObject(gameObject, _path, component);
        }
        
        void CheckObject(Object reference, string path, Object obj, bool visibleOnly = true) {
            var so = new SerializedObject(obj);
            var sp = so.GetIterator();
                         
            while (Next(sp, visibleOnly))
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                        CheckMissedReference(reference, sp, path, obj);
        }
        
        bool Next(SerializedProperty property, bool visibleOnly) {
            return visibleOnly ? property.NextVisible(true) : property.Next(true);
        }
        
        void CheckMissedReference(Object reference, SerializedProperty property, string path, Object obj) {
            var error = new Error {
                reference = reference,
                path = path,
                message = $"{obj.GetType().Name}.{property.displayName} ({property.type})"
            };

            errors.Add(error);
        }
        
        class Error {
            static int indexer = 0;
            int index;
            
            public int Index => index;
            public string message;
            public Object reference;
            public string report;
            public string path;

            public Error() {
                index = indexer++;
            }
            
            public Error(string message) : this() {
                this.message = message;
            }
            
            public void Draw(Rect rect) {
                GUI.Label(rect, message);
            }
            
            public void ContextMenu(GenericMenu menu) {
                menu.AddItem(new GUIContent("Print Report"), false, () => {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine(report);
                    Debug.Log(builder.ToString());
                });
            }
        }
        
        public class Ignore: ISerializable {
            public HashSet<string> ignores = new();
            
            public void Serialize(IWriter writer) {
                writer.Write("ignores", ignores.ToArray());
            }

            public void Deserialize(IReader reader) {
                ignores.Clear();
                ignores.UnionWith(reader.ReadCollection<string>("ignores"));
            }
        }
        
        class ErrorList : HierarchyList<Error> {
            public ErrorList(List<Error> collection) : base(collection, new (), new TreeViewState()) {}

            public override void DrawItem(Rect rect, ItemInfo info) {
                info.content.Draw(rect);
            }

            public override string GetPath(Error element) {
                return element.path;
            }

            public override void SetPath(Error element, string path) { }

            public override int GetUniqueID(Error element) {
                return element.Index;
            }

            public override Error CreateItem() {
                return null;
            }
            
            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                if (selected.Any(s => s.isItemKind)) {
                    menu.AddItem(new GUIContent("Select"), false, () => {
                        Selection.objects = selected
                            .Where(i => i.isItemKind)
                            .Select(i => i.asItemKind.content.reference)
                            .ToArray();
                    });
                    menu.AddItem(new GUIContent("Remove"), false, () => {
                        var messages = selected
                            .Where(i => i.isItemKind)
                            .Select(i => i.asItemKind.content.message)
                            .ToArray();
                        
                        if (messages.Any()) {
                            itemCollection.RemoveAll(i => messages.Contains(i.message));
                            Reload();
                        }
                    });
                    
                    menu.AddItem(new GUIContent("Ignore"), false, () => {
                        var messages = selected
                            .Where(i => i.isItemKind)
                            .Select(i => i.asItemKind.content.message)
                            .ToArray();
                    
                        var settings = EditorStorage.Instance.GetSerializable("MRT_Ignore") as Ignore ?? new Ignore();
                        
                        settings.ignores.UnionWith(messages);
                        
                        EditorStorage.Instance.SetSerializable("MRT_Ignore", settings);
                        
                        if (messages.Any()) {
                            itemCollection.RemoveAll(i => messages.Contains(i.message));
                            Reload();
                        }
                    });
                }
                
                if (selected.Count == 1) {
                    var element = selected[0];
                
                    if (element.isItemKind) element.asItemKind.content.ContextMenu(menu);

                    if (element.isFolderKind && element.name.EndsWith(".prefab")) {
                        menu.AddItem(new GUIContent("Select"), false, () => {
                            var path = element.fullPath;
                            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            Selection.objects = new [] { asset };
                        });
                    }
                }
            }

            public override bool CanRename(ItemInfo info) {
                return false;
            }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }
            
            
        }
    }
    
}
