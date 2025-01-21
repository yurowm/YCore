using UnityEngine;

namespace Yurowm.Shapes {
    public interface IShapeBuilder {
        
        int currentVertCount { get; }
        
        void AddVert(Vector2 position, Color32 color,
            Vector2 uv0, Vector2 uv1,
            Vector3 normal, Vector4 tangent); 
        
        void AddTriangle(int idx0, int idx1, int idx2, bool flip = false);
        
        float GetPointSize();
    }
}