using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Yurowm.Extensions;
using Object = UnityEngine.Object;

namespace Yurowm {
    public static class AnimationEventUtilities {
        
        static readonly Type stringType = typeof(string);
        static readonly Type intType = typeof(int);
        static readonly Type floatType = typeof(float);
        static readonly Type objectType = typeof(Object);
        static readonly Type voidType = typeof(void);
        static readonly object[] emptyParamters = new object[0];
        
        public static bool IsTimeForEvent(bool reverse, float eventTime, float lastTime, float time) {
            if (lastTime == time)
                return false;
            
            if (reverse) {
                if (time < lastTime)
                    return time <= eventTime && eventTime < lastTime;
                else
                    return time <= eventTime || eventTime < lastTime;
                
            } else {
                if (time > lastTime)
                    return lastTime < eventTime && eventTime <= time;
                else
                    return lastTime < eventTime || eventTime <= time;
            }
        }
        
        public static void Invoke(GameObject gameObject, AnimationEvent e) {
            var parameterType = GetEventParameterType(e);
                
            gameObject
                .GetComponents<Component>()
                .Any(c => TryToInvokeMethod(e, c, parameterType));
        }

        static bool TryToInvokeMethod(AnimationEvent e, Component component, Type parameterType) {
            const BindingFlags methodBinding = BindingFlags.Public | BindingFlags.Instance;
            
            var componentType = component.GetType();
            
            foreach (var method in componentType.GetMethods(methodBinding)) {
                if (method.Name != e.functionName) continue;
                
                if (method.ReturnType != voidType) continue;
                
                var parameters = method.GetParameters();
                
                if (parameters.Length > 1) continue;
                
                var realRarameterType = parameters.FirstOrDefault()?.ParameterType;

                if (parameterType == realRarameterType) {
                    method.Invoke(component, GetParameter(e, parameterType));
                    return true;
                }
                
                if (parameterType == null && IsSuitableParameterType(realRarameterType)) {
                    method.Invoke(component, GetParameter(e, realRarameterType));
                    return true;
                }
            }
                    
            return false;
        }

        static object[] GetParameter(AnimationEvent e, Type parameterType) {
            if (parameterType == stringType) return new object[] {e.stringParameter} ;
            if (parameterType == intType) return new object[] {e.intParameter} ;
            if (parameterType == floatType) return new object[] {e.floatParameter} ;
            if (parameterType == objectType) return new object[] {e.objectReferenceParameter} ;
            
            return emptyParamters;
        }

        static Type GetEventParameterType(AnimationEvent e) {
            Type result = null;
            
            if (!e.stringParameter.IsNullOrEmpty()) result = stringType;
            if (e.intParameter != 0) result = intType;
            if (e.floatParameter != 0) result = floatType;
            if (e.objectReferenceParameter) result = objectType;
            
            return result;
        }
        
        static bool IsSuitableParameterType(Type type) {
            return type == stringType || type == intType || type == floatType || type == objectType;
        }
    }
}