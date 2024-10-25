using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class MeshAsset2D : Mesh2DBase {

        public MeshAsset meshAsset;
        
        MeshData meshDataOverride;
        
        protected override MeshData GetMeshData() {
            if (meshDataOverride != null && !meshDataOverride.vertices.IsEmpty())
                return meshDataOverride;
            
            return meshAsset?.meshData;
        } 
        
        protected override void SetMeshData(MeshData meshData) {
            meshDataOverride = meshData;
        }
    }
}
