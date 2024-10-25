using System;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm {
    public class SetSortingLayer : BaseBehaviour {

        public SortingLayerAndOrder sorting;
	
        void Start () {
            Refresh ();
        }

        [ContextMenu("Refresh")]
        public void Refresh() {
            if (renderer) {
                renderer.sortingLayerID = sorting.layerID;
                renderer.sortingOrder = sorting.order;
            }
        }

        void OnValidate() {
            Refresh();    
        }
    }
}