using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;

namespace Yurowm.Shapes {
    
    [CustomEditor(typeof(MeshAsset))]
    public class MeshAssetEditor : UnityEditor.Editor {
        
        Vector3[] polygon = new Vector3[3];
        List<Vector3> tempPoints = new List<Vector3>();

        [MenuItem("Assets/Create/Mesh Asset")]
        static void CreateMeshAsset() {
            var meshes = Selection.objects.CastIfPossible<Mesh>().ToArray();
            
            if (meshes.Length == 0) {
                EditorUtility.DisplayDialog("Error",
                    "Select at least one mesh that will be used as a reference", 
                    "Ok");
                return;
            }
            
            if (Selection.objects.Length == 2 && Selection.objects.CastOne<MeshAsset>() != null) {
                // Updating Asset
                var asset = Selection.objects.CastOne<MeshAsset>();
                asset.meshData = MeshUtils.GenerateMeshData(meshes.FirstOrDefault());
            } else {
                foreach (var mesh in meshes) {
                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(mesh));
                    var name = mesh.name;
                    
                    string fullpath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name) + ".asset");

                    var newAsset = MeshAsset.Create(mesh);
                    
                    AssetDatabase.CreateAsset(newAsset, fullpath);
                }
            }
            
            AssetDatabase.SaveAssets();
        }

        public override bool HasPreviewGUI() => true;

        static readonly Color faceColor = Color.gray.Transparent(.5f);
        static readonly Color edgeColor = Color.white.Transparent(.2f);
        static readonly Color borderColor = Color.cyan;

        MeshUtils.TransformMode transformMode = 0;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            var meshAsset = target as MeshAsset;
            var data = meshAsset.meshData;

            using (GUIHelper.Horizontal.Start()) {
                transformMode = (MeshUtils.TransformMode) EditorGUILayout.EnumFlagsField("Transform", transformMode);
                if (transformMode != 0 && GUILayout.Button("Apply", GUILayout.Width(60))) {
                    for (var i = 0; i < data.vertices.Length; i++)
                        data.vertices[i] = MeshUtils.GetTransformVertex(transformMode, meshAsset.meshData.vertices[i]);

                    meshAsset.meshData.FixNormals();
                    meshAsset.meshData.BuildBorders();
                    
                    transformMode = 0;
                    
                    EditorUtility.SetDirty(meshAsset);
                    
                }
            }
            
            if (GUILayout.Button("Fix Normals")) {
                meshAsset.meshData.FixNormals();
                Undo.RecordObject(meshAsset, "Mesh Asset Fix Normals");
            }
            
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background) {
            base.OnPreviewGUI(rect, background);
            
            if (Event.current.type != EventType.Repaint) return;

            rect = rect.GrowSize(-50);
            
            if (target is MeshAsset meshAsset) {
                
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                
                foreach (var vertex in meshAsset.meshData.vertices) {
                    minX = Mathf.Min(minX, vertex.x);
                    maxX = Mathf.Max(maxX, vertex.x);
                    minY = Mathf.Min(minY, vertex.y);
                    maxY = Mathf.Max(maxY, vertex.y);
                }
                
                Rect shapeRect = new Rect();
                var size = rect.size;
                var meshAspectRatio = (maxX - minX) / (maxY - minY);
                
                if (size.x / size.y > meshAspectRatio)
                    size.x = size.y * meshAspectRatio;
                else
                    size.y = size.x / meshAspectRatio;
                
                shapeRect.size = size;
                shapeRect.center = rect.center;
                
                Vector3 GetPosition(Vector2 vertex) {
                    Vector3 result = vertex;
                    result.x = shapeRect.x + shapeRect.width * (result.x - minX) / (maxX - minX);
                    result.y = shapeRect.y + shapeRect.height * (maxY - result.y) / (maxY - minY);
                        
                    return result;
                }
                
                
                
                for (int i = 0; i < meshAsset.meshData.triangles.Length; i+= 3) {
                    for (int p = 0; p < 3; p++)
                        polygon[p] = GetPosition(meshAsset.meshData.vertices[meshAsset.meshData.triangles[i + p]]);
                    
                    Handles.color = faceColor;
                    Handles.DrawAAConvexPolygon(polygon);
                    
                    Handles.color = edgeColor;
                    Handles.DrawAAPolyLine(polygon[0], polygon[1], polygon[2], polygon[0]);
                }

                Handles.color = borderColor;

                foreach (var border in meshAsset.meshData.borders) {
                    tempPoints.Clear();
                    tempPoints.AddRange(border.points.Select(i => GetPosition(meshAsset.meshData.vertices[i])));
                    tempPoints.Add(tempPoints[0]);
                    Handles.DrawAAPolyLine(tempPoints.ToArray());
                }

                Handles.color = borderColor;
            }
        }
    }
}