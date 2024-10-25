using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.Shapes {
    public class VertexHelperBuilder : IShapeBuilder {
        public VertexHelper vh;
        public Canvas canvas;

        public int currentVertCount => vh.currentVertCount;
        
        public void AddVert(Vector2 position, Color32 color, Vector2 uv0, Vector2 uv1, Vector3 normal, Vector4 tangent) {
            vh.AddVert(position, color, uv0, uv1, normal, tangent);
        }

        public void AddTriangle(int idx0, int idx1, int idx2, bool flip = false) {
            if (flip)
                vh.AddTriangle(idx0, idx2, idx1);
            else 
                vh.AddTriangle(idx0, idx1, idx2);
        }

        public float GetPointSize() {
            return 1f / canvas.scaleFactor;
        }

        public void Clear() {
            vh.Clear();
        }
    }
}