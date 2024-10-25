using System;
using System.Collections.Generic;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    public static class InputLock {
        static List<string> keys = new List<string>();
        static bool full = false;

        public static void AddPassKey(string key) {
            if (!keys.Contains(key)) {
                keys.Add(key);
                DebugIt();
            }
        }
        
        public static void LockEverything() {
            keys.Clear();
            full = true;
            DebugIt();
        }
        
        public static void Unlock() {
            keys.Clear();
            full = false;
            DebugIt();
        }
        
        public static void Unlock(string key) {
            keys.Remove(key);
            DebugIt();
        }
        
        public static bool GetAccess(string key) {
            if (full) return false;
            if (keys.Count == 0) return true;
            return keys.Contains(key);
        }
        
        public static IDisposable Lock(string key) {
            AddPassKey(key);
            
            return new Locker(key);
        }
        
        public static IDisposable Lock() => Lock(YRandom.main.GenerateKey(6));

        static void DebugIt() {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            DebugTools.DebugPanel.Log("Input Lock", "UI", full ? "FULL" : keys.Join(", "));
            #endif
        }
        
        struct Locker: IDisposable {
            public string key;

            public Locker(string key) {
                this.key = key;
            }

            public void Dispose() {
                Unlock(key);
            }
        }
    }
}