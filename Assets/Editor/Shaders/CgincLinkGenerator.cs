using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shaders {
    public class CgincLinkGenerator : AssetPostprocessor {
        static string packageCachePath;
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) {
            
            foreach (var asset in importedAssets) {
                if (asset.EndsWith(".shader")) {
                    GenerateIncludeFile().Forget();
                    return;
                }
            }
        }

        static readonly Regex includePattern =
            new(@"$\s*#include \""(?<file>[\w\d_]+)_link.cginc\""", RegexOptions.Multiline);

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnDidReloadScripts() {
            GenerateIncludeFile().Forget();
        }
        
        static async UniTask GenerateIncludeFile() {
            var assetsDir = new DirectoryInfo(Application.dataPath);
            packageCachePath = Path.Combine(assetsDir.Parent.FullName, "Library", "PackageCache");
            
            await UpdateSearchDirectories();
            
            var shaders = searchDirectories
                .SelectMany(d => d.GetFiles("*.shader", SearchOption.AllDirectories))
                .ToHashSet();

            var cgincFiles = searchDirectories
                .SelectMany(d => d.GetFiles("*.cginc", SearchOption.AllDirectories))
                .ToHashSet();

            var cglinkFiles = cgincFiles.Where(f => f.Name.EndsWith("_link.cginc")).ToHashSet();

            cgincFiles.ExceptWith(cglinkFiles);

            var newCglinkFiles = new Dictionary<FileInfo, StringBuilder>();

            foreach (var shader in shaders) {
                var code = File.ReadAllText(shader.FullName);

                foreach (Match match in includePattern.Matches(code)) {
                    var fileName = match.Groups["file"].Value;

                    var cgincFile = cgincFiles.FirstOrDefault(f => f.NameWithoutExtension().Equals(fileName));

                    if (cgincFile == null)
                        continue;

                    var linkFile = new FileInfo(Path.Combine(shader.Directory.FullName, fileName + "_link.cginc"));

                    if (!newCglinkFiles.TryGetValue(linkFile, out var linkCode)) {
                        linkCode = new StringBuilder();
                        newCglinkFiles.Add(linkFile, linkCode);
                    }

                    // Use relative path or package path for the include
                    var relativePath = GetPackageRelativePath(cgincFile.FullName);
                    if (relativePath == null)
                        relativePath = Path.GetRelativePath(linkFile.Directory.FullName, cgincFile.FullName);

                    linkCode.AppendLine($"#include \"{relativePath}\"");
                }
            }

            cglinkFiles.RemoveWhere(f => {
                if (!newCglinkFiles.ContainsKey(f)) {
                    f.Delete();
                    return true;
                }

                return false;
            });

            foreach (var pair in newCglinkFiles)
                File.WriteAllText(pair.Key.FullName, pair.Value.ToString());

            AssetDatabase.Refresh();
        }

        static List<DirectoryInfo> searchDirectories;
        
        public static async UniTask UpdateSearchDirectories() {
            var result = new List<DirectoryInfo>();
            result.Add(new DirectoryInfo(Application.dataPath));

            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted) 
                await UniTask.Yield();

            if (listRequest.Status == StatusCode.Success) {
                foreach (var package in listRequest.Result) {
                    switch (package.source) {
                        case PackageSource.Embedded:
                        case PackageSource.Registry:
                        case PackageSource.Local:
                        case PackageSource.Git:
                            break;
                        default: continue;
                    }
                    
                    var packagePath = package.resolvedPath;
                    if (!packagePath.IsNullOrEmpty()) 
                        result.Add(new DirectoryInfo(packagePath));
                }
            } else
                Debug.LogError($"Failed to list packages: {listRequest.Error.message}");
            
            searchDirectories = result;
        }

        static string GetPackageRelativePath(string fullPath) {
            if (fullPath.StartsWith(packageCachePath))
                return Path.Combine("Packages", fullPath.Substring(packageCachePath.Length + 1));
            return null;
        }
    }
}