using TMPro;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.ComposedPages {
    public class ComposedText : ComposedElement {
        public TextMeshProUGUI textLabel;
        string text = "";

        FloatRange height = null;
        public void SetText(string text) {
            this.text = text;

            if (!text.IsNullOrEmpty()) {
                textLabel.text = text;
                if (height != null)
                    SetHeight(height);
                else
                    layout.preferredHeight = -1;
            }
            
            gameObject.SetActive(!text.IsNullOrEmpty());
        }

        public void SetHeight(FloatRange range) {
            height = range;
            Vector2 size = new Vector2(layout.transform.parent.rect().rect.width, 10000);
            size = textLabel.GetPreferredValues(textLabel.text, size.x, size.y);
            layout.preferredHeight = height.Clamp(size.y);
        }
        
        public void SetFlexibleHeight(float height) {
            layout.flexibleHeight = height;
        }

        public void SetAlignment(TextAlignmentOptions alignment) {
            textLabel.alignment = alignment;
            textLabel.wordWrappingRatios = .9f;
        }

        public override bool IsVisible() {
            return !text.IsNullOrEmpty();
        }

        #region IReserved

        public override void Rollout() {
            base.Rollout();
            height = null;
            layout.flexibleHeight = -1;
            textLabel.alignment = TextAlignmentOptions.Left;
            SetText("");
        }

        #endregion
    }
}