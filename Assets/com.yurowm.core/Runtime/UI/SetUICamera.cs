using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    [RequireComponent(typeof(Canvas))]
    public class SetUICamera : Behaviour {
        
        public float planeDistance = 0;

        [OnLaunch(INITIALIZATION_ORDER + 1)]
        static void OnLaunch() {
            if (!defaultCamera) {
                defaultCamera = AssetManager.Create<Camera>(); 
                if (defaultCamera) {
                    defaultCamera.name = "DefaultUICamera";
                    Set(defaultCamera);
                }
            }
        }

        public override void Initialize() {
            base.Initialize();
            if (currentCamera)
                Set(currentCamera);
        }

        static Camera defaultCamera;
        static Camera currentCamera;
        
        static List<Camera> last = new();

        public static void SetDefault(Camera cam) {
            defaultCamera = cam;
            Set(currentCamera);
        }
        
        public static Camera GetCurrent() {
            return currentCamera ? currentCamera : defaultCamera;
        }
        
        public static void Set(Camera cam) {
            currentCamera = cam;
            last.Remove(cam);
            last.Add(cam);
            GetAll<SetUICamera>()
                .ForEach(s => {
                    if (s.SetupComponent(out Canvas canvas)) {
                        canvas.worldCamera = cam ?? defaultCamera;
                        canvas.planeDistance = s.planeDistance;
                    }
                });
            defaultCamera?.gameObject.SetActive(cam == null || cam == defaultCamera);
        }
        
        public static void Remove(Camera cam) {
            if (cam == null) return;
            
            last.Remove(cam);
            
            Set(last.LastOrDefault() ?? defaultCamera);
        }
    }
}