using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm.Editors {
    [CustomPropertyDrawer(typeof(TypeSelector))]
    public class TypeSelecterEditor : PropertyDrawer {

        List<Type> types = null;
        Type targetType = null;
    
        const string nullTypeName = "<null>";
    
        void FindNames() {
            var attribute = fieldInfo.GetCustomAttribute<TypeSelector.TargetAttribute>(false);

            if (attribute != null)
                targetType = attribute.type;

            if (targetType != null)
                types = targetType.FindInheritorTypes(true).ToList();
        }
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.hasMultipleDifferentValues)
                return;

            if (types == null) FindNames();

            EditorGUI.BeginProperty(position, label, property);
        
            if (types == null)
                EditorGUI.LabelField(position, label, new GUIContent("ERROR", "TypeSelector.TargetAttribute is not set"));
            else if (types.Count == 0)
                EditorGUI.LabelField(position, label, new GUIContent("ERROR", "No type is found"));
            else {
                position = EditorGUI.PrefixLabel(position, label);
            
                var classNameProperty = property.FindPropertyRelative("className");
                var assemblyNameProperty = property.FindPropertyRelative("assemblyName");

                string className = classNameProperty.stringValue;
                string assemblyName = assemblyNameProperty.stringValue;
            
                if (GUI.Button(position, className, EditorStyles.popup)) {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent(nullTypeName), className == nullTypeName,
                        () => {
                            classNameProperty.stringValue = nullTypeName;
                            assemblyNameProperty.stringValue = "";
                            property.serializedObject.ApplyModifiedProperties();
                        });
                
                    foreach (var type in types.OrderBy(r => r.FullName)) {
                        var _t = type;
                    
                        menu.AddItem(new GUIContent(type.FullName),
                            className == type.FullName && assemblyName == type.Assembly.FullName,
                            () => {
                                classNameProperty.stringValue = _t.FullName;
                                assemblyNameProperty.stringValue = _t.Assembly.FullName;
                                property.serializedObject.ApplyModifiedProperties();
                            });
                    }
                
                    if (menu.GetItemCount() > 0)
                        menu.DropDown(position);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}