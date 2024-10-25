#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Yurowm.Core;
using Yurowm.Serialization;

namespace Yurowm.EditorCore {
        
    public class IncreaseBuildCode : IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report) {
            var settings = PropertyStorage.GetInstance<ProjectSettings>();
            
            if (settings.increaseBuildCode) {
                settings.buildCode++;
                
                PlayerSettings.Android.bundleVersionCode = settings.buildCode;
                PlayerSettings.iOS.buildNumber = settings.buildCode.ToString();
            }
            
            if (settings.autoVersionName) {
                var now = DateTime.UtcNow;
                PlayerSettings.bundleVersion = $"{now.Year}.{now.Month:00}{now.Day:00}.{settings.buildCode}";
            }
            
            settings.versionName = PlayerSettings.bundleVersion;
            
            settings.buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            
            PropertyStorage.Save(settings);
        }
    }
}

#endif
