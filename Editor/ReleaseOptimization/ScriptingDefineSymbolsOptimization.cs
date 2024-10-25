using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Yurowm.EditorCore;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    public class ScriptingDefineSymbolsOptimization : Optimization {
        public List<string> add = new List<string>();
        public List<string> remove = new List<string>();
        
        public override bool DoAnalysis() {
            report = "";
            var passed = true;
            
            var symbols = GetSymbols().ToArray();
            
            foreach (var symbol in add.Select(Normalize))
                if (!symbols.Contains(symbol)) {
                    passed = false;
                    report += $"+ {symbol}\n";
                }
            
            foreach (var symbol in remove.Select(Normalize))
                if (symbols.Contains(symbol)) {
                    passed = false;
                    report += $"- {symbol}\n";
                }
            
            report = report.Trim();
            
            return passed;
        }

        public override bool CanBeAutomaticallyFixed() {
            return true;
        }
        
        string Normalize(string symbol) {
            return symbol.Trim().ToUpperInvariant();
        }
        
        IEnumerable<string> GetSymbols() {
            return ScriptingDefineSymbolsEditor
                .GetRawSymbols(EditorUserBuildSettings.selectedBuildTargetGroup);
        }
        
        void SetSymbols(IEnumerable<string> symbols) {
            ScriptingDefineSymbolsEditor
                .SetSymbols(symbols, EditorUserBuildSettings.selectedBuildTargetGroup);
        }

        public override void Fix() {
            base.Fix();
            
            var symbols = GetSymbols().ToList();

            foreach (var symbol in add.Select(Normalize))
                if (!symbols.Contains(symbol))
                    symbols.Add(symbol);
            
            foreach (var symbol in remove.Select(Normalize))
                symbols.Remove(symbol);
            
            SetSymbols(symbols);
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("add", add.ToArray());
            writer.Write("remove", remove.ToArray());
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            add.Reuse(reader.ReadCollection<string>("add"));
            remove.Reuse(reader.ReadCollection<string>("remove"));
        }
    }
    
    public class ScriptingDefineSymbolsOptimizationEditor : ObjectEditor<ScriptingDefineSymbolsOptimization> {
        
        public override void OnGUI(ScriptingDefineSymbolsOptimization optimization, object context = null) {
            EditStringList("Add", optimization.add);
            EditStringList("Remove", optimization.remove);
        }
    }
}