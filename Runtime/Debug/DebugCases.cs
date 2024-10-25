using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public static class DebugCases {
        static Dictionary<object, List<string>> cases = new Dictionary<object, List<string>>();
        
        static bool initialized = false;
        
        static void Initialize() {
            initialized = true;
            DebugPanel.Log("Log All Cases", () => cases.Keys.ForEach(Log));
            DebugPanel.Log("PanelLog All Cases", () => cases.Keys.ForEach(PanelLog));
        }

        public static void NewCase(object keyObject) {
            if (!initialized) 
                Initialize();
            
            if (cases.TryGetValue(keyObject, out var c))
                c.Clear();
            else
                cases[keyObject] = new List<string>();
            
            Milestone(keyObject, "Start");
        }

        public static void RemoveCase(object keyObject) {
            cases.Remove(keyObject);
        }

        public static int Milestone(object keyObject, string value) {
            var c = cases[keyObject];
            
            c.Add(value);
            
            return c.Count;
        } 
        
        public static void Milestone(object keyObject, string value, int milestone) {
            var c = cases[keyObject];

            if (c.Count >= milestone)
                c.RemoveRange(milestone, c.Count - milestone);
            
            c.Add(value);
        }

        public static string Release(object keyObject) {
            var c = cases[keyObject];
            int lineNumber = 0;
            return c.Select(l => $"{++lineNumber}. {l}").Join("\n");
        }
        
        public static void Log(object keyObject) {
            Debug.Log(Release(keyObject));
        } 
        
        public static void PanelLog(object keyObject) {
            DebugPanel.Log($"case #{keyObject.GetHashCode()}", "DebugCases", Release(keyObject));
        }
    }
}
