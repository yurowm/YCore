using UnityEngine;

namespace Yurowm.Shapes {
    public class MeshUI : MeshUIBase {
        
        public Mesh mesh;
        
        MeshData md;
        int meshHashCode;
        
        protected override MeshData GetMeshData() {
            if (mesh && meshHashCode != mesh.GetHashCode()) {
                md = MeshDataCollection.Get(mesh);
                meshHashCode = mesh.GetHashCode();
            }
            return md;
        }

        protected override void SetMeshData(MeshData meshData) {
            md = meshData;
            mesh = null;
        }
    }
}