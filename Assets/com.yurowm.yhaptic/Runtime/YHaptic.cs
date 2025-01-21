using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Core;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.UI;

namespace Yurowm.Integrations {
    public class YHaptic: Integration {
        public override string GetName() => "YHaptic";
        
        public IYHapticProvider platform;

        public override Issue GetIssues() {
            var result = base.GetIssues();
            #if UNITY_IOS || UNITY_ANDROID
            return result;
            #else
            return result | Issue.Platform;
            #endif
        }

        protected override async UniTask Initialize() {
            base.Initialize();
            
            #if UNITY_IOS
            platform = new YHapticIOS();
            #elif UNITY_ANDROID
            platform = new YHapticAndroid();
            #endif
            
            if (platform != null)
                await platform.Initialize();
            
        }
        
        public bool IsSupported() {
            #if UNITY_EDITOR
            return false;
            #else
            return platform != null && platform.IsSupported();
            #endif
        }

        public bool IsEnabled() {
            if (!IsSupported()) return false;
            
            return GameSettings.Instance.GetModule<HapticSettings>().enabled;
        }

        public IYHapticProvider GetActiveProvider() {
            if (IsEnabled())
                return platform;
            return null;
        }
        
        [ReferenceValue("HapticSupport")]
        static int SupportsVibration() => Get<YHaptic>()?.IsSupported() ?? false ? 1 : 0;
    }
    
    public interface IYHapticProvider {
        UniTask Initialize();
        bool IsSupported();
    }
}