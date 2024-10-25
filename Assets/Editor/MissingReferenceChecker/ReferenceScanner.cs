using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;

namespace Yurowm.Utilities {
    
    [DashboardGroup("Development")]
    [DashboardTab("Reference Scanner", "Hammer")]
    public class ReferenceScanner: DashboardEditor {
        Object asset;
        Object replace;

        List<Ref> references = new ();
        ReferencesList list;
        
        public override bool Initialize() {
            list = new (references);
        
            return true;
        }

        public override void OnGUI() {
            asset = EditorGUILayout.ObjectField(new GUIContent("Asset"), asset, typeof(Object), true);
            
            if (asset && replace && replace.GetType() != asset.GetType())
                replace = null;
            
            if (asset)
                replace = EditorGUILayout.ObjectField(new GUIContent("Replace"), replace, asset.GetType(), true);
            
            using (GUIHelper.Horizontal.Start()) {
                if (asset && GUILayout.Button("Scan", GUILayout.Width(150))) 
                    Scanning(asset);
                
                if (asset && replace && asset != replace && asset.GetType() == replace.GetType())
                    if (GUILayout.Button("Replace", GUILayout.Width(150)))
                        Replace();
            }
            
            list.OnGUI();
        }

        public override void OnToolbarGUI() {
            base.OnToolbarGUI();
            
            var selected = Selection.objects;
            
            if (!selected.IsEmpty() && GUILayout.Button(new GUIContent($"Scan: {(selected.Length == 1 ? selected[0].name : "Few objects")}"), EditorStyles.toolbarButton))
                Scanning(selected);
        }

        public static IEnumerable<Ref> GetReferences<MB>() where MB: MonoBehaviour {
            
            MonoScript FindScript() {
                string[] guids = AssetDatabase.FindAssets("t:Script");
                var type = typeof(MB);
                foreach (string guid in guids) {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (asset != null && asset.GetClass() != null && asset.GetClass() == type)
                        return asset;
                }
                return null;
            }
                
            var script = FindScript();
                
            return script == null ? Enumerable.Empty<Ref>() : GetReferences(script);
        }

        public static IEnumerable<Ref> GetReferences(Object asset) {
            return GetRefs(asset, asset).Collect<Ref>();
        }
        
        static IEnumerable GetRefs(Object asset, params Object[] objects) {
            if (objects.IsEmpty())
                yield break;
            
            bool CheckRef(Object o) => objects.Contains(o);

            float progress = 0;
            
            void ProgressBar(string status) {
                progress += 0.1f;
                progress = progress.Repeat(1);
                EditorUtility.DisplayProgressBar("Scanning", status, progress);
            }
            
            bool Next(SerializedProperty property, bool visibleOnly) {
                return visibleOnly ? property.NextVisible(true) : property.Next(true);
            }
            
            IEnumerable CheckObject(Object reference, string path, Object obj, bool visibleOnly = true) {
                var so = new SerializedObject(obj);
                var sp = so.GetIterator();
                         
                while (Next(sp, visibleOnly)) {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference) {
                        var objRef = sp.objectReferenceValue;
                        if (objRef && CheckRef(objRef)) {
                            var entry = new Ref {
                                reference = reference,
                                property = sp.Copy(),
                                path = path
                            };
                            
                            yield return entry;
                        }
                    }
                }
            }
            
            string GetPath(Transform transform) {
                if (!transform) return "";
                return $"{GetPath(transform.parent)}/{transform.name}";
            }
            
            IEnumerable CheckGameObject(string path, GameObject gameObject) {
                var _path = path + GetPath(gameObject.transform);
                yield return CheckObject(gameObject, _path, gameObject, false);

                foreach (var component in gameObject.GetComponents<Component>().NotNull())
                    yield return CheckObject(gameObject, _path, component);
            }
            
            var assetGO = asset as GameObject;
            
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                ProgressBar(path);
                var transform = AssetDatabase.LoadAssetAtPath<Transform>(path);
                
                if (transform) {
                    if (assetGO && assetGO == transform.gameObject)
                        continue;
                    
                    foreach (var child in transform.AndAllChild())
                        yield return CheckGameObject(path, child.gameObject);
                    
                    continue;
                }

                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj) 
                    yield return CheckObject(obj, path, obj);
            }

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++) {
                var scene = SceneManager.GetSceneAt(sceneIndex);

                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var child in root.transform.AndAllChild()) {
                        ProgressBar($"@{scene.name}: {child.name}");
                        yield return CheckGameObject("@" + scene.name, child.gameObject);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        void Scanning(params Object[] objects) {
            references.Reuse(GetRefs(asset, objects).Collect<Ref>());
            
            list.folderCollection.Clear();
            list.Reload();
            list.ExpandAll();
        }
        
        void Replace() {
            if (!asset || !replace || asset.GetType() != replace.GetType())
                return;
            
            foreach (var reference in GetRefs(asset, asset).Collect<Ref>().ToArray()) {
                reference.property.objectReferenceValue = replace;
                reference.property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        public class Ref {
            public Object reference;
            public string path;
            public SerializedProperty property;
        }
        
        class ReferencesList: HierarchyList<Ref> {
            public ReferencesList(List<Ref> refs) : base(refs, new List<TreeFolder>(), new TreeViewState()) {
                onDoubleClick += r => Selection.objects = new[] { r.reference };
            }
            
            public override int GetUniqueID(Ref element) {
                return element.reference.GetInstanceID();
            }

            public override Ref CreateItem() {
                return null;
            }

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                if (selected.Count == 1) {
                    var element = selected[0];
                    if (element.isFolderKind && element.name.EndsWith(".prefab")) {
                        menu.AddItem(new GUIContent("Select"), false, () => {
                            var path = element.fullPath;
                            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            Selection.objects = new [] { asset };
                        });
                    }
                }
            }

            public override void SetPath(Ref element, string path) {}

            public override string GetName(Ref element) {
                if (!element.reference)
                    return "Missed";
                
                return $"{element.reference.name} ({element.property.displayName})";
            }

            public override string GetPath(Ref element) {
                return element.path;
            }
        }
    }
}
