using System.Text.RegularExpressions;
using UnityEngine;
using Yurowm.DebugTools;
using Yurowm.Integrations;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm.Quality {
    public static class YQuality {
        
        [OnLaunch]
        static void Setup() {
            #if UNITY_IOS
            SetupIOS();
            #endif
            #if UNITY_ANDROID
            SetupAndroid();
            #endif
            
            DebugPanel.Log("RAM Memory Size", "System", SystemInfo.systemMemorySize);
            DebugPanel.Log("CPU Count", "System", SystemInfo.processorCount);
            DebugPanel.Log("GPU Memory Size", "System", SystemInfo.graphicsMemorySize);
        }
        
        static void SetupIOS() {
            var cpu = AppleSpecification.GetCPU();

            switch (cpu) {
                case { family: "A", number: >= 11 }:
                case { family: "M" }:
                    QualitySettings.antiAliasing = 4; // 4x MSAA
                    break;
                default:
                    QualitySettings.antiAliasing = 0; // No AA
                    break;
            }
            
            Application.targetFrameRate = 60;
        }
        
        static void SetupAndroid() {
            QualitySettings.antiAliasing = 0; // No AA
            
            Application.targetFrameRate = 60;
        }
    }
    
    public static class AppleSpecification {
        static readonly Regex cpuParser = new (@"Apple (?<family>\w)(?<number>\d+)[^\S\r\n]*(?<version>\w+)?");
        
        public struct CPU {
            public string family;
            public int number;
            public string version;
        }
        
        public static CPU GetCPU() {
            var match = cpuParser.Match(SystemInfo.graphicsDeviceName);
            if (!match.Success)
                return default;
            
            var result = new CPU();
            
            result.family = match.Groups["family"].Value;
            result.number = int.TryParse(match.Groups["number"].Value, out var n) ? n : -1;
            result.version = match.Groups["version"]?.Value;
            
            return result;
        }

    }
}