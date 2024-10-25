using System;
using System.Reflection;
using UnityEngine.UI;

namespace Yurowm.YPlanets.UI {
    // Work In Progress
    public class UIInstancer : MaskableGraphic {
        public Graphic source;
        
        static MethodInfo onPopulateMeshMethod;
        
        static UIInstancer() {
            onPopulateMeshMethod = typeof(Graphic)
                .GetMethod("OnPopulateMesh", 
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    Type.DefaultBinder,
                    new[] { typeof (VertexHelper) },
                    null);
        }

        readonly object[] arguments = new object[1];
        
        protected override void OnPopulateMesh(VertexHelper vh) {
            if (source == null) return;
            
            arguments[0] = vh;
            
            onPopulateMeshMethod.Invoke(source, arguments);
        }
    }
}