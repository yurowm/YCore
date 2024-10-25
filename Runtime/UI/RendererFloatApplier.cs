using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.UI {
    public class RendererFloatApplier : RendererPropertyApplier {
        
        public string propertyName = "";
        public float value = 0f;
        
        public override void ModifyProperty(MaterialPropertyBlock block) {
            if (!propertyName.IsNullOrEmpty())  
                block.SetFloat(propertyName, value);
        }

        public override void ModifyProperty(Material material) {
            material.SetFloat(propertyName, value);
        }
    }
}