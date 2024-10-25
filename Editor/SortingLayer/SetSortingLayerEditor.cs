using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEditorInternal;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Properties {
    [CustomPropertyDrawer(typeof(SortingLayerAndOrder))]
    public class SortingLayerProperty : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.hasMultipleDifferentValues)
                return;

            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, label);
            
            var rect = position;
            
            rect.width /= 2;
            
            var layerProperty = property.FindPropertyRelative("layerID");
            var orderProperty = property.FindPropertyRelative("order");
            
            string[] layerNames = GetSortingLayerNames();
            List<int> layerIDs = GetSortingLayerUniqueIDs().ToList();
            
            int currentIndex = Mathf.Max(0, layerIDs.IndexOf(layerProperty.intValue));

            int newIndex = EditorGUI.Popup(rect, currentIndex, layerNames);
            if (newIndex != currentIndex) 
                layerProperty.intValue = layerIDs[newIndex];
            
            rect.x += rect.width;

            orderProperty.intValue = EditorGUI.IntField(rect, orderProperty.intValue);
            
            EditorGUI.EndProperty();
        }

        public static string[] GetSortingLayerNames() {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[]) sortingLayersProperty.GetValue(null, new object[0]);
        }

        public static int[] GetSortingLayerUniqueIDs() {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[]) sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        }

        public static void DrawSortingLayerAndOrder(string name, SortingLayerAndOrder sorting) {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent(name));
            rect.xMin = rect2.x;

            Rect fieldRect = new Rect(rect);
            fieldRect.width /= 2;

            string[] layerNames = GetSortingLayerNames();
            List<int> layerIDs = GetSortingLayerUniqueIDs().ToList();
            int id = Mathf.Max(0, layerIDs.IndexOf(sorting.layerID));
            sorting.layerID = layerIDs.Get(EditorGUI.Popup(fieldRect, id, layerNames));
            fieldRect.x += fieldRect.width;

            sorting.order = EditorGUI.IntField(fieldRect, sorting.order);
        }
    }
}