using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Yurowm.Utilities {
    public abstract class ScriptingDefineSymbolBase {
        
        public ScriptingDefineSymbolBase() {}
        
        public abstract bool GetState();
        public abstract void SetState(bool state);
        
        public abstract string GetSybmol();
    }
    
    public abstract class ScriptingDefineSymbol : ScriptingDefineSymbolBase {
        public bool enable = false;
        
        public ScriptingDefineSymbol() {}
        
        public override bool GetState() {
            return enable;
        }
        public override void SetState(bool state) {
            enable = state;
        }
    }
    
    public abstract class ScriptingDefineSymbolAuto : ScriptingDefineSymbolBase {
        public bool enable = false;
        
        public virtual IEnumerable<string> GetRequiredPackageIDs() {
            yield break;
        }
        
        public virtual IEnumerable<string> GetRequiredNamespaces() {
            yield break;
        }
        
        public virtual IEnumerable<string> GetRequiredClasses() {
            yield break;
        }
        
        public virtual IEnumerable<Platform> GetSupportedPlatforms() {
            yield return GetCurrentPlatform();
        }
        
        static Platform GetCurrentPlatform() {
            
            #if UNITY_EDITOR
            
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget) {
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                case UnityEditor.BuildTarget.WSAPlayer:
                    return Platform.Windows;
                case UnityEditor.BuildTarget.StandaloneOSX:
                    return Platform.OSX;
                case UnityEditor.BuildTarget.iOS:
                    return Platform.iOS;
                case UnityEditor.BuildTarget.Android:
                    return Platform.Android;
                case UnityEditor.BuildTarget.WebGL:
                    return Platform.Web;
                case UnityEditor.BuildTarget.StandaloneLinux64:
                    return Platform.Linux;
                case UnityEditor.BuildTarget.PS4:
                    return Platform.PS4;
                case UnityEditor.BuildTarget.XboxOne:
                    return Platform.XboxOne;
                case UnityEditor.BuildTarget.tvOS:
                    return Platform.tvOS;
                case UnityEditor.BuildTarget.Switch:
                    return Platform.Switch;
                case UnityEditor.BuildTarget.Lumin:
                    return Platform.Lumin;
                case UnityEditor.BuildTarget.Stadia:
                    return Platform.Stadia;
            }
            
            #endif
            
            return Platform.Unknown;
        }
        
        public enum Platform {
            Unknown,
            
            Windows,
            OSX,
            Linux,
            
            Android,
            iOS,
            tvOS,
            
            PS4,
            XboxOne,
            Switch,
            Stadia,
            
            CloudRendering,
            Lumin,
            Web
        }
        
        #if UNITY_EDITOR
        string[] missiedPackages = null;
        string[] missiedNamespaces = null;
        string[] missiedClasses = null;
        
        DelayedAccess updateAccess = new DelayedAccess(10);
        
        static string[] allPackages = null;
        static string[] allNamespaces = null;
        static ScriptingDefineSymbolAuto() {
            OnRecompile();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnRecompile() {
            var listRequest = UnityEditor.PackageManager.Client.List(true, true);
            while (!listRequest.IsCompleted)
                Thread.Sleep(10);
            allPackages = listRequest.Result.Select(p => p.name).ToArray();
            allNamespaces = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Select(t => t.Namespace)
                .Distinct()
                .ToArray();
        }
        #endif
        
        public bool IsEnabled() {
            #if UNITY_EDITOR
            if (updateAccess.GetAccess()) {
                missiedNamespaces = null;
                missiedClasses = null;
                missiedPackages = null;
            }
            
            var currentPlatform = GetCurrentPlatform();
            if (GetSupportedPlatforms().All(p => p != currentPlatform))
                return false;

            if (missiedPackages == null)
                missiedPackages = GetRequiredPackageIDs()
                    .Where(id => !allPackages.Contains(id))
                    .ToArray();
            
            if (missiedPackages.Length > 0)
                return false;
            
            if (missiedNamespaces == null)
                missiedNamespaces = GetRequiredNamespaces()
                    .Where(n => !allNamespaces.Contains(n))
                    .ToArray();
            
            if (missiedNamespaces.Length > 0)
                return false;
            
            if (missiedClasses == null) {
                missiedClasses = GetRequiredClasses()
                    .Where(n => UnityUtils.FindType(n) == null)
                    .ToArray();
            }
            
            if (missiedClasses.Length > 0)
                return false;
            
            return true;
            #else
            return false;
            #endif
            
        }
        
        public override bool GetState() {
            return enable;
        }
        public override void SetState(bool state) {
            enable = state;
        }
    }
}