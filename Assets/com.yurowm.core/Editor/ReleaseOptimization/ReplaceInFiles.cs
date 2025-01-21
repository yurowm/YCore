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
    public class ReplaceInFiles : Optimization {
        
        public List<string> namePattern = new();
        public List<Pattern> patterns = new();
        DirectoryInfo rootFolder;
        
        public override void OnInitialize() {
            rootFolder = new DirectoryInfo(Path.Combine(Application.dataPath));
        }

        public override bool DoAnalysis() {
            if (patterns.IsEmpty())
                return true;
            
            var pass = true;
            report = "";
            
            foreach (var file in GetFilesWithExtensions(rootFolder.FullName))
                if (Contains(file)) {
                    pass = false;
                    report += file.Name + "\n";
                }

            return pass;
        }

        public override void Fix() {
            if (patterns.IsEmpty())
                return;
            
            GetFilesWithExtensions(rootFolder.FullName).ForEach(Fix);
            
            AssetDatabase.Refresh();
        }

        IEnumerable<FileInfo> GetFiles(string path) {
            foreach (var file in Directory.GetFiles(path))
                yield return new FileInfo(file);

            foreach (var directory in Directory.GetDirectories(path))
            foreach (var info in GetFiles(directory))
                yield return info;
        }

        IEnumerable<FileInfo> GetFilesWithExtensions(string path) {
            var namePatterns = namePattern
                .Select(p => new Regex(p))
                .ToArray();
            foreach (var file in GetFiles(path))
                if (namePatterns.Any(r => r.IsMatch(file.FullName)))
                    yield return file;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }
        
        bool Contains(FileInfo file) {
            var originalText = File.ReadAllText(file.FullName);
            var text = originalText;
            
            foreach (var pattern in patterns)
                text = pattern.Fix(text);

            return text != originalText;
        }
        
        void Fix(FileInfo file) {
            var text = File.ReadAllText(file.FullName);

            foreach (var pattern in patterns)
                text = pattern.Fix(text);
                    
            File.WriteAllText(file.FullName, text);
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("extensions", namePattern.ToArray());
            writer.Write("patterns", patterns.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            namePattern.Reuse(reader.ReadCollection<string>("extensions"));
            patterns.Reuse(reader.ReadCollection<Pattern>("patterns"));
        }
        
        public class Pattern : ISerializable {
            public string original;
            public string replacement;
            
            public string Fix(string text) {
                return Regex.Replace(text, original, replacement);
            }
            
            public void Serialize(IWriter writer) {
                writer.Write("original", original);
                writer.Write("replacement", replacement);
            }

            public void Deserialize(IReader reader) {
                reader.Read("original", ref original);
                reader.Read("replacement", ref replacement);
            }
        }
    }
    
    public class ReplaceInFilesEditor : ObjectEditor<ReplaceInFiles> {
        public override void OnGUI(ReplaceInFiles optimization, object context = null) {
            EditStringList("Name pattern (regex)", optimization.namePattern);
            EditList("Patterns", optimization.patterns);
        }
    }
    public class ReplaceInFilesPatternEditor : ObjectEditor<ReplaceInFiles.Pattern> {
        public override void OnGUI(ReplaceInFiles.Pattern pattern, object context = null) {
            pattern.original = EditorGUILayout.TextField("Original (Regex)", pattern.original);
            pattern.replacement = EditorGUILayout.TextField("Replacement", pattern.replacement);
        }
    }
}