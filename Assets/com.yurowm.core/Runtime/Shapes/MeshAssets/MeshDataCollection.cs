using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yurowm.Shapes {
    public static class MeshDataCollection {
        
        static Dictionary<int, MeshData> collection = new();
        
        public static MeshData Get(Mesh mesh) {
            if (!mesh) return null;
            var hashCode = mesh.GetHashCode();
            if (!collection.TryGetValue(hashCode, out var data)) {
                data = MeshUtils.GenerateMeshData(mesh);
                collection[hashCode] = data;
            }
        
            return data;
        }
    }
}