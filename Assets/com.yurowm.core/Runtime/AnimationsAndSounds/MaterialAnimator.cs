using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Shapes;
using Yurowm.Spaces;

namespace Yurowm {
    public class MaterialAnimator : BaseBehaviour, SpaceTime.ISensitiveComponent {
        Material material;
        
        public string textureName = "_MainTex";
        public Vector2 offsetSpeed = Vector2.up;
        
        Vector2 offset;
        
        void OnEnable() {
            if (this.SetupComponent(out IMaterialProvider materialProvider))
                material = materialProvider.material;
            else
                material = renderer.material;
        
            if (material)
                offset = material.GetTextureOffset(textureName);
            else
                enabled = false;
        }

        void Update() {
            offset += offsetSpeed * DeltaTime;
            material.SetTextureOffset(textureName, offset);
        }

        #region ISensitiveComponent
        
        float DeltaTime => spaceTime?.Delta ?? Time.unscaledDeltaTime;
        
        SpaceTime spaceTime;
        public void OnChangeTime(SpaceTime time) {
            spaceTime = time;
        }

        #endregion
    }
}