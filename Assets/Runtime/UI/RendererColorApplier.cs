using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.UI {
    public class RendererColorApplier : RendererPropertyApplier {
        
        public string propertyName = "";
        public Color color = Color.white;
        
        public override void ModifyProperty(MaterialPropertyBlock block) {
            if (!propertyName.IsNullOrEmpty())
                block.SetColor(propertyName, color);
        }

        public override void ModifyProperty(Material material) {
            material.SetColor(propertyName, color);
        }
    }
}