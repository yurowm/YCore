#if UNITY_EDITOR
using System.Text.RegularExpressions;
using Yurowm.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Yurowm.Serialization;
using Yurowm.YJSONSerialization;

namespace Yurowm.Utilities {

    public class EditorStorage : ISerializable {
        public static EditorStorage Instance {get; private set;}
        
        Dictionary<string, string> strings = new();
        Dictionary<string, bool> bools = new();
        Dictionary<string, float> numbers = new();
        Dictionary<string, ISerializable> serializables = new();
        bool dirty = false;
        
        public void SetDirty() => dirty = true;

        static FileInfo fileInfo;
        
        static EditorStorage() {
            fileInfo = new FileInfo(Application.dataPath);
            fileInfo = new FileInfo(Path.Combine(fileInfo.Directory.FullName, 
                "ProjectSettings", "ProjectEditorSettings" + Serializer.FileExtension));
            Load();
            EditorApplication.update += Update;
        }

        static void Update() {
            if (Instance.dirty) {
                Instance.Save();
                Instance.dirty = false;
            }
        }

        EditorStorage() {}

        #region Set & Get
        Regex keyValidator = new Regex(@"^[\w\d\._]+$");
        bool ValidateKey(string key) {
            if (!keyValidator.IsMatch(key)) {
                Debug.LogError("Wrong key format. Only word characters is allowed");
                return false;
            }
            return true;
        }
        
        void Set<V>(Dictionary<string, V> dictionary, string key, V value) {
            if (ValidateKey(key)) {
                if (dictionary.ContainsKey(key)) {
                    var old = dictionary[key];
                    if (!Equals(old, value)) {
                        dictionary[key] = value;
                        dirty = true;
                    }                    
                } else {
                    dictionary.Add(key, value);
                    dirty = true;
                }
            }
        }
        
        bool Equals<V>(V a, V b) {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        V Get<V>(Dictionary<string, V> dictionary, string key, V defaultValue = default) {
            if (dictionary.TryGetValue(key, out var result))
                return result;
            return defaultValue;
        }
        
        public void SetText(string key, string value) {
            Set(strings, key, value);
        }

        public string GetText(string key, string defaultValue = null) {
            return Get(strings, key, defaultValue);
        }
        
        public void SetBool(string key, bool value) {
            Set(bools, key, value);
        }
        
        public bool GetBool(string key, bool defaultValue = false) {
            return Get(bools, key, defaultValue);
        }

        public void SetNumber(string key, float value) {
            Set(numbers, key, value);
        }
        
        public int GetInt(string key, int defaultValue = 0) {
            return Mathf.RoundToInt(GetFloat(key, defaultValue));
        }

        public float GetFloat(string key, float defaultValue = 0f) {
            return Get(numbers, key, defaultValue);
        }

        public void SetSerializable(string key, ISerializable value) {
            Set(serializables, key, value);
        }

        public ISerializable GetSerializable(string key) {
            return Get(serializables, key);
        }

        #endregion
        
        public void Delete(string key) {
            strings.Remove(key);
            bools.Remove(key);
            numbers.Remove(key);
            serializables.Remove(key);
        }
        
        void Save() {
            var raw = Serializator.ToTextData(this, true);
            
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
                
            File.WriteAllText(fileInfo.FullName, raw);
        }

        static void Load() {
            Instance = new EditorStorage();
            if (fileInfo.Exists) {
                try {
                    string raw = File.ReadAllText(fileInfo.FullName);
                    Serializator.FromTextData(Instance, raw);
                } catch (Exception) {}
            }
        }
        
        public void Serialize(IWriter writer) {
            writer.Write("strings", strings);
            writer.Write("bools", bools);
            writer.Write("numbers", numbers);
            writer.Write("serializables", serializables);
        }

        public void Deserialize(IReader reader) {
            strings = reader.ReadDictionary<string>("strings").ToDictionary();
            bools = reader.ReadDictionary<bool>("bools").ToDictionary();
            numbers = reader.ReadDictionary<float>("numbers").ToDictionary();
            serializables = reader.ReadDictionary<ISerializable>("serializables").ToDictionary();
        }
    }
}
#endif
