using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm.ContentManager {
    [CreateAssetMenu(fileName = "AssetManager", menuName = "Managers/Asset Manager")]
    public class AssetManager : SingletonScriptableObject<AssetManager> {

        public List<Item> aItems = new List<Item>();

        Dictionary<string, Object[]> assets = new Dictionary<string, Object[]>();
        Dictionary<string, GameObject[]> prefabs = new Dictionary<string, GameObject[]>();
        Dictionary<int, GameObject[]> prefabs_hash = new Dictionary<int, GameObject[]>();

        public override void Initialize() {
            base.Initialize();
            aItems.RemoveAll(x => x.item == null);
            
            assets = aItems
                .GroupBy(i => i.item.name)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => i.item).ToArray());
            
            prefabs = assets.Values
                .Select(o => o.CastIfPossible<GameObject>().ToArray())
                .Where(p => !p.IsEmpty())
                .ToDictionary(g => g[0].name, g => g);
            
            prefabs_hash = prefabs
                .ToDictionary(
                    g => g.Key.GetHashCode(),
                    g => g.Value);
        }

        #region GetAsset

        public static A GetAsset<A>(string key) where A: class {
            return GetAssets(key)?.CastOne<A>();
        }

        public static Object[] GetAssets(string key) {
            if (!Instance || key.IsNullOrEmpty()) return null;
            if (!Instance.assets.ContainsKey(key)) return null;
            return Instance.assets[key];
        }

        public static IEnumerable<A> GetAssets<A>(Func<A, bool> condition = null) where A : Object {
            if (!Instance) yield break;
            foreach (var item in Instance.aItems)
                if (item.item is A a)
                    if (condition == null || condition.Invoke(a))
                        yield return a;
        }

        public static A GetAsset<A>(Func<A, bool> condition = null) where A : Object {
            return GetAssets(condition).FirstOrDefault();
        }

        #endregion

        #region Create

        public static T Create<T>(int hash) where T : Component {
            return Create(hash)?.GetComponent<T>();
        }

        public static T Create<T>(string key) where T : Component {
            return Create(key)?.GetComponent<T>();
        }

        public static T Create<T>() where T : Component {
            var component = GetPrefab<T>();
            return component ? Create<T>(component.name) : null;
        }

        public static GameObject Create(string key) {
            if (Instance.prefabs.ContainsKey(key)) {
                var instance = Instantiate(Instance.prefabs[key].FirstOrDefault());
                instance.name = key;
                instance.SetActive(true);
                return instance;
            }
            return null;
        }

        public static GameObject Create(int hash) {
            if (Instance.prefabs_hash.ContainsKey(hash)) {
                var prefab = Instance.prefabs_hash[hash].FirstOrDefault();
                var instance = Instantiate(prefab);
                instance.name = prefab.name;
                instance.SetActive(true);
                return instance;
            }
            return null;
        }

        #endregion

        #region GetPrefab
        
        public static T GetPrefab<T>(string key) where T : Component {
            GameObject obj = GetPrefab(key);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        public static Component GetPrefab(Type type, string key) {
            GameObject obj = GetPrefab(key);
            return obj != null ? obj.GetComponent(type) : null;
        }

        public static GameObject GetPrefab(string key) {
            if (!Instance || key.IsNullOrEmpty()) return null;
            return Instance.prefabs.Get(key)?.FirstOrDefault();
        }

        public static T GetPrefab<T>(Func<T, bool> condition = null) where T : Component {
            return GetPrefabList(condition).FirstOrDefault();
        }
        
        public static IEnumerable<T> GetPrefabList<T>(Func<T, bool> condition = null) where T : class {
            if (!Instance) yield break;
            foreach (var prefabs in Instance.prefabs.Values)
            foreach (var prefab in prefabs)
                if (prefab.SetupComponent(out T t) && (condition?.Invoke(t) ?? true))
                    yield return t;
        }
        
        #endregion

        #region Emit

        public static L Emit<L>(L reference = null, LiveContext context = null) where L : ContextedBehaviour {
            if (!Instance) return null;
            L result;
            L originalComponent = null;
            if (!reference) {
                result = GetPrefab<L>();
                if (result) originalComponent = result.gameObject.GetComponent<L>();
                result = null;
            } else if (reference._original) {
                originalComponent = reference._original.GetComponent<L>();
            } else {
                if (Instance.prefabs.Values.SelectMany(v => v).All(x => x.gameObject != reference.gameObject))
                    throw new Exception("This is a wrong reference. Use only original references from Content2 manager or instances which was created by original reference.");
                originalComponent = reference.gameObject.GetComponent<L>();
            }

            return Emit<L>(item => item.EqualContent(originalComponent), context);
        }

        public static L Emit<L>(string name, LiveContext context = null) where L : ContextedBehaviour {
            try {
                return Emit<L>(item => name.IsNullOrEmpty() || item.name == name, context);
            } catch {
                return null;
            }
        }

        static L Emit<L>(Func<ContextedBehaviour, bool> condition, LiveContext context = null) where L : ContextedBehaviour {
            if (!Instance) return null;

            L result = Reserve.Take<L>(condition, context);
            if (result) return result;

            L prefab = Instance.prefabs.Values
                .SelectMany(a => a)
                .Select(i => i.GetComponent<L>())
                .FirstOrDefault(i => i != null && condition(i));

            if (prefab) {
                result = Instantiate(prefab.gameObject).GetComponent<L>();
                result._original = prefab.gameObject;
                result.name = prefab.name;
                (context ?? LiveContext.globalContext).Add(result);
                return result;
            }

            Debug.LogError("The prefab is not found in the storage");
            return null;
        }

        #endregion
        
        [Serializable]
        public class Item {
            public Item(Object item) {
                this.item = item;
            }

            public Object item;
            public string path = "";

            int _hashCode = 0;
            public override int GetHashCode() {
                if (_hashCode == 0) _hashCode = item.name.GetHashCode();
                return _hashCode;
            }

            public override string ToString() {
                return item.name;
            }
        }
    }
}

