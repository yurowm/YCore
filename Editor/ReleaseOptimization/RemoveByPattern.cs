using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class RemoveByPattern : Optimization {
        
        public List<string> patterns = new List<string>();
        string[] _patterns;
        DirectoryInfo rootFolder;
        
        public override void OnInitialize() {
            rootFolder = new DirectoryInfo(Path.Combine(Application.dataPath));
        }

        public override bool DoAnalysis() {
            report = "";
            var pass = true;
            
            foreach (var info in ScanFolder(rootFolder.FullName)) {
                report += info.FullName + "\n";
                pass = false;
            }
            
            return pass;
        }

        IEnumerable<FileSystemInfo> ScanFolder(string path) {
            _patterns = patterns
                .Select(p => p.Replace('/', Path.DirectorySeparatorChar))
                .ToArray();
            
            if (_patterns.IsEmpty())
                yield break;
            
            foreach (var file in Directory.GetFiles(path)) {
                if (!Pass(file))
                    yield return new FileInfo(file);
            }
            
            foreach (var directory in Directory.GetDirectories(path)) {
                if (!Pass(directory)) {
                    yield return new DirectoryInfo(directory);
                    continue;
                }

                foreach (var info in ScanFolder(directory))
                    yield return info;
            }

        }
        
        bool Pass(string path) {
            return !_patterns.Any(path.Contains);
        }
        
        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            foreach (var info in ScanFolder(rootFolder.FullName)) {
                switch (info) {
                    case FileInfo file: file.Delete(); break;
                    case DirectoryInfo directory: directory.Delete(true); break;
                }
            }
            
            AssetDatabase.Refresh();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("patterns", patterns.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            patterns.Reuse(reader.ReadCollection<string>("patterns"));
        }
    }
    
    public class RemoveByPatternEditor : ObjectEditor<RemoveByPattern> {
        public override void OnGUI(RemoveByPattern optimization, object context = null) {
            EditStringList("Patterns", optimization.patterns);
        }
    }
}