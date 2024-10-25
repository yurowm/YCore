using System.Collections.Generic;
using Yurowm.Utilities;

namespace Yurowm {
    public class mXParserSymbol : ScriptingDefineSymbolAuto {
        public override string GetSybmol() {
            return "MXPARSER";
        }

        public override IEnumerable<string> GetRequiredNamespaces() {
            yield return "org.mariuszgromada.math.mxparser";
        }
    }
}