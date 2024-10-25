using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    public static class ReferenceValues {

        static readonly Dictionary<string, Func<Data>> references = new Dictionary<string, Func<Data>>();
        static List<string> keys = new List<string>();
        static bool isInitialized = false;
        
        [OnLaunch(-1000)]
        public static void Initialize() {
            if (!Utils.IsMainThread() && Application.isPlaying)
                return;
            
            references.Clear();
            
            var parameters = new object[0];
            foreach (var pair in Utils.GetAllMethodsWithAttribute<ReferenceValueAttribute>
                (BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(p => p.Item2.name))
                references[pair.Item2.name] = () => {
                    var method = pair.Item1 as MethodInfo;
                    var data = new Data {
                        type = method.ReturnType,
                        name = pair.Item2.name
                    };
                    try {
                        data.value = method.Invoke(null, parameters);
                    } catch {}
                    return data;
                };
            
            foreach (var pair in Utils.GetAllMethodsWithAttribute<ReferenceValueLoaderAttribute>
                    (BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                (pair.Item1 as MethodInfo)?.Invoke(null, parameters);
                
            keys.Reuse(references.Keys);
            
            isInitialized = true;
        }

        public static void Add<T>(string key, Func<T> provider) {
            if (key.IsNullOrEmpty()) return;
            if (provider == null) return;
            
            references[key] = () => {
                var data = new Data {
                    type = typeof(T),
                    name = key
                };
                try {
                    data.value = provider.Invoke();
                } catch {}
                return data;
            };
        }

        public static object Get(string key) {
            return references.ContainsKey(key) ? references[key]().value : 0;
        }
        
        public static Type GetType(string key) {
            return references.ContainsKey(key) ? references[key]().type : null;
        }
        
        public static List<Data> Keys() {
            if (!isInitialized) Initialize();
            return references.Values.Select(v => v.Invoke()).ToList();
        }

        public struct Data {
            public Type type;
            public string name;
            public object value;
        }
    }
    
    public class ReferenceValueAttribute : Attribute {
        public readonly string name;
        
        public ReferenceValueAttribute(string name) {
            this.name = name;
        }
    }
    
    public class ReferenceValueLoaderAttribute : Attribute { }
}