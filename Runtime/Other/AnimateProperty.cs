using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AnimateProperty : PropertyAttribute {

        public readonly string ReferenceMemberName;

        public AnimateProperty(string referenceMemberName) {
            ReferenceMemberName = referenceMemberName;
        }
        
        static Dictionary<Type, Dictionary<FieldInfo, PropertyInfo>> maps =
            new Dictionary<Type, Dictionary<FieldInfo, PropertyInfo>>();
        
        static Dictionary<FieldInfo, PropertyInfo> Map(Type type) {
            if (maps.TryGetValue(type, out var result))
                return result;
            
            result = new Dictionary<FieldInfo, PropertyInfo>();
            
            foreach (var member in type.GetMembersDeep<MemberInfo>()) {
                var attribute = member.GetCustomAttribute<AnimateProperty>();
                if (attribute == null)
                    continue;
                
                FieldInfo field = null;
                PropertyInfo property = null;
                
                switch (member) {
                    case FieldInfo f: {
                        field = f;
                        property = type.GetMemberDeep<PropertyInfo>(attribute.ReferenceMemberName);
                    } break;
                    case PropertyInfo p: {
                        property = p;
                        field = type.GetMemberDeep<FieldInfo>(attribute.ReferenceMemberName);
                    } break;
                }
                
                if (field != null && property != null 
                                  && property.CanRead && property.CanWrite 
                                  && field.FieldType == property.PropertyType) {
                    result.Add(field, property);
                }
            }
            
            if (result.Count == 0)
                result = null;
            
            maps.Add(type, result);
            
            return result;
        }

        public static bool Update(object target) {
            var map = Map(target.GetType());
            
            if (map == null) return false;

            bool result = false;

            foreach (var p in map) {
                if (!result && p.Value.GetValue(target).Equals(p.Key.GetValue(target))) continue;
                
                result = true;
                
                p.Value.SetValue(target, p.Key.GetValue(target));
            }
            
            return result;
        }
    }
}
