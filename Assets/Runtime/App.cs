using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;
using DeviceType = UnityEngine.DeviceType;

namespace Yurowm.Core {
    public static class App {
        
        public static Action onFirstLaunch = delegate {};
        public static Action onLaunch = delegate {};
        public static Action onQuit = delegate {};
        public static Action onFocus = delegate {};
        public static Action<bool> onFocusState = delegate {};
        public static Action onUnfocus = delegate {};
        
        static GameData _data;
        public static GameData data {
            get => _data;
            private set => _data = value;
        }
        
        static AppBehaviour appBehaviour;

        [OnLaunch(int.MinValue)]
        static async UniTask StartLaunch() {
            if (!OnceAccess.GetAccess("App_Min")) 
                return;
            
            #if UNITY_EDITOR
            data = new GameData("PlayerEditor");
            #elif DEVELOPMENT_BUILD
                data = new GameData("PlayerDebug", "NixM20TSkARg1ax");
            #else
                data = new GameData("Player", "NixM20TSkARg1ax");
            #endif
            
            await data.Load();
        }

        [OnLaunch(int.MinValue)]
        static void EndLaunch() {
            if (!OnceAccess.GetAccess("App_Max")) 
                return;
            
            var appData = data.GetModule<Data>();
                
            if (appData.LaunchCount == 0)
                onFirstLaunch.Invoke();
                
            appData.OnLaunch();
            onLaunch.Invoke();
            onFocus.Invoke();
                
            appBehaviour = new GameObject("App").AddComponent<AppBehaviour>();
            appBehaviour.gameObject.hideFlags = HideFlags.HideAndDontSave;
            
            UILogic().Forget();
        }

        public static bool IsFirstAppLaunch() {
            var appData = data.GetModule<Data>();

            return appData.LaunchCount == 0;
        }

        #region UI
        
        public static Action onScreenResize = delegate {};
        public static SingleCallEvent onGetReady = new();
        
        static RectOffsetFloat? _safeOffset;

        public static RectOffsetFloat safeOffset {
            get {
                if (_safeOffset.HasValue)
                    return _safeOffset.Value;
                
                _safeOffset = RectOffsetFloat.Delta(
                    new Rect(default, new Vector2(Screen.width, Screen.height)), 
                    Screen.safeArea);
                
                return _safeOffset.Value;
            }
        }
        
        public static bool isTablet => IsTablet();

        static async UniTask UILogic() {
            Vector2 screenSize = default;
            ScreenOrientation screenOrientation = default;
            
            while (true) {
                if (screenSize.x != Screen.width 
                    || screenSize.y != Screen.height 
                    || screenOrientation != Screen.orientation) {
                    
                    screenSize.x = Screen.width;
                    screenSize.y = Screen.height;
                    screenOrientation = Screen.orientation;

                    _safeOffset = null;
                    _safeOffset = safeOffset;
                    
                    DebugPanel.Log("Safe Area Offset", "UI", safeOffset);
                    DebugPanel.Log("Orientation", "UI", screenOrientation);
                    DebugPanel.Log("Resolution", "UI", screenSize);

                    onScreenResize.Invoke();
                }
                
                await UniTask.Yield();
            }
        }
        
        static bool IsTablet() {
            var deviceModel = SystemInfo.deviceModel;
            
            if (!deviceModel.IsNullOrEmpty()) {
                deviceModel = deviceModel.ToLower();
                if (deviceModel.Contains("ipad")) return true;
                if (deviceModel.Contains("iphone")) return false;
            }

            var resolution = new Vector2(Screen.width, Screen.height);
            
            if (resolution.x <= 0 || resolution.y <= 0)
                return false;
            
            if (Application.isMobilePlatform) {
                var diagonal = resolution.FastMagnitude() / Screen.dpi;
                if (diagonal >= 9f) 
                    return true;
            }
            
            if (Application.isEditor) {
                var aspectRatio = 1f * resolution.x / resolution.y;
                if (aspectRatio < 1) aspectRatio = 1f / aspectRatio;
                return aspectRatio.Round(2) < (16f / 9f).Round(2);
            }
            
            return false;
        }
        
        #endregion

        public class Data : GameData.Module, IServerDataModule {
            public int LaunchCount {get; private set;}
            public DateTime FirstLaunchTime {get; private set;}
            
            public void OnLaunch() {
                if (LaunchCount == 0 || FirstLaunchTime == default)
                    FirstLaunchTime = DateTime.Now;
                LaunchCount++;
                SetDirty();
            }

            public override void Serialize(IWriter writer) {
                writer.Write("launchCount", LaunchCount);
                writer.Write("firstLaunchTime", FirstLaunchTime);
            }

            public override void Deserialize(IReader reader) {
                LaunchCount = reader.Read<int>("launchCount");
                FirstLaunchTime = reader.Read<DateTime>("firstLaunchTime");
            }
        }
        
        class AppBehaviour : MonoBehaviour {
            void OnApplicationFocus(bool hasFocus) {
                onFocusState?.Invoke(hasFocus);
                if (hasFocus)
                    onFocus.Invoke();
                else
                    onUnfocus.Invoke();
            }

            void OnApplicationQuit() {
                onQuit.Invoke();    
            }
        }

        #region Go to external
        
        public static void OpenAppStorePage() {            
            if (Application.isEditor) { 
                Application.OpenURL("https://google.com");
                return;
            }
            
            switch (Application.platform) {
                case RuntimePlatform.Android: 
                    Application.OpenURL($"market://details?id={Application.identifier}");
                    return;
                case RuntimePlatform.IPhonePlayer:
                    #if UNITY_IOS
                    UnityEngine.iOS.Device.RequestStoreReview();
                    #endif
                    return;
            }
        }
    
        public static void OpenHelpPopup() {
            if (Application.isEditor) {
                Application.OpenURL("https://gmail.com");
                return;   
            }

            switch (Application.platform) {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    var email = GameParameters.GetModule<GameParametersGeneral>().supportEmail;
                    if (email.IsNullOrEmpty())
                        return;
                    
                    var subject = $"{Application.productName} Support";
                    
                    var body = $"Bundle ID: {Application.identifier}\n" 
                               + $"Version: {Application.version}\n" 
                               + $"Platform: {Application.platform}";
                    
                    Application.OpenURL($"mailto:{email}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}");
                    
                    return;
            }
        }

        #endregion

        public static async UniTask LoadingShow() {
            var page = Page.GetCurrent();
            if (page != null && page.HasTag("Loading")) 
                return;
            
            page = Page.Get("Loading");
            if (page != null) {
                await page.ShowAndWait();
                page.Clean();
            }
        }
    }
}