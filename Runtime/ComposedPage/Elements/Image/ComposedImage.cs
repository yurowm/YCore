using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.ComposedPages {
    public class ComposedImage : ComposedElement {
        public Image image;

        public void SetSprite(Sprite sprite) {
            image.sprite = sprite;
            image.preserveAspect = true;
            SetColor(Color.white);
        }

        public void SetHeight(float height) {
            layout.preferredHeight = height;
        }

        public void SetMaterial(Material material) {
            image.material = material;
        }

        public void SetColor(Color color) {
            image.color = image.sprite ? color : Color.clear;
        }

        public override void Rollout() {
            base.Rollout();
            SetSprite(null);
            SetColor(Color.clear);
        }
    }
}