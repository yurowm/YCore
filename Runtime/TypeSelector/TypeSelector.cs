using System;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm {
    [Serializable]
    public class TypeSelector {
        [SerializeField]
        string className = "";
        [SerializeField]
        string assemblyName = "";
    
        const string nullClassName = "<null>";
        Type type = null;

        public Type GetSelectedType() {
            if (type == null && className != nullClassName) {
                type = Type.GetType(GetTypeName());
                if (type == null) className = nullClassName;
            }
            return type;
        }

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public class TargetAttribute : Attribute {
            public readonly Type type;
            public TargetAttribute(Type targetType) {
                type = targetType;
            }
        }

        public T Emit<T>(params object[] args) where T : class {
            return Activator.CreateInstance(GetSelectedType(), args) as T;
        }
    
        string GetTypeName() {
            string name = className;
            if (!assemblyName.IsNullOrEmpty())
                name += ", " + assemblyName;
            return name;
        }

        public override string ToString() {
            return GetTypeName();
        }
    }
}