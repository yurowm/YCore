using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.UI {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    public class RaycastTrarget : Graphic {

        protected override void OnEnable() {
            base.OnEnable();
            raycastTarget = true;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            
            var rect = rectTransform.rect;
            
            vh.AddVert(new Vector3(rect.xMin, rect.yMin, 0), default, default);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin, 0), default, default);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax, 0), default, default);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax, 0), default, default);
            
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(1, 2, 3);
        }
    }
}