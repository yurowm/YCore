using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shaders {
    public class CgincLinkGenerator : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) {
            foreach (var asset in importedAssets) {
                if (asset.EndsWith(".shader")) {
                    GenerateIncludeFile();
                    return;
                }
            }
        }

        static readonly Regex includePattern =
            new(@"$\s*#include \""(?<file>[\w\d_]+)_link.cginc\""", RegexOptions.Multiline);

        [UnityEditor.Callbacks.DidReloadScripts]
        static void GenerateIncludeFile() {
            var shaders = GetRootDirectories()
                .SelectMany(d => d.GetFiles("*.shader", SearchOption.AllDirectories))
                .ToHashSet();

            var cgincFiles = GetRootDirectories()
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

                    linkCode.AppendLine(
                        $"#include \"{Path.GetRelativePath(linkFile.Directory.FullName, cgincFile.FullName)}\"");
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

        static IEnumerable<DirectoryInfo> GetRootDirectories() {
            var assetDirectory = new DirectoryInfo(Application.dataPath);
            yield return assetDirectory;
            yield return new DirectoryInfo(Path.Combine(assetDirectory.Parent?.FullName, "Library", "PackageCache"));
        }
    }
}