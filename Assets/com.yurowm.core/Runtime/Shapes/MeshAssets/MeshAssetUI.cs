namespace Yurowm.Shapes {
    public class MeshAssetUI : MeshUIBase {
        
        public MeshAsset meshAsset;
        
        MeshData meshDataOverride;
        
        protected override MeshData GetMeshData() => meshDataOverride ?? meshAsset?.meshData;
        
        protected override void SetMeshData(MeshData meshData) {
            meshDataOverride = meshData;
        }
    }
}
