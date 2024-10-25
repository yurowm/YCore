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
    public class RemoveThirdPartyContent : Optimization {

        FileInfo packagesFile;
        
        public List<string> valid = new List<string>();
        public List<string> invalid = new List<string>();
        
        Regex pareser = new Regex(@"""(?<key>[^""]+)"":\s*""(?<value>[^""]+)""");
        
        public override void OnInitialize() {
            packagesFile = new FileInfo(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json"));
        }
        
        public override bool DoAnalysis() {
            var raw = File.ReadAllText(packagesFile.FullName);
           
            var pass = true;

            foreach (Match match in pareser.Matches(raw)) {
                string key = match.Groups["key"].Value;
                if (!Pass(key)) {
                    pass = false;
                    report += key + "\n";
                }
            }
            
            return pass;
        }

        bool Pass(string key) {
            if (!invalid.IsEmpty() && invalid.Any(key.Contains))
                return false;
            if (valid.IsEmpty() || valid.Any(key.Contains))
                return true;
            return false;
        }
        
        public override bool CanBeAutomaticallyFixed() {
            return true;
        }

        public override void Fix() {
            #region Load Packages

            string raw = File.ReadAllText(packagesFile.FullName);
            
            Dictionary<string, string> packagesToKeep = new Dictionary<string, string>();
            int startIndex = int.MaxValue;
            int endIndex = int.MinValue;
            
            foreach (Match match in pareser.Matches(raw)) {
                startIndex = Mathf.Min(startIndex, match.Index);
                endIndex = Mathf.Max(endIndex, match.Index + match.Length);
                
                var key = match.Groups["key"].Value;
                
                if (Pass(key))
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
        
            AssetDatabase.Refresh();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("valid", valid.ToArray());
            writer.Write("invalid", invalid.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            valid.Reuse(reader.ReadCollection<string>("valid"));
            invalid.Reuse(reader.ReadCollection<string>("invalid"));
        }
    }
    
    public class RemoveThirdPartyContentEditor : ObjectEditor<RemoveThirdPartyContent> {
        public override void OnGUI(RemoveThirdPartyContent optimization, object context = null) {
            EditStringList("Patterns Valid", optimization.valid);
            EditStringList("Patterns Invalid", optimization.invalid);
        }
    }
}