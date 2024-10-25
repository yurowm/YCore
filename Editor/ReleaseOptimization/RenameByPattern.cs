using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class RenameByPattern : Optimization {
        
        public List<Pattern> patterns = new List<Pattern>();
        DirectoryInfo rootFolder;
        
        public override void OnInitialize() {
            rootFolder = new DirectoryInfo(Path.Combine(Application.dataPath));
        }

        public override bool DoAnalysis() {
            if (patterns.IsEmpty())
                return true;
            
            bool pass = true;
            report = "";
            
            foreach (var file in ScanFolder(rootFolder.FullName)) {
                var name = file.Name;
                patterns.ForEach(p => name = p.Rename(name));
                if (file.Name != name) {
                    report += file.Name + "\n";
                    pass = false;
                }
            }
            
            return pass;
        }

        IEnumerable<FileInfo> ScanFolder(string path) {
            foreach (var file in Directory.GetFiles(path))
                yield return new FileInfo(file);

            foreach (var directory in Directory.GetDirectories(path))
            foreach (var info in ScanFolder(directory))
                yield return info;

        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            if (patterns.IsEmpty())
                return;

            foreach (var file in ScanFolder(rootFolder.FullName)) {
                var name = file.Name;
                patterns.ForEach(p => name = p.Rename(name));
                if (file.Name != name) {
                    try { 
                        file.MoveTo(Path.Combine(file.Directory.FullName, name));
                    } catch (Exception e) {
                        Debug.LogException(e);
                        report = $"{file.FullName} file is failed to fix";
                        throw;
                    }
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
            patterns.Reuse(reader.ReadCollection<Pattern>("patterns"));
        }
        
        public abstract class Pattern : ISerializable {
            
            public abstract string Rename(string fileName);
            
            public virtual void Serialize(IWriter writer) { }

            public virtual void Deserialize(IReader reader) { }
        }
        
        public class ReplacePattern : Pattern {
            public string original;
            public string replacement;
            
            public override string Rename(string fileName) {
                return Regex.Replace(fileName, original, replacement);
            }

            public override void Serialize(IWriter writer) {
                base.Serialize(writer);
                writer.Write("original", original);
                writer.Write("replacement", replacement);
            }

            public override void Deserialize(IReader reader) {
                base.Deserialize(reader);
                reader.Read("original", ref original);
                reader.Read("replacement", ref replacement);
            }
        }
    }
    
    public class RenameByPatternEditor : ObjectEditor<RenameByPattern> {
        public override void OnGUI(RenameByPattern optimization, object context = null) {
            EditList("Patterns", optimization.patterns);
        }
    }
    
    public class ReplacePatternEditor : ObjectEditor<RenameByPattern.ReplacePattern> {
        public override void OnGUI(RenameByPattern.ReplacePattern pattern, object context = null) {
            pattern.original = EditorGUILayout.TextField("Original (Regex)", pattern.original);
            pattern.replacement = EditorGUILayout.TextField("Replacement", pattern.replacement);
        }
    }
}