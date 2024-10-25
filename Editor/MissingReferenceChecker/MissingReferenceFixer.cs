using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Object = UnityEngine.Object;

namespace Yurowm.Utilities {
    public class MissingReferenceFixer {
        
        enum State {
            WaitingTarget,
            WaitingReplacement,
            FileList,
            Fixing
        }
        
        State state = State.WaitingTarget;
        
        string targetGUID = "";
        string replacementGUID = "";

        List<string> files = new List<string>();
        string report = "";

        List<GUID> existedGUIDs = new List<GUID>();
        List<GUID> missedGUIDs = new List<GUID>();
        
        public void OnGUI() {
            switch (state) {
                case State.WaitingTarget: {
                    GUILayout.Label("Target Reference");
                    EditorGUILayout.LabelField("GUID", targetGUID);

                    if (GUILayout.Button("Scan GUIDs", GUILayout.Width(150)))
                        ScanGUIDs();
                    if (missedGUIDs.Count > 0 && GUILayout.Button("Select...", GUILayout.Width(100))) {
                        GenericMenu menu = new GenericMenu();
                        
                        foreach (var guid in missedGUIDs) {
                            var g = guid;   
                            menu.AddItem(new GUIContent($"{guid.path}/{guid.value}"), false, () => {
                                targetGUID = g.value;
                            });
                        }
                        
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
                    
                    using (GUIHelper.Lock.Start(targetGUID.IsNullOrEmpty())) 
                        if (GUILayout.Button("Search Files", GUILayout.Width(150)))
                            SearchTargetFiles();
                        
                } break;
                case State.FileList: {
                    EditorGUILayout.LabelField("GUID", targetGUID);
                    if (GUILayout.Button("< Back", GUILayout.Width(150))) {
                        files.Clear();
                        report = "";
                        state = State.WaitingTarget;
                    }
                    if (GUILayout.Button("Fix it", GUILayout.Width(150))) 
                        state = State.WaitingReplacement;
                    GUILayout.Label(report);
                } break;
                case State.WaitingReplacement: {
                    EditorGUILayout.LabelField("GUID", targetGUID);
                    
                    EditorGUILayout.LabelField("Replacement", replacementGUID);
                    
                    if (GUILayout.Button("Select...", GUILayout.Width(100))) {
                        GenericMenu menu = new GenericMenu();
                        
                        foreach (var guid in existedGUIDs) {
                            var g = guid;   
                            var itemPath = $"{guid.path[0]}/{guid.path.Replace('.', '/')}";
                            menu.AddItem(new GUIContent(itemPath), false, () => {
                                replacementGUID = g.value;
                            });
                        }
                        
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
                        
                    using (GUIHelper.Lock.Start(replacementGUID.IsNullOrEmpty())) 
                        if (GUILayout.Button("Fixes Files", GUILayout.Width(150)))
                            FixFiles();
                } break;
                case State.Fixing: {
                    GUILayout.Label(report);
                    if (GUILayout.Button("< Main", GUILayout.Width(150))) {
                        files.Clear();
                        report = "";
                        state = State.WaitingTarget;
                    }
                } break;
            }
        }
        
        void ScanGUIDs() {
            
            float progress = 0;
            
            void ProgressBar(string status) {
                progress += 0.1f;
                progress = progress.Repeat(1);
                EditorUtility.DisplayProgressBar("Scanning", status, progress);
            }
            
            state = State.WaitingTarget;
            
            var directory = new DirectoryInfo(Application.dataPath).Parent;
            DelayedAccess access = new DelayedAccess(1f / 3);
            
            Regex guidFilter = new Regex(@"\{.*guid\: (?<id>[a-f0-9]{32}).*\}");

            List<GUID> guids = new List<GUID>();
            List<GUID> foundGUIDs = new List<GUID>();
            
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                ProgressBar(path);
                
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                
                if (asset) {
                    var guid = new GUID(AssetDatabase.AssetPathToGUID(path), asset.GetType());
                    if (asset is MonoScript script && typeof(Component).IsAssignableFrom(script.GetClass()))
                        guid.path = script.name;
                    guids.Add(guid);
                }
                
                if (!path.StartsWith("Assets/")) continue;
                
                var fullName = Path.Combine(directory.FullName, path); 
                var file = new FileInfo(fullName);

                switch (file.Extension) {
                    case ".prefab":
                    case ".asset":
                    case ".unity":
                        break;
                    default: continue;
                }
                
                string raw = File.ReadAllText(fullName);

                foreach (Match match in guidFilter.Matches(raw))
                    foundGUIDs.Add(new GUID(match.Groups["id"].Value, path:path));
            }
            
            foundGUIDs.RemoveAll(f => f.value.StartsWith("0000000000"));
            
            guids.ForEach(e => foundGUIDs.RemoveAll(f => f.value == e.value));
            
            missedGUIDs = foundGUIDs.OrderBy(f => f.path).ToList();
            
            StringBuilder builder = new StringBuilder();
            
            foundGUIDs.ForEach(m => builder.AppendLine($"{m.value}: {m.path}"));

            existedGUIDs = guids.Where(g => !g.path.IsNullOrEmpty())
                .OrderBy(g => g.path)
                .ToList();
            
            EditorUtility.ClearProgressBar();
        }
        
        void SearchTargetFiles() {
            float progress = 0;
            
            void ProgressBar(string status) {
                progress += 0.1f;
                progress = progress.Repeat(1);
                EditorUtility.DisplayProgressBar("Searching", status, progress);
            }
            
            state = State.FileList;
            
            files.Clear();
            
            DelayedAccess access = new DelayedAccess(1f / 3);
            
            var directory = new DirectoryInfo(Application.dataPath).Parent;
            
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                ProgressBar(path);
                
                if (!path.StartsWith("Assets/")) continue;
                
                var fullName = Path.Combine(directory.FullName, path); 
                var file = new FileInfo(fullName);

                switch (file.Extension) {
                    case ".prefab":
                    case ".asset":
                    case ".unity":
                        break;
                    default: continue;
                }
                
                string raw = File.ReadAllText(fullName); 
                
                if (raw.Contains(targetGUID))
                    files.Add(path);
            }
            
            report = string.Join("\n", files.ToArray());
            
            EditorUtility.ClearProgressBar();
        }
        
        void FixFiles() {
            
            float progress = 0;
            
            void ProgressBar(string status) {
                progress += 0.1f;
                progress = progress.Repeat(1);
                EditorUtility.DisplayProgressBar("Searching", status, progress);
            }
            
            state = State.Fixing;
            
            var directory = new DirectoryInfo(Application.dataPath).Parent;
            
            foreach (var path in files) {
                ProgressBar(path);
                
                if (!path.StartsWith("Assets/")) continue;
                
                var fullName = Path.Combine(directory.FullName, path); 
                var file = new FileInfo(fullName);

                string raw = File.ReadAllText(fullName); 
                
                raw = raw.Replace(targetGUID, replacementGUID);
                
                File.WriteAllText(fullName, raw);
            }
            
            report = "Success";
            
            targetGUID = "";
            replacementGUID = "";
            
            EditorUtility.ClearProgressBar();
            
            AssetDatabase.Refresh();
        }

        struct GUID {
            public string path;
            public string value;
            public Type type;

            public GUID(string value, Type type = null, string path = null) {
                this.value = value;
                this.type = type;
                this.path = path;
            }
        }
    }
    
}
