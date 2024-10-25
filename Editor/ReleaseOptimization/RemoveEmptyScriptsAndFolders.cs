using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.DeveloperTools {
    public class RemoveEmptyScriptsAndFolders : Optimization {
        
        DirectoryInfo projectFolder;
        
        public override void OnInitialize() {
            projectFolder = new DirectoryInfo(Application.dataPath);
        }

        public override bool DoAnalysis() {
            return !IsEmptyFolders(projectFolder.FullName);
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            Execute(projectFolder.FullName);
            AssetDatabase.Refresh();
        }

        public static void Execute(string parentFolder) {
            foreach (var file in Directory.GetFiles(parentFolder)) {
                if (Path.GetExtension(file) != ".cs") continue;

                if (!File.ReadLines(file).All(string.IsNullOrWhiteSpace)) continue;

                var info = new FileInfo(file);
                File.Delete(info.FullName);
                File.Delete(Path.Combine(info.Directory.FullName, info.Name + ".meta"));
            }
            foreach (var directory in Directory.GetDirectories(parentFolder)) {
                Execute(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0) {
                    Directory.Delete(directory, false);
                    File.Delete(directory + ".meta");
                }
            }
        }

        static bool IsEmptyFolders(string parentFolder) {
            foreach (var file in Directory.GetFiles(parentFolder)) {
                var info = new FileInfo(file);
                if (info.Extension != ".cs") continue;
                
                if (File.ReadLines(file).All(l => l.IsNullOrEmpty())) 
                    return true;
            }
            
            foreach (var directory in Directory.GetDirectories(parentFolder)) {
                if (IsEmptyFolders(directory))
                    return true;
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                    return true;
            }
            return false;
        }
    }
}