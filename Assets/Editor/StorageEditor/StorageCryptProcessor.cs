using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm.EditorCore {
        
    public class StorageCryptProcessor: IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject {
        
        public int callbackOrder { get; }
        
        public void OnPostGenerateGradleAndroidProject(string path) {
            if (GetDirectory(BuildTarget.Android, path, out var directory)) 
                PostProcess(directory);
        }

        public void OnPostprocessBuild(BuildReport report) {
            if (report.summary.platform != BuildTarget.Android)
                if (GetDirectory(report.summary.platform, report.summary.outputPath, out var directory))
                    PostProcess(directory);
        }

        static void PostProcess(DirectoryInfo directory) {
            Clear(directory);
            foreach (var file in GetSourceFiles()) 
                Save(file, source => source.Encrypt(), directory);
        }
        
        static IEnumerable<FileInfo> GetSourceFiles() {
            var directoryInfo = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, "Data"));

            if (!directoryInfo.Exists) 
                yield break;
            
            foreach (var fileInfo in directoryInfo.GetFiles())
                if (fileInfo.Extension == ".json")
                    yield return fileInfo;
        }
        
        static bool GetDirectory(BuildTarget target, string rootPath, out DirectoryInfo directory) {
            directory = null;
  
            switch (target) {
                case BuildTarget.iOS: 
                    directory = new DirectoryInfo(Path.Combine(rootPath, "Data/Raw/Data"));
                    break;
                case BuildTarget.Android:
                    directory = new DirectoryInfo(Path.Combine(rootPath, "src/main/assets/Data/"));
                    break;
                case BuildTarget.WebGL:
                    directory = new DirectoryInfo(Path.Combine(rootPath, "StreamingAssets/Data"));
                    break;
            }
            
            return directory != null && directory.Exists;
        }
        
        static void Clear(DirectoryInfo directory) {
            if (directory == null || !directory.Exists)
                return;
            
            foreach (var file in directory.GetFiles()) 
                file.Delete();
            
            foreach (var subdir in directory.GetDirectories()) 
                subdir.Delete(true);
        }
        
        static void Save(FileInfo file, Func<string, string> modification, DirectoryInfo directory) {
            if (!file.Exists || directory == null || !directory.Exists) 
                return;
            
            var text = File.ReadAllText(file.FullName);
            
            text = modification?.Invoke(text) ?? text;
            
            File.WriteAllText(Path.Combine(directory.FullName, file.Name), text);
        }
    }
}
