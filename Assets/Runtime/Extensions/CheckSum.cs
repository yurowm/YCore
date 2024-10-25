using System.Collections;
using UnityEngine;

namespace Yurowm.Utilities {
    
    public class CheckSum : MonoBehaviour {
        public static long Calc(params object[] args) {
            return Calc(args.GetEnumerator());
        }
        
        public static long Calc(IEnumerator args) {
            if (args == null)
                return 0;
            
            const long m = 32768;
            const long a = 1103515245;
            const long b = 65536;
            const long c = 12345;

            long current;
            long result = c;
            while (args.MoveNext()) {
                if (args.Current != null) {
                    current = args.Current.GetHashCode();
                    if (current == 0) current = -1;

                    result = (a * result * current / b + c) % m;
                }
            }
            return result;
        }
    }
}