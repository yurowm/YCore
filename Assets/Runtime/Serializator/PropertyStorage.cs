using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {
    public static class PropertyStorage {
        
        [OnLaunch(int.MinValue)]
        static async UniTask OnLaunch() {
            if (!OnceAccess.GetAccess("PropertyStorage")) 
                return;

            var storages = Utils
                .FindInheritorTypes<IPropertyStorage>(true, true)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(Activator.CreateInstance)
                .CastIfPossible<IPropertyStorage>();
            
            foreach (var storage in storages) {
                await Load(storage);
                loadedStorages.Add(storage);
            }
        }
        
        public static void Save(IPropertyStorage storage) {
            string raw = Serializer.Instance.Serialize(storage);
            if (storage.Catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                raw = raw.Encrypt();
            TextData.SaveText(Path.Combine("Data", storage.FileName), raw, storage.Catalog);
        }

        static void Load(IPropertyStorage storage, string raw) {
            if (raw.IsNullOrEmpty())
                return;

            if (storage.Catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                raw = raw.Decrypt();
                
            Serializer.Instance.Deserialize(storage, raw);
        }

        public static async UniTask Load(IPropertyStorage storage) {
            var raw = await TextData.LoadTextTask(Path.Combine("Data", storage.FileName), 
                storage.Catalog);
            
            Load(storage, raw);
        }

        public static void LoadInEditor(IPropertyStorage storage) {
            var raw = TextData.LoadTextInEditor(Path.Combine("Data", storage.FileName), storage.Catalog);
            Load(storage, raw);
        }

        static List<IPropertyStorage> loadedStorages = new ();
        static bool GetLoadedStorage<S>(out S storage) where S : IPropertyStorage {
            storage = loadedStorages.CastOne<S>();
            return storage != null;
        }
        
        public static S GetInstance<S>() where S : IPropertyStorage {
            if (GetLoadedStorage<S>(out var storage))
                return storage;
            #if UNITY_EDITOR
            var result = Activator.CreateInstance<S>();
            LoadInEditor(result);
            loadedStorages.Add(result);
            return result;
            #else
            return default;
            #endif
        }
        
        public static async UniTask<S> Load<S>() where S : IPropertyStorage {
            if (GetLoadedStorage<S>(out var storage)) {
                return storage;
            }
            var result = Activator.CreateInstance<S>();
            await Load(result);
            loadedStorages.Add(result);
            return result;
        }
        
        public static async UniTask<string> GetSource(IPropertyStorage storage) {
            return await TextData.LoadTextTask(Path.Combine("Data", storage.FileName), storage.Catalog);
        }
    }

    [Preserve]
    public interface IPropertyStorage : ISerializable {
        string FileName {get;}
        TextCatalog Catalog {get;}
    }
}
    
