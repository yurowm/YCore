using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm {
    [CustomPropertyDrawer(typeof(AnimateProperty))]
    public class AnimatePropertyDrawer : PropertyDrawer {

        static readonly MethodInfo DrawDefaultPropertyField = 
            typeof(EditorGUI)
            .GetMethod("DefaultPropertyField",
            BindingFlags.Static | BindingFlags.NonPublic);
        
        object[] invokeParameters = new object[3];
        PropertyInfo propertyInfo = null;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            
            var target = property.serializedObject.targetObject;
            
            if (propertyInfo == null)
                propertyInfo = target
                    .GetType()
                    .GetMemberDeep<PropertyInfo>(((AnimateProperty) attribute).ReferenceMemberName);

            if (propertyInfo == null) {
                EditorGUI.LabelField(position, label, new GUIContent("Error"));   
                return;
            }
            
            invokeParameters[0] = position;
            invokeParameters[1] = property;
            invokeParameters[2] = new GUIContent(((AnimateProperty) attribute).ReferenceMemberName.NameFormat());
            
            EditorGUI.BeginChangeCheck();
            
            DrawDefaultPropertyField.Invoke(null, invokeParameters);

            if (!EditorGUI.EndChangeCheck()) return;
            
            property.serializedObject.ApplyModifiedProperties();

            Undo.RecordObject(target, "Inspector");
 
            propertyInfo.SetValue(target, property.GetObjectValue(),null);
        }
    }
}