using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class ProjectStructure : Optimization {
        
        public List<FolderStructure> structures = new List<FolderStructure>();
        public List<FolderToCheck> foldersToCheck = new List<FolderToCheck>();

        IEnumerable<DirectoryInfo> GetFolders() {
            foreach (var folder in foldersToCheck) {
                var directoryInfo = new DirectoryInfo(Path.Combine(Application.dataPath, folder.path));
                if (!directoryInfo.Exists) continue;

                foreach (var f in GetSubDirectories(directoryInfo, folder.subfolders))
                    yield return f;
            }
        }
        
        IEnumerable<DirectoryInfo> GetSubDirectories(DirectoryInfo directoryInfo, int depth) {
            if (depth <= 0) {
                yield return directoryInfo;
                yield break;
            }
            
            if (depth == 1) {
                foreach (var d in directoryInfo.GetDirectories())
                    yield return d;
                yield break;
            }

            foreach (var d in directoryInfo.GetDirectories())
            foreach (var sd in GetSubDirectories(d, depth - 1))
                yield return sd;
        }

        public override bool DoAnalysis() {

            foreach (var dir in GetFolders()) { 
                foreach (var file in GetFiles(dir)) {
                    if (file.Extension == ".meta") continue;

                    var p = Path.Combine(dir.FullName, GetRightPath(file), file.Name);
                    if (file.FullName != p)
                        return false;
                }
                
            }

            return true;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        StringBuilder errors;
        DirectoryInfo _d;

        public override void Fix() {
            errors = new StringBuilder();
            
            foreach (var dir in GetFolders()) {
            
                foreach (var file in GetFiles(dir)) {
                    if (file.Name.EndsWith(".meta")) continue;
                    
                    MoveFile(file, Path.Combine(dir.FullName, GetRightPath(file)));
                }
            }
            
            RemoveEmptyScriptsAndFolders.Execute(Application.dataPath);
            
            

            if (errors.Length > 0)
                throw new Exception(errors.ToString());
        }

        string GetRightPath(FileInfo file) {
            string result;
            
            if (KeepStructure(file, "Plugins", out result))
                return result;
            
            if (KeepStructure(file, "Resources", out result))
                return result;
            
            result = structures.FirstOrDefault(s => s.extensions.Contains(file.Extension.ToLower()))?.folder ?? "Other";

            if (file.FullName.Contains(Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar))
                result = Path.Combine(result, "Editor");
            
            return result;
        }
        
        bool KeepStructure(FileInfo file, string keyFolder, out string result) {
            result = null;
            if (!file.FullName.Contains(Path.DirectorySeparatorChar + keyFolder + Path.DirectorySeparatorChar))
                return false;
            
            var directory = file.Directory;
            result = "";
            
            while (true) {
                result = Path.Combine(directory.Name, result);
                if (directory.Name == keyFolder) break;
                directory = directory.Parent;
            }
            
            return true;
        }
        
        void MoveFile(FileInfo file, string distination) {
            if (file.Directory.FullName == distination)
                return;
            
            if (!Directory.Exists(distination))
                Directory.CreateDirectory(distination);
            
            string newFilePath = Path.Combine(distination, file.Name);
            
            if (File.Exists(newFilePath)) {
                errors.AppendLine($"Can't move \"{file.Name}\" into \"{distination}\" becase file with this name already exists");
                return;
            }
            
            File.Move(file.FullName, Path.Combine(distination, file.Name));
            
            file = new FileInfo(file.FullName + ".meta");
            if (file.Exists)
                File.Move(file.FullName, Path.Combine(distination, file.Name));
        }

        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory) {
            foreach (var file in directory.GetFiles())
                yield return file;
            
            foreach (var file in directory.GetDirectories().SelectMany(GetFiles))
                yield return file;
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("structures", structures.ToArray());
            writer.Write("foldersToCheck", foldersToCheck.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            structures.Reuse(reader.ReadCollection<FolderStructure>("structures"));
            foldersToCheck.Reuse(reader.ReadCollection<FolderToCheck>("foldersToCheck"));
        }
        
        public class FolderToCheck : ISerializable {
            
            public string path;
            public int subfolders = 0;
            
            public void Serialize(IWriter writer) {
                writer.Write("path", path);
                writer.Write("subfolders", subfolders);
            }

            public void Deserialize(IReader reader) {
                reader.Read("path", ref path);
                reader.Read("subfolders", ref subfolders);
            }
        }
        
        public class FolderStructure : ISerializable {
            
            public List<string> extensions = new List<string>();
            public string folder;
            
            public void Serialize(IWriter writer) {
                writer.Write("extensions", extensions.ToArray());
                writer.Write("folder", folder);
            }

            public void Deserialize(IReader reader) {
                extensions.Reuse(reader
                    .ReadCollection<string>("extensions")
                    .Where(e => !e.IsNullOrEmpty())
                    .Select(e => e.StartsWith(".") ? e : "." + e));
                reader.Read("folder", ref folder);
            }
        }
    }
    
    public class ProjectStructureEditor : ObjectEditor<ProjectStructure> {
        public override void OnGUI(ProjectStructure optimization, object context = null) {
            EditList("Folders To Check", optimization.foldersToCheck);
            EditList("Structures", optimization.structures);
        }
    }
    
    public class ProjectStructureFolderStructureEditor : ObjectEditor<ProjectStructure.FolderStructure> {
        public override void OnGUI(ProjectStructure.FolderStructure s, object context = null) {
            s.folder = EditorGUILayout.TextField("Folder", s.folder);
            EditStringList("Extensions", s.extensions);
        }
    }
    
    public class ProjectStructureFolderToCheckEditor : ObjectEditor<ProjectStructure.FolderToCheck> {
        public override void OnGUI(ProjectStructure.FolderToCheck f, object context = null) {
            f.path = EditorGUILayout.TextField("Path", f.path);
            f.subfolders = EditorGUILayout.IntField("Subfolders", f.subfolders).ClampMin(0);
        }
    }
}