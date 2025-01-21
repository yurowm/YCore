using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class UnpackPackages : Optimization {
        
        public string path = "_Yurowm/Packages";
        public List<string> patterns = new List<string>();
        
        FileInfo packagesFile;
        DirectoryInfo projectFolder;
        
        Regex pareser = new Regex(@"""(?<key>[^""]+)"":\s*""(?<value>[^""]+)""");

        public override void OnInitialize() {
            projectFolder = new DirectoryInfo(Path.Combine(Application.dataPath, path));
            packagesFile = new FileInfo(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json"));
        }

        public override bool DoAnalysis() {
            var raw = File.ReadAllText(packagesFile.FullName);
            
            var passed = true;
            
            foreach (Match match in pareser.Matches(raw))
                if (match.Groups["value"].Value.StartsWith("file:") 
                    && Pass(match.Groups["key"].Value)) {
                    report += match.Groups["key"].Value + "\n";
                    passed = false;
                }

            return passed;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }
        
        bool Pass(string key) {
            if (patterns.IsEmpty())
                return false;
            return patterns.Any(key.Contains);
        }

        public override void Fix() {
            #region Remove old code

            projectFolder.Refresh();
            
            if (projectFolder.Exists) {
                projectFolder.Delete(true);
                projectFolder.Create();
            } else 
                projectFolder.Create();

            #endregion

            #region Load Packages

            string raw = File.ReadAllText(packagesFile.FullName);
            
            Dictionary<string, string> packagesToKeep = new Dictionary<string, string>();
            int startIndex = int.MaxValue;
            int endIndex = int.MinValue;
            
            foreach (Match match in pareser.Matches(raw)) {
                startIndex = Mathf.Min(startIndex, match.Index);
                endIndex = Mathf.Max(endIndex, match.Index + match.Length);
                
                if (match.Groups["value"].Value.StartsWith("file:") 
                    && Pass(match.Groups["key"].Value)) {
                    var dir = new DirectoryInfo(match.Groups["value"].Value.Substring(5));
                    UnpackPackage(dir);
                } else
                    packagesToKeep.Add(match.Groups["key"].Value, match.Groups["value"].Value);
            }
            
            #endregion

            #region Update JSON

            if (endIndex > startIndex) {
                string result = raw.Substring(0, startIndex) +
                                packagesToKeep.Select(p => $"\"{p.Key}\": \"{p.Value}\"").Join(",\n") +
                                raw.Substring(endIndex);
                File.WriteAllText(packagesFile.FullName, result);
            }

            #endregion
            
            UnityEditor.PackageManager.Client.Resolve();
        }

        void UnpackPackage(DirectoryInfo packageDir) {
            var targetDir = new DirectoryInfo(Path.Combine(projectFolder.FullName, packageDir.Name));
            
            DirectoryCopy(packageDir, targetDir.FullName);
            
            targetDir.GetFiles().ForEach(f => f.Delete());
            foreach (var subDir in targetDir.GetDirectories()) {
                if (subDir.Name == "Tests")
                    subDir.Delete(true);
            }
        }
        
        void DirectoryCopy(DirectoryInfo dir, string destDirName) {

            if (!dir.Exists)
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + dir.FullName);
            
            DirectoryInfo[] dirs = dir.GetDirectories();
            
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);
            
            foreach (FileInfo file in dir.GetFiles())
                file.CopyTo(Path.Combine(destDirName, file.Name), false);
            
            foreach (DirectoryInfo subdir in dirs)
                DirectoryCopy(subdir, Path.Combine(destDirName, subdir.Name));
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("path", path);
            writer.Write("patterns", patterns.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("path", ref path);
            patterns.Reuse(reader.ReadCollection<string>("patterns"));
        }
    }
    
    public class UnpackPackagesEditor : ObjectEditor<UnpackPackages> {
        public override void OnGUI(UnpackPackages optimization, object context = null) {
            optimization.path = EditorGUILayout.TextField("Path", optimization.path);
            EditStringList("Patterns", optimization.patterns);
        }
    }
}