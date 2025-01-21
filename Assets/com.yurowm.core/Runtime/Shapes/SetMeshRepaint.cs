using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class SetMeshRepaint : Repaint, IRepaintSetMesh {
        IMeshComponent component;
        
        public void SetMesh(Mesh mesh) {
            if (component != null || this.SetupComponent(out component))
                component.SetMesh(mesh);
        }
    }
}