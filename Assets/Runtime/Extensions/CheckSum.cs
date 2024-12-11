using System.Collections;
using UnityEngine;

namespace Yurowm.Utilities {
    public class CheckSum {
        public static long Calc(params object[] args) {
            return Calc(args);
        }
        
        public static long Calc(IEnumerable args) {
            if (args == null)
                return 0;
            
            const long m = 32768;
            const long a = 1103515245;
            const long b = 65536;
            const long c = 12345;

            long current;
            long result = c;

            foreach (var arg in args) {
                current = args.GetHashCode();
                if (current == 0) current = -1;

                result = (a * result * current / b + c) % m;
            }
            
            return result;
        }
    }
}