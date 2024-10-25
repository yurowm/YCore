using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class MeshBuilder : MeshBuilderBase {

        Mesh mesh;

        public void CreateMesh() {  
            mesh = new Mesh {
                name = "MeshBuilder"
            };
        }

        public override void Clear() {
            base.Clear();
            mesh?.Clear();
        }

        public Mesh Build() {
            if (vertices.Count >= 3) {
                mesh.SetVertices(vertices
                    .Select(x => x.position.To3D())
                    .ToArray());
                
                mesh.SetColors(vertices
                    .Select(p => p.color)
                    .ToArray());
                
                mesh.SetTriangles(triangles
                    .SelectMany(t => t.GetIDs())
                    .ToArray(), 0);
                
                mesh.SetUVs(0, vertices
                    .Select(x => x.uv0)
                    .ToArray());
                
                mesh.SetUVs(1, vertices
                    .Select(x => x.uv1)
                    .ToArray());

                mesh.RecalculateNormals();
            }

            return mesh;
        }

        public override float GetPointSize() {
            var camera = Camera.main;
            if (camera && camera.orthographic)
                return camera.orthographicSize * 2 / Screen.height;
            
            return base.GetPointSize();
        }
    }
}