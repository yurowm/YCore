using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.InUnityReporting;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {
    public class GameData : ISerializable {

        CryptKey Key;

        #region Modules

        List<Module> modules = new List<Module>();
        
        public IEnumerable<Module> GetModules() {
            foreach (var module in modules)
                yield return module;
        }

        public M GetModule<M>() where M : Module {
            return modules.CastOne<M>() ?? CreateModule<M>();
        }
        
        public bool CreateModule<M>(out M module) where M : Module {
            module = CreateModule<M>();
            return module != null;
        }
        
        public M CreateModule<M>() where M : Module {
            var result = modules.CastOne<M>();
            
            if (result != null)
                return null;

            try {
                result = Activator.CreateInstance<M>();
                result.setDirty = SetDirty;
                result.Initialize();
                modules.Add(result);
            } catch (Exception e) {
                Debug.LogException(e);
                result = null;
            }
            
            return result;
        }
        
        public abstract class Module : ISerializable {
            public Action setDirty;
        
            public void SetDirty() {
                setDirty.Invoke();    
            }
        
            public virtual void Initialize() {}
    
            public virtual string GetKey() => GetType().Name.ToLowerInvariant();
                
            public abstract void Serialize(IWriter writer);

            public abstract void Deserialize(IReader reader);
        }
        
        #endregion
        
        readonly string fileName;
        readonly string backupFileFormat = "{0}_backup" + Serializer.FileExtension;
        
        public GameData() {
            fileName = null;
            Key = null;
        }
        
        public GameData(string name, string cryptKey = null) {
            fileName = name + Serializer.FileExtension;
            Key = cryptKey.IsNullOrEmpty() ? null : CryptKey.Get(cryptKey);
        }
        
        #region Save & Load
        
        bool isDirty;
        
        public Action onSetDirty = delegate {};
        
        public void SetDirty() {
            isDirty = true;
            onSetDirty?.Invoke();
        }

        long dirtyCheck;
        
        async UniTask DirtyCheck() {
            var thisProcess = dirtyCheck;
            while (thisProcess == dirtyCheck) {
                if (isDirty) {
                    Save(fileName);
                    await UniTask.WaitForSeconds(1);
                }
                await UniTask.Yield();
            }
        }
        
        async UniTask ReadFromFile(string fileName) {
            var raw = await TextData.LoadTextTask(Path.Combine("Data", fileName), TextCatalog.Persistent);
            
            if (!raw.IsNullOrEmpty()) {
                if (Key != null)
                    raw = raw.Decrypt(Key);
                
                Serializer.Instance.Deserialize(this, raw);
            }
            
            Initialize();
        }

        void Save(string fileName) {
            isDirty = false;
            
            string raw = Serializer.Instance.Serialize(this);
            
            if (Key != null)
                raw = raw.Encrypt(Key);
            
            TextData.SaveText(
                Path.Combine("Data", fileName),
                raw,
                TextCatalog.Persistent);
        }
        
        
        public void Backup(string name) {
            Save(backupFileFormat.FormatText(name));
        }
        
        public void Restore(string name) {
            ReadFromFile(backupFileFormat.FormatText(name)).Forget();
        }

        public UniTask Load() {
            return ReadFromFile(fileName);
        }
        
        void Initialize() {
            modules.ForEach(m => m.Initialize());
            dirtyCheck = YRandom.main.Seed();
            DirtyCheck().Forget();
            Reporter.AddReport($"Game Data ({fileName})", new SerializableReport(this));
        }
        
        public void Clear() {
            modules.Clear();
            SetDirty();
        }   
        
        public string ToServerRaw() {
            var data = new GameData();
            data.modules.Reuse(modules.Where(m => m is IServerDataModule));
            return Serializer.Instance.Serialize(data);
        }
        
        #endregion
        
        #region ISerializable
        
        public void Serialize(IWriter writer) {
            writer.Write("modules", modules);
        }

        public void Deserialize(IReader reader) {
            modules.Reuse(reader.ReadCollection<Module>("modules"));
            modules.ForEach(m => m.setDirty = SetDirty);
            Upgrade(this);
        }
        
        #endregion
        
        static readonly IGameDataUpgrade[] upgrades = Utils
            .FindInheritorTypes<IGameDataUpgrade>(true)
            .Where(t => t.IsInstanceReadyType())
            .Select(Activator.CreateInstance)
            .CastIfPossible<IGameDataUpgrade>()
            .ToArray();
        
        static void Upgrade(GameData data) {
            foreach (var upgrade in upgrades) upgrade.Upgrade(data);
        }
    }

    [Preserve]
    public interface IGameDataUpgrade {
        void Upgrade(GameData data);
    }
    
    public interface IServerDataModule { }
}