using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;

namespace Yurowm {
    
    using Setter = AnimationValueSetter;
    
    [CustomEditor(typeof(AnimationValueSetter))]
    public class AnimationValueSetterEditor : UnityEditor.Editor {
        

        AnimationValueSetter setter;
        
        GUIHelper.Spoiler spoiler;
        
        string[] targetComponentNames;
        
        SerializedProperty property_targetGameObject;
        SerializedProperty property_targetComponent;
        SerializedProperty property_path;
        
        SerializedProperty property_onSetValue;
        
        void OnEnable() {
            setter = target as AnimationValueSetter;

            targetComponentNames = setter.TargetGameObject
                .GetComponents<Component>()
                .Where(c => !(c is AnimationValueSetter))
                .Select(c => c.GetType().Name)
                .Distinct()
                .OrderBy(c => c)
                .ToArray();
            
            
            property_targetGameObject = serializedObject.FindProperty("targetGameObject");
            property_targetComponent = serializedObject.FindProperty("targetComponent");
            property_path = serializedObject.FindProperty("path");
            property_onSetValue = serializedObject.FindProperty("onSetValue");

            if (spoiler != null) return;
            
            spoiler = new GUIHelper.Spoiler(false);
            UpdateMemberInfo();
        }
        
                                
        Type GetBaseType(int index = -1) {
            if (index < 0)
                index = property_path.arraySize;
            
            if (index == 0)
                return setter.TargetGameObject
                    .GetComponent(property_targetComponent.stringValue).GetType();
            
            if (index > 0) {
                var memberInfos = setter.TraceMembers();
                if (memberInfos != null)
                    return Setter.GetMemberType(memberInfos[index - 1]);
            }
            
            return null;
        }   
                                
        MemberInfo targetMember;
        SerializedProperty lastValueProperty = null;
        SerializedProperty realProperty = null;
        
        void UpdateMemberInfo() {
            serializedObject.ApplyModifiedProperties();
            
            var memberInfo = setter.TraceMembers()?.Last();
            
            if (memberInfo == null) return;
            
            var value = setter.GetRealValue();
            
            var type = Setter.GetMemberType(memberInfo);
            
            switch (Setter.GetValueType(type)) {
                case Setter.ValueType.Float:
                    lastValueProperty = serializedObject.FindProperty("valueFloat");
                    break;
                case Setter.ValueType.Bool:
                    lastValueProperty = serializedObject.FindProperty("valueBool");
                    break;
                case Setter.ValueType.Color:
                    lastValueProperty = serializedObject.FindProperty("valueColor");
                    break;
                default: 
                    return;
            }
            
            lastValueProperty.SetObjectValue(Setter.Convert(value, lastValueProperty.GetValueType()));
            
            serializedObject.ApplyModifiedProperties();
            
            targetMember = memberInfo;
            
            setter.SetRealValue(value);
            
            var component = setter.TargetGameObject
                .GetComponent(property_targetComponent.stringValue);
            realProperty = null;
            
            if (!component) return;
             
            var componentSO = new SerializedObject(component);
            
            var path = setter.path.Join(".");

            realProperty = componentSO.FindProperty(path);
            
            OnEnable();
        }
        

        void ClearPathNext(int index) {
            for (int j = property_path.arraySize - 1; j > index; j--)
                property_path.DeleteArrayElementAtIndex(j);
            
            UpdateMemberInfo();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            
            using (spoiler.Start("Member Settings")) {
                if (spoiler.IsVisible()) {
                        
                    #region Target Component

                    using (GUIHelper.Change.Start(OnEnable)) 
                        EditorGUILayout.PropertyField(property_targetGameObject);

                    var selectedTargetComponent = property_targetComponent.stringValue ?? "<Null>";
                    if (selectedTargetComponent.IsNullOrEmpty())
                        selectedTargetComponent = null;
                    
                    if (GUIHelper.Button(property_targetComponent.displayName, selectedTargetComponent ?? "")) {
                        GenericMenu menu = new GenericMenu();

                        foreach (var componentName in targetComponentNames) {
                            var _name = componentName;
                            menu.AddItem(new GUIContent(componentName), componentName == selectedTargetComponent, () => {
                                property_targetComponent.stringValue = _name;
                                property_targetComponent.serializedObject.ApplyModifiedProperties();
                            });
                        }
                        
                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }

                    #endregion

                    #region Path

                    if (selectedTargetComponent != null) {
                        
                        if (property_path.arraySize == 0)
                            property_path.InsertArrayElementAtIndex(0);
                        
                        for (int i = 0; i < property_path.arraySize; i++) {
                            var element = property_path.GetArrayElementAtIndex(i);
                            
                            var value = element.stringValue;

                            if (value.IsNullOrEmpty() && i < property_path.arraySize - 1) {
                                ClearPathNext(i);
                            }
                            
                            if (GUIHelper.Button(" ", value ?? "")) {
                                GenericMenu menu = new GenericMenu();
                                
                                var _i = i;
                                var _element = element;
                                
                                if (i > 0)
                                    menu.AddItem(new GUIContent("[Remove]"), false, () => {
                                        ClearPathNext(_i - 1);
                                        property_path.serializedObject.ApplyModifiedProperties();
                                    });
                                
                                Type type = GetBaseType(i);

                                if (type != null) {
                                    foreach (var memeber in Setter.GetMembers(type)) {
                                        var _m = memeber;
                                        menu.AddItem(new GUIContent(memeber.Name), memeber.Name == value, () => {
                                            _element.stringValue = _m.Name;
                                            ClearPathNext(_i);
                                            property_path.serializedObject.ApplyModifiedProperties();
                                        });
                                    }
                                }

                                if (menu.GetItemCount() > 0)
                                    menu.ShowAsContext();
                            }
                        }
                        
                        
                        if (GUIHelper.Button(" ", "+")) {
                            var baseType = GetBaseType();
                                
                            var value = Setter
                                .GetMembers(baseType)
                                .FirstOrDefault();
                            
                            if (value != null) {
                                property_path.InsertArrayElementAtIndex(property_path.arraySize - 1);
                                property_path.GetArrayElementAtIndex(property_path.arraySize - 1).stringValue = value.Name;
                                UpdateMemberInfo();
                            }
                        }
                    }

                    #endregion

                    EditorGUILayout.PropertyField(property_onSetValue, true);
                }
            }

            if (targetMember != null && lastValueProperty != null)
                using (GUIHelper.Change.Start(SetValue))
                    EditorGUILayout.PropertyField(lastValueProperty, new GUIContent(targetMember.Name.NameFormat()));

            serializedObject.ApplyModifiedProperties();
        }
        
        void SetValue() {
            if(realProperty == null) return;
            realProperty.SetObjectValue(lastValueProperty.GetObjectValue());
            realProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}