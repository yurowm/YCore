using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Scripting;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Services {
    [Preserve]
    public abstract class ThirdPartyAPI {
        
        static ThirdPartyAPI[] all;
        
        static ThirdPartyAPI() {
            all = Utils.FindInheritorTypes<ThirdPartyAPI>(true)
                .Where(t => !t.IsAbstract)
                .Select(Activator.CreateInstance)
                .Cast<ThirdPartyAPI>()
                .ToArray();
            all.ForEach(a => a.Initialize());
        }
        
        public static ThirdPartyAPI Get(string name) {
            name = name.ToLower();
            return all.FirstOrDefault(a => a.name == name);
        }

        public abstract string GetName();
        
        string name;
        
        void Initialize() {
            name = GetName().ToLower();
            
            var type = GetType();
            methods = type
                .GetMethods(methodBindingFlags)
                .Where(m => m.DeclaringType == type)
                .GroupBy(m => m.Name)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        }
        
        Dictionary<string, MethodInfo> methods;
        
        const BindingFlags methodBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        
        public void Call(string methodName, params object[] arg) {
            if (methodName.IsNullOrEmpty())
                return;
            
            if (methods.TryGetValue(methodName, out var methodInfo))
                methodInfo.Invoke(this, arg);
            else
                throw new Exception($"Wrong API Call: '{methodName}')");
        }
    }
}