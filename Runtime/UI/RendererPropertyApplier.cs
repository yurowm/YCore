using System;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Core;

namespace Yurowm.UI {
    [ExecuteInEditMode]
    public abstract class RendererPropertyApplier : MonoBehaviour {
        
        [ContextMenu("Clear Blocks")]
        void Clear() {
            propertyBlock = new MaterialPropertyBlock();
            
            foreach (var renderer in GetComponentsInChildren<Renderer>()) 
                renderer.SetPropertyBlock(propertyBlock);
            
            Refresh();
        }

        public bool forUI = false;
        
        MaterialPropertyBlock propertyBlock;
        
        public abstract void ModifyProperty(MaterialPropertyBlock block);
        public abstract void ModifyProperty(Material material);

        void OnEnable() {
            Refresh();
        }

        void OnValidate() {
            Refresh();
        }

        void OnDidApplyAnimationProperties() {
            Refresh();
        }

        public void Refresh() {
            if (!enabled)
                return;
            
            if (forUI) {
                var modifiOriginal = !Application.isPlaying;
                
                foreach (var graphic in GetComponentsInChildren<MaskableGraphic>()) {
                    if (!modifiOriginal && graphic.material.GetInstanceID() > 0)
                        graphic.material = Instantiate(graphic.material);
                    ModifyProperty(graphic.material);
                }
            } else {
                propertyBlock ??= new MaterialPropertyBlock();

                foreach (var renderer in GetComponentsInChildren<Renderer>()) {
                    renderer.GetPropertyBlock(propertyBlock); 
                    ModifyProperty(propertyBlock);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }
}