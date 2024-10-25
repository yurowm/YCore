using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class SetMeshAsset : Repaint, IRepaintSetShape {
        IMeshDataComponent component;
        
        public void SetMesh(MeshData meshData) {
            if (component != null || this.SetupComponent(out component))
                component.meshData = meshData;
        }
    }
}