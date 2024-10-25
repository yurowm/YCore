using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;

namespace Yurowm {
    [CustomEditor(typeof(LinearAnimator))]
    [CanEditMultipleObjects]
    public class LinearAnimatorEditor : Editor {
        
        SOHelper soHelper = new SOHelper();

        public override void OnInspectorGUI() {
            var reference = target as LinearAnimator;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(reference.localTime)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(reference.randomizeTime)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(reference.unscaledTime)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(reference.resetTimeOnEnable)));

            soHelper.targetObject = serializedObject;
            
            using (soHelper.Start("rotZ", "Z-Rotation", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("rotAsix"), new GUIContent("Asix"));
                    EditorGUILayout.PropertyField(so.FindProperty("rotZampl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("rotZfreq"), new GUIContent("Frequency"));
                    EditorGUILayout.PropertyField(so.FindProperty("rotZphase"), new GUIContent("Phase"));
                    EditorGUILayout.PropertyField(so.FindProperty("rotZvelocity"), new GUIContent("Linear Speed"));
                    EditorGUILayout.PropertyField(so.FindProperty("rotZoffset"), new GUIContent("Linear Offset"));
                    if (GUIHelper.Button(null, "Normalize Asix")) {
                        var prop = so.FindProperty("rotAsix");
                        prop.vector3Value = prop.vector3Value.FastNormalized();
                    }
                }
            }

            using (soHelper.Start("sizeX", "X-Scale", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("sizeXampl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("sizeXfreq"), new GUIContent("Frequency"));
                }
            }

            using (soHelper.Start("sizeY", "Y-Scale", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("sizeYampl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("sizeYfreq"), new GUIContent("Frequency"));
                }
            }

            using (soHelper.Start("posX", "X-Position", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("posXampl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("posXfreq"), new GUIContent("Frequency"));
                    EditorGUILayout.PropertyField(so.FindProperty("posXphase"), new GUIContent("Phase"));
                    EditorGUILayout.PropertyField(so.FindProperty("posXvelocity"), new GUIContent("Linear Speed"));
                }
            }

            using (soHelper.Start("posY", "Y-Position", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("posYampl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("posYfreq"), new GUIContent("Frequency"));
                    EditorGUILayout.PropertyField(so.FindProperty("posYphase"), new GUIContent("Phase"));
                    EditorGUILayout.PropertyField(so.FindProperty("posYvelocity"), new GUIContent("Linear Speed"));
                }
            }

            using (soHelper.Start("alpha", "Alpha", out var so)) {
                if (so != null) {
                    EditorGUILayout.PropertyField(so.FindProperty("alphaAmpl"), new GUIContent("Amplitude"));
                    EditorGUILayout.PropertyField(so.FindProperty("alphaFreq"), new GUIContent("Frequency"));
                    EditorGUILayout.PropertyField(so.FindProperty("alphaPhase"), new GUIContent("Phase"));
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        class SOHelper : IDisposable {
            SerializedObject serializedObject;
            GUIHelper.IndentLevel indentLevel;
            Type type = typeof(LinearAnimator);
            public SerializedObject targetObject;

            public SOHelper Start(string propertyName, string label, out SerializedObject serializedObject) {
                
                var property = targetObject.FindProperty(propertyName);
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                
                if (!property.boolValue && !property.hasMultipleDifferentValues) {
                    serializedObject = null;
                    return null;
                }
                
                this.serializedObject = null;

                if (property.hasMultipleDifferentValues) {
                    var fieldinfo = type.GetField(propertyName);
                    if (fieldinfo?.FieldType == typeof(bool))
                        this.serializedObject = new SerializedObject(property.serializedObject.targetObjects
                            .Where(o => (bool) fieldinfo.GetValue(o))
                            .ToArray());
                    serializedObject = this.serializedObject;
                } else {
                    serializedObject = property.serializedObject;
                }
                
                indentLevel = GUIHelper.IndentLevel.Start();
                return this;
            }

            public void Dispose() {
                indentLevel.Dispose();
                if (serializedObject != null && serializedObject.hasModifiedProperties)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}