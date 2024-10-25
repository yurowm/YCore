using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Extensions;

namespace Yurowm.Utilities {
    public class ObjectTag : BaseBehaviour {
        public Action<bool> onEnableStatusChanged = null;
        
        static Dictionary<string, HashSet<GameObject>> objects = new();
        
        [OnLaunch(Behaviour.INITIALIZATION_ORDER)]
        public static void Initialize() {
            SceneManager
                .GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<ObjectTag>(true))
                .ForEach(c => c.Add());
            
            OnLaunchAttribute.unload += () => objects.Clear();
        }

        public new string tag = "";
        string _tag;

        void Start() {
            Add();    
        }

        void OnDestroy() {
            Remove();
        }

        void Add() {
            _tag = tag;
            if (!objects.ContainsKey(_tag))
                objects.Add(_tag, new HashSet<GameObject>());
            objects[_tag].Add(gameObject);
        }

        void Remove() {
            if (!_tag.IsNullOrEmpty() && objects.ContainsKey(_tag))
                objects[_tag].Remove(gameObject);
        }

        public static List<GameObject> GetAll(string tag) {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].ToList();
            return new List<GameObject>();
        }

        public static GameObject Get(string tag) {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].FirstOrDefault();
            return null;
        }

        public static GameObject GetRandom(string tag) {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].GetRandom();
            return null;
        }

        public static List<T> GetAll<T>(string tag) where T : Component {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].Select(x => x.GetComponent<T>()).ToList();
            return new List<T>();
        }

        public static T Get<T>(string tag) where T : Component {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].FirstOrDefault()?.GetComponent<T>();
            return null;
        }

        public static T GetRandom<T>(string tag) where T : Component {
            if (!tag.IsNullOrEmpty() && objects.ContainsKey(tag) && objects[tag].Count > 0)
                return objects[tag].GetRandom().GetComponent<T>();
            return null;
        }
        
        void OnEnable() {
            onEnableStatusChanged?.Invoke(true);
        }

        void OnDisable() {
            onEnableStatusChanged?.Invoke(false);
        }
    }
}