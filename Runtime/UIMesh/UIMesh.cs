using System;

namespace UnityEngine.UI {
    public class UIMesh : MaskableGraphic, ILayoutElement {
    
        public Mesh mesh;
        public bool flipHorizontal = false;
        public bool flipVertical = false;
        public bool preserveAspect = true;

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

        public float preferredHeight {
            get {
                if (mesh != null)
                    return mesh.bounds.size.y;
                return 0;
            }
        }

        public float preferredWidth {
            get {
                if (mesh != null)
                    return mesh.bounds.size.x;
                return 0;
            }
        }

        public virtual void CalculateLayoutInputHorizontal() {}

        public virtual void CalculateLayoutInputVertical() {}

        protected override void OnPopulateMesh(VertexHelper toFill) {
            if (mesh == null) {
                base.OnPopulateMesh(toFill);
                return;
            }

            GenerateMesh(toFill);
        }

        void GenerateMesh(VertexHelper vh) {
            var color32 = color;
            vh.Clear();

            if (mesh == null || !mesh.isReadable || mesh.vertexCount == 0)
                return;

            var min = mesh.bounds.min;
            var max = mesh.bounds.max;
            var size = mesh.bounds.size;

            if (size.x == 0 || size.y == 0)
                return;

            var rsize = rectTransform.rect.size;
            var roffset = Vector2.zero;

            if (preserveAspect) {
                float mRatio = size.x / size.y; 
                float rRatio = rsize.x / rsize.y;

                Vector2 fixedSize = rsize;
                if (rRatio > mRatio)
                    fixedSize.x *= mRatio / rRatio;
                else
                    fixedSize.y *= rRatio / mRatio;

                roffset = rsize - fixedSize;
                roffset.x = (roffset.x - rsize.x) * rectTransform.pivot.x;
                roffset.y = (roffset.y - rsize.y) * rectTransform.pivot.y;
                rsize = fixedSize;
            }


            Vector3 vertex;


            for (int i = 0; i < mesh.vertexCount; i++) {
                vertex = (mesh.vertices[i] - min);
                vertex.x /= size.x;
                vertex.y /= size.y;
                vertex.z /= (size.x + size.y) / 2;

                if (flipHorizontal) vertex.x = 1f - vertex.x;
                if (flipVertical) vertex.y = 1f - vertex.y;

                vertex.x = roffset.x + vertex.x * rsize.x;
                vertex.y = roffset.y + vertex.y * rsize.y;
                vertex.z *= (rsize.x + rsize.y) / 2;

                vh.AddVert(vertex, color32, mesh.uv[i], Vector2.zero, mesh.normals[i], mesh.tangents[i]);
            }

            for (int i = 0; i < mesh.triangles.Length; i += 3) {
                vh.AddTriangle(mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);
            }
        }
    }
}
