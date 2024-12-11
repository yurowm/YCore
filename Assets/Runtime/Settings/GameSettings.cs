using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.ComposedPages;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.InUnityReporting;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {
    public class GameSettings : ISerializable {
        
        [OnLaunch(int.MinValue)]
        static async UniTask OnLaunch() {
            if (!OnceAccess.GetAccess("GameSettings")) 
                return;
            
            if (Instance == null)
                await LoadWithDefaultCryptKey();
                
            Instance.Initialize();
            onLoaded?.Invoke(Instance);
        }
        
        CryptKey KEY;
        
        #region Modules

        List<SettingsModule> modules = new();
        
        public M GetModule<M>() where M : SettingsModule {
            M result = modules.CastOne<M>();
            if (result == null) {
                try {
                    result = Activator.CreateInstance<M>();
                    result.setDirty = SetDirty;
                    result.Initialize();
                    modules.Add(result);
                } catch (Exception e) {
                    Debug.LogException(e);
                    return null;
                }
            }
        
            return result;
        }
        
        #endregion
        
        #region Dictionary

        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        
        public string Get(string key) {
            if (key.IsNullOrEmpty()) return null;
            return dictionary.Get(key);
        }
        
        public void Set(string key, string value) {
            dictionary.Set(key, value);
            isDirty = true;
        }

        #endregion
        
        public static Action<GameSettings> onLoaded = delegate{};
        
        public void Initialize() {
            modules.ForEach(m => m.Initialize());
            DirtyCheck().Forget();
            Reporter.AddReport("Game Settings", new SerializableReport(this));
        }
        
        public static void ExecuteOnLoad(Action<GameSettings> action) {
            if (Instance != null)
                action?.Invoke(Instance);
            else 
                onLoaded += action;
        }

        #region Save & Load
        
        bool isDirty = false;
        
        public void SetDirty() {
            isDirty = true;
        }

        async UniTask DirtyCheck() {
            int count = 0;
            while (true) {
                if (isDirty) {
                    Save();
                    DebugPanel.Log("Save", "Services", count++);
                    isDirty = false;
                }
                await UniTask.Yield();
            }
        }

        public void Save() {
            string raw = Serializator.ToTextData(this);
            if (KEY != null)
                raw = raw.Encrypt(KEY);
            
            TextData.SaveText(
                Path.Combine("Data", "GameSettings" + Serializer.FileExtension),
                raw,
                TextCatalog.Persistent);
        }
        
        public static GameSettings Instance {
            get;
            private set;
        }
        
        public static UniTask LoadWithDefaultCryptKey() {
            return Load("ehO2MCO0t9ZH4J5Z5Fj3");
        }
        
        public static async UniTask Load(string cryptKey = null) {
            if (Instance != null) 
                return;

            Instance = new GameSettings();
            
            CryptKey key = null;
            
            if (cryptKey.IsNullOrEmpty())
                Debug.LogError("You are trying to load settings without cryption key");
            else
                key = CryptKey.Get(cryptKey);
            
            var raw = await TextData.LoadTextRoutine(
                Path.Combine("Data", "GameSettings" + Serializer.FileExtension),
                TextCatalog.Persistent);

            if (!raw.IsNullOrEmpty()) {
                if (key != null)
                    raw = raw.Decrypt(key);
                
                Serializator.FromTextData(Instance, raw);
            }
            
            Instance.KEY = key;
        }
        
        public void Clear() {
            dictionary.Clear();
            modules.Clear();
            SetDirty();
        }   
        
        #endregion
        
        #region ISerializable
        
        public void Serialize(IWriter writer) {
            writer.Write("modules", modules.ToArray());
            writer.Write("dictionary", dictionary);
        }

        public void Deserialize(IReader reader) {
            modules.Clear();
            modules.AddRange(reader.ReadCollection<SettingsModule>("modules"));
            modules.ForEach(m => m.setDirty = SetDirty);
            dictionary = reader.ReadDictionary<string>("dictionary").ToDictionary();
        }
        
        #endregion
    }
    
    public abstract class SettingsModule : ISerializable {
        public Action setDirty;
        
        public void SetDirty() {
            setDirty.Invoke();
            DebugPanel.Log("SetDirty request", "Services", $"{GetType().Name}: {DateTime.Now}");
        }
        
        public virtual void Initialize() {}
    
        public abstract void Serialize(IWriter writer);

        public abstract void Deserialize(IReader reader);
    }
    
    public class DebugSettings: SettingsModule {
        bool debugPanel = false;
        
        public bool DebugPanel {
            get => debugPanel;
            set {
                if (debugPanel != value) {
                    debugPanel = value;
                    SetDirty();
                }
                if (DebugPanelUI.Instance) 
                    DebugPanelUI.Instance.MakeVisible(debugPanel);
            }
        }
        
        public override void Serialize(IWriter writer) {
            writer.Write("debugPanel", debugPanel);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("debugPanel", ref debugPanel);
        }
    }
}