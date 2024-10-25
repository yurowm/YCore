using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Yurowm {
    [ExecuteAlways]
    public class AnimationValueSetter : MonoBehaviour {
        const BindingFlags fieldBinding = BindingFlags.Instance 
                                          | BindingFlags.Public
                                          | BindingFlags.GetField | BindingFlags.SetField 
                                          | BindingFlags.GetProperty | BindingFlags.SetProperty; 

        public enum ValueType {
            None = -1,
            Float = 0,
            Bool = 1,
            Color = 2
        }
        
        [SerializeField]
        GameObject targetGameObject;
        public GameObject TargetGameObject {
            get {
                if (targetGameObject)
                    return targetGameObject;
                return gameObject;
            }
        }
        
        public string targetComponent;
        
        public string[] path;
        
        public float valueFloat;
        public bool valueBool;
        public Color valueColor;

        public UnityEvent onSetValue = new UnityEvent();
        
        Component component;
        MemberInfo[] memberInfos;
        
        ValueType valueType = ValueType.None;

        void Awake() {
            Refresh();
            
            enabled = false;
        }

        void OnEnable() {
            lastValue = GetRealValue();
        }

        void OnDisable() {
            Apply();
        }


        bool isInitialized = false;
        public void Refresh() {
            if (isInitialized && Application.isPlaying) return;
            
            memberInfos = TraceMembers();
            
            if (memberInfos == null || memberInfos.Length == 0) return;
            
            isInitialized = true;
            
            var type = GetMemberType(memberInfos[memberInfos.Length - 1]);
            
            valueType = GetValueType(type);
        }

        public MemberInfo[] TraceMembers() {
            component = TargetGameObject.GetComponent(targetComponent);
            if (!component) return null;
            
            Type type = component.GetType();
            
            var result = new MemberInfo[path.Length];
            
            for (int i = 0; i < path.Length; i++) {
                var p = path[i];
                var member = GetMember(type, p);

                if (member == null)
                    return null;
                
                type = GetMemberType(member);
                result[i] = member;
            }
            
            return result;
        }

        #region Reflection
        
        public static MemberInfo GetMember(Type type, string name) {
            return type.GetMember(name).FirstOrDefault();
        }
        
        public static IEnumerable<MemberInfo> GetMembers(Type type) {
            foreach (var field in type.GetFields(fieldBinding))
                yield return field;
            foreach (var property in type.GetProperties(fieldBinding))
                if (property.CanRead && property.CanWrite)
                    yield return property;
        }

        public static Type GetMemberType(MemberInfo info) {
            switch (info) {
                case FieldInfo f: return f.FieldType;
                case PropertyInfo p: return p.PropertyType;
                default: return null;
            }
        }

        public static object GetValue(MemberInfo info, object parent) {
            switch (info) {
                case FieldInfo f: return f.GetValue(parent);
                case PropertyInfo p: return p.GetValue(parent);
                default: return null;
            }
        }

        public static void SetValue(MemberInfo info, object parent, object value) {
            switch (info) {
                case FieldInfo f: f.SetValue(parent, value); return;
                case PropertyInfo p: p.SetValue(parent, value); return;
            }
        }
        

        #endregion
        
        public static ValueType GetValueType(Type type) {
            if (type == typeof(float) || type == typeof(byte))
                return ValueType.Float;
            
            if (type == typeof(bool))
                return ValueType.Bool;
            
            if (type == typeof(Color) || type == typeof(Color32))
                return ValueType.Color;
            
            return ValueType.None;
        }
        
        public object GetRealValue() {
            Refresh();
            
            if (memberInfos == null || component == null) return null;
            
            object result = component;

            foreach (var field in memberInfos) 
                result = GetValue(field, result);

            return result;
        }
        
        public void SetRealValue(object value) {
            Refresh();
            
            if (memberInfos == null) return;
            
            SetRealValueRecursive(component, 0, value);
        }
        
        void SetRealValueRecursive(object obj, int index, object value) {
            
            var member = memberInfos[index];
            
            if (index == memberInfos.Length - 1) {
                value = Convert(value, GetMemberType(member));
                
                SetValue(member, obj, value);
            } else {
                var currentValue = GetValue(member, obj);
                
                SetRealValueRecursive(currentValue, index + 1, value);
                
                if (GetMemberType(member).IsValueType) 
                    SetValue(member, obj, currentValue);
            }            
        }
        
        public static object Convert(object value, Type type) {
            if (value.GetType() == type) return value;
            
            if (value is Color color && type == typeof(Color32))
                return (Color32) color;
            
            if (value is Color32 color32 && type == typeof(Color))
                return (Color) color32;
            
            if (value is float f) {
                if (type == typeof(byte))
                    return (byte) Mathf.CeilToInt(Mathf.Clamp01(f) * 255);
                return value;
            }
            if (value is byte b) {
                if (type == typeof(float))
                    return (float) b / 255;
                return value;
            }
            
            return value;
        }
        
        public object GetValue() {
            switch (valueType) {
                case ValueType.Float: return valueFloat;
                case ValueType.Bool: return valueBool;
                case ValueType.Color: return valueColor;
                default: return null;
            }
        }
        
        object lastValue;
        
        void Update() {
            Apply();
        }
    
        void Apply() {
            if (!isInitialized)
                Refresh();
            
            if (memberInfos == null) {
                enabled = false;
                return;
            }
            
            var value = GetValue();
            
            if (value == lastValue) return;
            
            lastValue = value;
            
            SetRealValue(value);
            
            onSetValue.Invoke();
        }
    }
    
}