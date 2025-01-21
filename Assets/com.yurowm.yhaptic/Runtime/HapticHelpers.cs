using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public static class HapticHelpers {
        public static DirectoryInfo GetRootFolder() => 
            new(Path.Combine(TextData.GetPath(TextCatalog.StreamingAssets), SoundController.RootFolderName));

        public static IEnumerable<string> GetAllHapticPaths() => 
            GetAllPaths(false, ".ahap");
        
        public static IEnumerable<string> GetAllPaths(bool withExtension, params string[] extensions) {
            IEnumerable<FileInfo> GetAllFiles(DirectoryInfo directory) {
                foreach (var file in directory.EnumerateFiles()) {
                    yield return file;
                }    
                
                foreach (var dir in directory.EnumerateDirectories()) 
                foreach (var file in GetAllFiles(dir))
                    yield return file;
            }
            
            var rootDirectory = GetRootFolder();
            
            var trimSize = rootDirectory.FullName.Length + 1;
            
            foreach (var file in GetAllFiles(rootDirectory))
                if (extensions.Contains(file.Extension)) {
                    var path = file.FullName;
                    path = path.Substring(trimSize);
                    if (!withExtension)
                        path = path.Substring(0, path.Length - file.Extension.Length);
                    yield return path.Replace('\\', '/');
                }
        }
    }
}
