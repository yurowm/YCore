using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Serialization {
    public static class PropertyStorage {
        
        [OnLaunch(int.MinValue)]
        static IEnumerator OnLaunch() {
            if (!OnceAccess.GetAccess("PropertyStorage")) 
                yield break;

            var storages = Utils
                .FindInheritorTypes<IPropertyStorage>(true, true)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(Activator.CreateInstance)
                .CastIfPossible<IPropertyStorage>();
            
            foreach (var storage in storages) {
                yield return Load(storage);
                loadedStorages.Add(storage);
            }
        }
        
        public static void Save(IPropertyStorage storage) {
            string raw = Serializator.ToTextData(storage, true);
            if (storage.Catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                raw = raw.Encrypt();
            TextData.SaveText(Path.Combine("Data", storage.FileName), raw, storage.Catalog);
        }

        static void Load(IPropertyStorage storage, string raw) {
            if (raw.IsNullOrEmpty())
                return;

            if (storage.Catalog == TextCatalog.StreamingAssets && !Application.isEditor)
                raw = raw.Decrypt();
                
            Serializator.FromTextData(storage, raw);
        }

        public static IEnumerator Load(IPropertyStorage storage) {
            string raw = null;
            
            yield return TextData.LoadTextRoutine(Path.Combine("Data", storage.FileName), 
                r => raw = r,
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
        
        public static IEnumerator Load<S>(Action<S> getResult) where S : IPropertyStorage {
            if (GetLoadedStorage<S>(out var storage)) {
                getResult?.Invoke(storage);
                yield break;
            }
            var result = Activator.CreateInstance<S>();
            yield return Load(result);
            loadedStorages.Add(result);
            getResult?.Invoke(result);
        }
        
        public static IEnumerator GetSource(IPropertyStorage storage, Action<string> getResult) {
            string result = null;
            yield return TextData.LoadTextRoutine(Path.Combine("Data", storage.FileName),
                r => result = r,
                storage.Catalog);
            getResult?.Invoke(result);
        }
    }

    [Preserve]
    public interface IPropertyStorage : ISerializable {
        string FileName {get;}
        TextCatalog Catalog {get;}
    }
}
    
