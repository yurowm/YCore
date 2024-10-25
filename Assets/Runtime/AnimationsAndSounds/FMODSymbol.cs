using System.Collections.Generic;
using Yurowm.Utilities;

namespace Yurowm {
    public class FMODSymbol : ScriptingDefineSymbolAuto {
        public override string GetSybmol() {
            return "FMOD";
        }

        public override IEnumerable<string> GetRequiredNamespaces() {
            yield return "FMOD";
        }
    }
}