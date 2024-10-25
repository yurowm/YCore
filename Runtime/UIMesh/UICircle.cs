using Yurowm.Extensions;

namespace UnityEngine.UI {
    public class UICircle : MaskableGraphic, ILayoutElement {

        public int segments = 64;
        public float smoothBorderSize = 1f;
        
        public override Texture mainTexture {
            get {
                if (overrideSprite == null) {
                    if (material != null && material.mainTexture != null) {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return overrideSprite.texture;
            }
        }

        Sprite m_OverrideSprite;
        public Sprite overrideSprite {
            get => m_OverrideSprite ?? sprite;
            set {
                if (m_OverrideSprite == value) return;
                m_OverrideSprite = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        Sprite m_Sprite;
        public Sprite sprite {
            get => m_Sprite;
            set {
                if (m_Sprite == value) return;
                m_Sprite = value;
                SetAllDirty();
            }
        }
        
        public float flexibleHeight => -1;
        public float flexibleWidth => -1;
        
        public int layoutPriority => 0;
        
        public float minHeight => 0;
        public float minWidth => 0;
        
        public float preferredHeight => 0;
        public float preferredWidth => 0;

        public virtual void CalculateLayoutInputHorizontal() {}

        public virtual void CalculateLayoutInputVertical() {}

        protected override void OnPopulateMesh(VertexHelper toFill) {
            GenerateMesh(toFill);
        }

        static readonly Color borderColor = new Color(1, 1, 1, 0);
        
        void GenerateMesh(VertexHelper vh) {
            vh.Clear();
            
            int segments = Mathf.Max(3, this.segments);
            float smoothBorderSize = Mathf.Max(0, this.smoothBorderSize);
            
            Rect rect = GetPixelAdjustedRect();
            
            Vector2 center = rect.center;
            Vector2 uvCenter = Vector2.one / 2;
            
            float radius = Mathf.Min(rect.width, rect.height) / 2;
            float uvRadius = .5f;
            
            center += (rectTransform.pivot - Vector2.one / 2) * (rect.size - radius * 2 * Vector2.one);
            vh.AddVert(center.To3D(), color, uvCenter);

            for (int i = 0; i < segments; i++) {
                Vector2 offset = Vector2.right.Rotate((360f * i) / segments);
                vh.AddVert((center + offset * (radius - smoothBorderSize)).To3D(), color, uvCenter + offset * (uvRadius * (radius - smoothBorderSize) / radius)); // i + 1
            }
            
            for (int i = 0; i < segments; i++)
                vh.AddTriangle(0, i, i + 1);
            vh.AddTriangle(0, segments, 1);

            if (smoothBorderSize == 0) return;
            
            
            for (int i = 0; i < segments; i++) {
                Vector2 offset = Vector2.right.Rotate((360f * i) / segments);
                vh.AddVert((center + offset * radius).To3D(), borderColor, uvCenter + offset * uvRadius);
            }    
            
            for (int i = 1; i < segments; i++) {
                vh.AddTriangle(i, i + 1, segments + i);
                vh.AddTriangle(i + 1, segments + i, segments + i + 1);
            }
            
            vh.AddTriangle(segments, 1, segments + segments);
            vh.AddTriangle(segments + segments, 1, segments + 1);
        }
    }
}
