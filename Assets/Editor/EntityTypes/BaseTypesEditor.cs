using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm.Spaces {
    public static class BaseTypesEditor {

        public static void SelectBody(IBody item) {
            var bodyType = item.BodyType;
            if (bodyType != null) {
                var selectedBody = AssetManager.GetPrefab(bodyType, item.bodyName);
                AssetManagerEditor.OnSelectGUI("Body", bodyType, selectedBody, 
                    smo => item.bodyName = smo?.name ?? "");
            }
        }
        
        public static void SelectAsset<A>(object obj, string fieldName, Action<A> onChange = null, params GUILayoutOption[] options) where A : Object {
            SelectAsset(fieldName.NameFormat(), obj, fieldName, onChange, options);
        }

        public static void SelectAsset<A>(string label, object obj, string fieldName, Action<A> onChange = null, params GUILayoutOption[] options) where A : Object {
            var member = obj.GetType()
                .GetMember(fieldName, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault();

            if (member == null) {
                Label(label, "No Field");
                return;
            }

            switch (member) {
                case FieldInfo field: {
                    if (field.FieldType == typeof(string)) {
                        A selected = GetAsset<A>(field.GetValue(obj) as string);
                        AssetManagerEditor.OnSelectGUI(label, selected, 
                            newValue => {
                                field.SetValue(obj, newValue?.name ?? "");
                                onChange?.Invoke(newValue);
                            }, null, options);
                    } else
                        Label(label, "Wrong Field Type");

                    return;
                }
                case PropertyInfo property: {
                    if (property.PropertyType != typeof(string))
                        Label(label, "Wrong Property Type");
                    else if (!property.CanRead || !property.CanWrite)
                        Label(label, "No R/W Access");
                    else {
                        A selected = GetAsset<A>(property.GetValue(obj) as string);
                        AssetManagerEditor.OnSelectGUI(label, selected, 
                            newValue => {
                                property.SetValue(obj, newValue?.name ?? "");
                                onChange?.Invoke(newValue);
                            }, null, options);
                    }
                    return;
                }
            }
        }
        
        
        public static void SelectType<T>(object obj, string fieldName, Action<Type> onChange = null) {
            SelectType<T>(fieldName.NameFormat(), obj, fieldName, onChange);
        }

        public static void SelectType<T>(string label, object obj, string fieldName, Action<Type> onChange = null) {
            var member = obj.GetType()
                .GetMember(fieldName, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault();

            if (member == null) {
                Label(label, "No Field");
                return;
            }

            switch (member) {
                case FieldInfo field: {
                    if (field.FieldType == typeof(string)) {
                        var typeName = field.GetValue(obj) as string;
                        var selected = GetType(typeof(T), typeName);
                        OnSelectTypeGUI<T>(label, selected,
                            newValue => {
                                field.SetValue(obj, newValue?.FullName ?? "");
                                onChange?.Invoke(newValue);
                            });
                    } else
                        Label(label, "Wrong Field Type");

                    return;
                }
                case PropertyInfo property: {
                    if (property.PropertyType != typeof(string))
                        Label(label, "Wrong Property Type");
                    else if (!property.CanRead || !property.CanWrite)
                        Label(label, "No R/W Access");
                    else {
                        var typeName = property.GetValue(obj) as string;
                        var selected = GetType(typeof(T), typeName);
                        OnSelectTypeGUI<T>(label, selected,
                            newValue => {
                                property.SetValue(obj, newValue?.FullName ?? "");
                                onChange?.Invoke(newValue);
                            });
                    }
                    return;
                }
            }
        }
        
        public static void OnSelectTypeGUI<T>(string label, Type selected, Action<Type> onChange, bool nullable = true) {
            using (GUIHelper.Horizontal.Start()) {
                
                var types = GetTypes(typeof(T));
                
                if (types.IsEmpty())
                    Label(label, "No Suitable Types");
                else {
                    EditorGUILayout.PrefixLabel(label);
                    
                    if (GUILayout.Button(selected?.FullName ?? "null", EditorStyles.popup)) {
                        GenericMenu menu = new GenericMenu();

                        if (nullable)
                            menu.AddItem(new GUIContent("null"), selected == null,
                                () => onChange(null));
                
                        foreach (var type in types) {
                            var _t = type;
                    
                            menu.AddItem(new GUIContent(type.FullName.Replace('.', '/')),
                                selected == type,
                                () => onChange(_t));
                        }
                
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
                }
            }
        }
        
        static Dictionary<Type, Type[]> types = new Dictionary<Type, Type[]>();
        
        static Type[] GetTypes(Type baseType) {
            if (types.TryGetValue(baseType, out var result))
                return result;
            
            result = baseType.FindInheritorTypes(false)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.FullName)
                .ToArray();
                
            types[baseType] = result;
            
            return result;
        }
        
        static Type GetType(Type baseType, string name) {
            if (name.IsNullOrEmpty())
                return null;
            return GetTypes(baseType).FirstOrDefault(t => t.FullName == name);
        }


        static void Label(string label, string text) {
            if (label.IsNullOrEmpty()) 
                GUILayout.Label(text);
            else 
                EditorGUILayout.LabelField(label, text);
        }
        
        static O GetAsset<O>(string name) where O : Object {
            if (typeof(Component).IsAssignableFrom(typeof(O)))
                return AssetManager.GetPrefab(typeof(O), name) as O;
            return AssetManager.GetAsset<O>(name);
        }
    }
}