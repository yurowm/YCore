using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.YPlanets.UI {
    [AddComponentMenu("UI/Effects/Gradient Legacy")]
    public class UIGradientLegacy : BaseMeshEffect {
        public enum Type {
            Vertical,
            Horizontal,
        }
        [SerializeField]
    
        public Type GradientType = Type.Vertical;

        [SerializeField]
        [Range(-1.5f, 1.5f)]
        public float Offset = 0f;
    
        public bool multiply = true;

        [SerializeField]
        public Color StartColor = Color.white;
        [SerializeField]
        public Color EndColor = Color.black;

        static List<UIVertex> list = new List<UIVertex>();
    
        public void Refresh() {
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive())
                return;

            list.Clear();
            vh.GetUIVertexStream(list);

            if (list.Count > 0) {
                int nCount = list.Count;
                switch (GradientType) {
                    case Type.Vertical: {
                        float fBottomY = list[0].position.y;
                        float fTopY = list[0].position.y;
                        float fYPos = 0f;

                        for (int i = nCount - 1; i >= 1; --i) {
                            fYPos = list[i].position.y;
                            if (fYPos > fTopY)
                                fTopY = fYPos;
                            else if (fYPos < fBottomY)
                                fBottomY = fYPos;
                        }

                        float fUIElementHeight = 1f / (fTopY - fBottomY);
                        for (int i = nCount - 1; i >= 0; --i) {
                            UIVertex uiVertex = list[i];
                            var color =  Color32.Lerp(EndColor, StartColor, (uiVertex.position.y - fBottomY) * fUIElementHeight - Offset);
                                
                            if (multiply) 
                                color = color.Multiply(uiVertex.color);
                            
                            uiVertex.color = color;
                            list[i] = uiVertex;
                        }
                    }
                        break;
                    case Type.Horizontal: {
                        float fLeftX = list[0].position.x;
                        float fRightX = list[0].position.x;
                        float fXPos = 0f;

                        for (int i = nCount - 1; i >= 1; --i) {
                            fXPos = list[i].position.x;
                            if (fXPos > fRightX)
                                fRightX = fXPos;
                            else if (fXPos < fLeftX)
                                fLeftX = fXPos;
                        }

                        float fUIElementWidth = 1f / (fRightX - fLeftX);
                        for (int i = nCount - 1; i >= 0; --i) {
                            UIVertex uiVertex = list[i];
                            var color =  Color32.Lerp(StartColor, EndColor, (uiVertex.position.x - fLeftX) * fUIElementWidth - Offset);
                            
                            if (multiply) 
                                color = color.Multiply(uiVertex.color);
                            
                            uiVertex.color = color;
                            list[i] = uiVertex;
                        }
                    }
                        break;
                    default:
                        break;
                }
            }
            vh.AddUIVertexTriangleStream(list);
        }

        public void SetStartColor(Color color) {
            if (StartColor == color) return;
        
            StartColor = color;
            Refresh();
        }

        public void SetEndColor(Color color) {
            if (EndColor == color) return;
        
            EndColor = color;
            Refresh();
        }
    }
}