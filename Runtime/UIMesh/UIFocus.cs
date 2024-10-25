using System;

namespace UnityEngine.UI {
    public class UIFocus : MaskableGraphic, ILayoutElement {

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

        [NonSerialized]
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
            if (sprite == null) {
                base.OnPopulateMesh(toFill);
                return;
            }

            GenerateMesh(toFill);
        }

        #region Properties
        
        bool hasBorder {
            get {
                if (sprite != null) {
                    Vector4 v = sprite.border;
                    return v.sqrMagnitude > 0f;
                }
                return false;
            }
        }
        
        float multipliedPixelsPerUnit => pixelsPerUnit * m_PixelsPerUnitMultiplier;

        public float pixelsPerUnit {
            get {
                float spritePixelsPerUnit = 100;
                if (sprite)
                    spritePixelsPerUnit = sprite.pixelsPerUnit;

                if (canvas)
                    m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;

                return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
            }
        }
        
        [SerializeField]
        float m_PixelsPerUnitMultiplier = 1.0f;
        [SerializeField]
        float extraBorder = 1.0f;
        [SerializeField]
        float extraBorderUV = 0.01f;
        [SerializeField]
        float extraUVOffset = 0;

        
        float m_CachedReferencePixelsPerUnit = 100;
        
        #endregion
        
        static readonly Vector2[] s_VertScratch = new Vector2[4];
        static readonly Vector2[] s_UVScratch = new Vector2[4];
        
        void GenerateMesh(VertexHelper vh) {
            vh.Clear();
            
            if (!sprite || !hasBorder) return;

            Vector4 outer, inner, padding, border;

            outer = Sprites.DataUtility.GetOuterUV(sprite);
            inner = Sprites.DataUtility.GetInnerUV(sprite);
            padding = Sprites.DataUtility.GetPadding(sprite);
            border = sprite.border;
            
            float ppu = pixelsPerUnit;
            Rect rect = GetPixelAdjustedRect();

            Vector4 adjustedBorders = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);
            padding = padding / multipliedPixelsPerUnit;

            s_VertScratch[0] = new Vector2(padding.x, padding.y);
            s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

            s_VertScratch[1].x = adjustedBorders.x;
            s_VertScratch[1].y = adjustedBorders.y;

            s_VertScratch[2].x = rect.width - adjustedBorders.z;
            s_VertScratch[2].y = rect.height - adjustedBorders.w;

            for (int i = 0; i < 4; ++i) {
                s_VertScratch[i].x += rect.x;
                s_VertScratch[i].y += rect.y;
            }

            s_UVScratch[0] = new Vector2(outer.x, outer.y);
            s_UVScratch[1] = new Vector2(inner.x, inner.y);
            s_UVScratch[2] = new Vector2(inner.z, inner.w);
            s_UVScratch[3] = new Vector2(outer.z, outer.w);


            for (int x = 0; x < 3; ++x) {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y) {
                    int y2 = y + 1;

                    AddQuad(vh,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        color,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                }
            }
            
                        
            Rect outerRect = new Rect(rect);
            outerRect.x -= extraBorder * ppu;
            outerRect.y -= extraBorder * ppu;
            outerRect.width += extraBorder * ppu * 2;
            outerRect.height += extraBorder * ppu * 2;

            // Left
            AddQuad(vh,
                new Vector2(outerRect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax),
                color,
                new Vector2(outer.x - extraBorderUV, outer.y),
                new Vector2(outer.x - extraUVOffset, outer.w));
            
            // Right
            AddQuad(vh,
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(outerRect.xMax, rect.yMax),
                color,
                new Vector2(outer.z + extraUVOffset, outer.y),
                new Vector2(outer.z + extraBorderUV, outer.w));
            
            // Bottom
            AddQuad(vh,
                new Vector2(rect.xMin, outerRect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                color,
                new Vector2(outer.x, outer.y - extraBorderUV),
                new Vector2(outer.z, outer.y - extraUVOffset));
            
            // Top
            AddQuad(vh,
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, outerRect.yMax),
                color,
                new Vector2(outer.x, outer.w + extraUVOffset),
                new Vector2(outer.z, outer.w + extraBorderUV));
            
            // Bottom-Left
            AddQuad(vh,
                new Vector2(outerRect.xMin, outerRect.yMin),
                new Vector2(rect.xMin, rect.yMin),
                color,
                new Vector2(outer.x - extraBorderUV, outer.y - extraBorderUV),
                new Vector2(outer.x - extraUVOffset, outer.y - extraUVOffset));
            
            // Bottom-Right
            AddQuad(vh,
                new Vector2(rect.xMax, outerRect.yMin),
                new Vector2(outerRect.xMax, rect.yMin),
                color,
                new Vector2(outer.z + extraUVOffset, outer.y - extraBorderUV),
                
                new Vector2(outer.z + extraBorderUV, outer.y - extraUVOffset));
            
            // Top-Left
            AddQuad(vh,
                new Vector2(outerRect.xMin, rect.yMax),
                new Vector2(rect.xMin, outerRect.yMax),
                color,
                new Vector2(outer.x - extraBorderUV, outer.w + extraUVOffset),
                new Vector2(outer.x - extraUVOffset, outer.w + extraBorderUV));
            
            // Top-Right
            AddQuad(vh,
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(outerRect.xMax, outerRect.yMax),
                color,
                new Vector2(outer.z + extraUVOffset, outer.w + extraUVOffset),
                new Vector2(outer.z + extraBorderUV, outer.w + extraBorderUV));
            
        }
        
        static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax) {
            int startIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
            vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
        
        Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect) {
            Rect originalRect = rectTransform.rect;

            for (int axis = 0; axis <= 1; axis++) {
                float borderScaleRatio;

                if (originalRect.size[axis] != 0) {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0) {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }
    }
}
