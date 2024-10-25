using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.Shapes {
    public class MeshUIBuilder : MeshBuilderBase {
        
        public Canvas canvas;
       
        
        public void Build(VertexHelper vh) {
            vertices.ForEach(v => vh.AddVert(
                v.position, v.color, 
                v.uv0, v.uv1,
                v.normal, v.tangent));
            
            triangles.ForEach(t => 
                vh.AddTriangle(t.idx0, t.idx1, t.idx2));
        }

        public override float GetPointSize() {
            if (canvas)
                return 1f / canvas.scaleFactor;
            return base.GetPointSize();
        }
    }
}