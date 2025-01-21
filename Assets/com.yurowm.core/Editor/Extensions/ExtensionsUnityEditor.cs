using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yurowm.Extensions {
    public static class ExtensionsUnityEditor {
        
        public static object GetObjectValue(this SerializedProperty property) {
            switch (property.propertyType) {
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Float: return property.floatValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Color: return property.colorValue;
                case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask: return property.intValue;
                case SerializedPropertyType.Enum: return property.enumValueIndex;
                case SerializedPropertyType.Vector2: return property.vector2Value;
                case SerializedPropertyType.Vector3: return property.vector3Value;
                case SerializedPropertyType.Vector4: return property.vector4Value;
                case SerializedPropertyType.Vector2Int: return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int: return property.vector3IntValue;
                case SerializedPropertyType.Rect: return property.rectValue;
                case SerializedPropertyType.ArraySize: return property.arraySize;
                case SerializedPropertyType.Character: return property.intValue;
                case SerializedPropertyType.AnimationCurve: return property.animationCurveValue;
                case SerializedPropertyType.Bounds: return property.boundsValue;
                case SerializedPropertyType.Quaternion: return property.quaternionValue;
                case SerializedPropertyType.ExposedReference: return property.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize: return property.fixedBufferSize;
                case SerializedPropertyType.RectInt: return property.rectIntValue;
                case SerializedPropertyType.BoundsInt: return property.boundsIntValue;
                default: return null;
            }
        }
        
        public static void SetObjectValue(this SerializedProperty property, object value) {
            switch (property.propertyType) {
                case SerializedPropertyType.Integer: property.intValue = (int) value; return;
                case SerializedPropertyType.Boolean: property.boolValue = (bool) value; return;
                case SerializedPropertyType.Float: property.floatValue = (float) value; return;
                case SerializedPropertyType.String: property.stringValue = (string) value; return;
                case SerializedPropertyType.Color: property.colorValue = (Color) value; return;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = (Object) value; return;
                case SerializedPropertyType.LayerMask: property.intValue = (int) value; return;
                case SerializedPropertyType.Enum: property.enumValueIndex = (int) value; return;
                case SerializedPropertyType.Vector2: property.vector2Value = (Vector2) value; return;
                case SerializedPropertyType.Vector3: property.vector3Value = (Vector3) value; return;
                case SerializedPropertyType.Vector4: property.vector4Value = (Vector4) value; return;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = (Vector2Int) value; return;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = (Vector3Int) value; return;
                case SerializedPropertyType.Rect: property.rectValue = (Rect) value; return;
                case SerializedPropertyType.ArraySize: property.arraySize = (int) value; return;
                case SerializedPropertyType.Character: property.intValue = (int) value; return;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = (AnimationCurve) value; return;
                case SerializedPropertyType.Bounds: property.boundsValue = (Bounds) value; return;
                case SerializedPropertyType.Quaternion: property.quaternionValue = (Quaternion) value; return;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = (Object) value; return;
                case SerializedPropertyType.RectInt: property.rectIntValue = (RectInt) value; return;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = (BoundsInt) value; return;
                default: return;
            }
        }
        
        public static Type GetValueType(this SerializedProperty property) {
            switch (property.propertyType) {
                case SerializedPropertyType.Integer: return typeof (int);
                case SerializedPropertyType.Boolean: return typeof (bool);
                case SerializedPropertyType.Float: return typeof (float);
                case SerializedPropertyType.String: return typeof (string);
                case SerializedPropertyType.Color: return typeof (Color);
                case SerializedPropertyType.ObjectReference: return typeof (Object);
                case SerializedPropertyType.LayerMask: return typeof (int);
                case SerializedPropertyType.Enum: return typeof (int);
                case SerializedPropertyType.Vector2: return typeof (Vector2);
                case SerializedPropertyType.Vector3: return typeof (Vector3);
                case SerializedPropertyType.Vector4: return typeof (Vector4);
                case SerializedPropertyType.Vector2Int: return typeof (Vector2Int);
                case SerializedPropertyType.Vector3Int: return typeof (Vector3Int);
                case SerializedPropertyType.Rect: return typeof (Rect);
                case SerializedPropertyType.ArraySize: return typeof (int);
                case SerializedPropertyType.Character: return typeof (int);
                case SerializedPropertyType.AnimationCurve: return typeof (AnimationCurve);
                case SerializedPropertyType.Bounds: return typeof (Bounds);
                case SerializedPropertyType.Quaternion: return typeof (Quaternion);
                case SerializedPropertyType.ExposedReference: return typeof (Object);
                case SerializedPropertyType.RectInt: return typeof (RectInt);
                case SerializedPropertyType.BoundsInt: return typeof (BoundsInt);
                default: return null;
            }
        }
        
    }
}