using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class RemoveByPath : Optimization {
        
        public List<string> folders = new List<string>();
        public List<string> files = new List<string>();

        IEnumerable<DirectoryInfo> GetDirectories() {
            foreach (var folder in folders)
                yield return new DirectoryInfo(Path.Combine(Application.dataPath, folder));
        }
        
        IEnumerable<FileInfo> GetFiles() {
            foreach (var file in files)
                yield return new FileInfo(Path.Combine(Application.dataPath, file));
        }
        
        public override bool DoAnalysis() {
            report = "";
            var pass = true;
            
            foreach (var dir in GetDirectories().Where(d => d.Exists)) {
                report += dir.FullName + "\n";
                pass = false;
            }
            
            foreach (var file in GetFiles().Where(d => d.Exists)) {
                report += file.FullName + "\n";
                pass = false;
            }
            
            return pass;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            GetFiles().ForEach(f => f.Delete());
            GetDirectories()
                .Where(d => d.Exists)
                .ForEach(d => d.Delete(true));
        }
        
        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("folders", folders.ToArray());
            writer.Write("files", files.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            folders.Reuse(reader.ReadCollection<string>("folders"));
            files.Reuse(reader.ReadCollection<string>("files"));
        }
    }
    
    public class RemoveByPathEditor : ObjectEditor<RemoveByPath> {
        
        public override void OnGUI(RemoveByPath optimization, object context = null) {
            EditStringList("Files", optimization.files);
            EditStringList("Folders", optimization.folders);
        }
    }
}