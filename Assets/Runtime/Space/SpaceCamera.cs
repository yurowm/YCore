using System;
using System.Collections;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;
#if FMOD
using FMODUnity;
#endif

namespace Yurowm.Spaces {
    public class SpaceCamera : SpacePhysicalItem {
        public SpacePhysicalItem target = null;
        public Vector2 offset = new Vector2();
        public Vector2 forward = new Vector2();
        public float backlash = 0.2f;
        public float smooth = 3f;
        public Action<float> onZoom = delegate { };
        public Action<Vector2> onMove = delegate { };

        public Camera camera;
        
        public const int UPDATE_ORDER = -1000;
            
        public override Vector2 position {
            set {
                if (position == value) return;
                base.position = value;
                camera.transform.localPosition = value;
                onMove(value);
            }
        }
        
        
        public Vector2 viewSize => new Vector2(viewSizeHorizontal, viewSizeVertical);

        /// <summary>
        /// Раньше было просто viewSize. Сейчас это вектор
        /// </summary>
        public float viewSizeVertical {
            get {
                if (camera != null) 
                    return camera.orthographicSize;
                return 1;
            }
            set {
                if (!camera || camera.orthographicSize == value) return;
                camera.orthographicSize = value;
                onZoom(value);
            }
        }

        public float viewSizeHorizontal {
            get => viewSizeVertical * camera?.aspect ?? 1;
            set => viewSizeVertical = value / (camera?.aspect ?? 1);
        }

        public SpaceCamera() {
            bodyName = "SpaceCamera";
        }

        public override SpaceObject EmitBody() {
            var result = base.EmitBody();
            camera = result.GetComponent<Camera>();
            return result;
        }

        public override void OnAddToSpace(Space space) {
            base.OnAddToSpace(space);
            Update().Run(space.coroutine, order: UPDATE_ORDER);
            
            // #if FMOD
            // if (!RuntimeManager.HasListener[0])
            //     RuntimeManager.AddListener(0);
            // #endif
        }

        public override void OnRemoveFromSpace(Space space) {
            base.OnRemoveFromSpace(space);
            
            // #if FMOD
            // if (RuntimeManager.HasListener[0])
            //     RuntimeManager.RemoveListener(0);
            // #endif
        }

        public bool Zoom(float zoom) {
            if (!enabled || camera.orthographicSize == zoom) return false;
            
            camera.orthographicSize = zoom;
            onZoom(zoom);
            
            return true;
        }

        public override void OnEnable() {
            base.OnEnable();
            SetUICamera.Set(camera);
        }

        public override void OnDisable() {
            base.OnDisable();
            SetUICamera.Remove(camera);
        }

        public IEnumerator Update() {
            while (IsAlive()) {
                yield return null;
                
                #if FMOD
                RuntimeManager.SetListenerLocation(body.gameObject);
                #endif
                
                if (!enabled || !target || !camera) 
                    continue;
                
                if (smooth <= 0) {
                    var lastPosition = position;
                    
                    position = target.position - GetOffset();
                    
                    if (!forward.IsEmpty())
                        position -= forward.Rotate(target.direction);
                    
                    if (lastPosition != position) 
                        onMove(position);
                
                } else {
                    Vector2 delta = (position + GetOffset()) - target.position;
                    
                    if (!forward.IsEmpty())
                        delta += forward.Rotate(target.direction);
                
                    if (delta.MagnitudeIsLessThan(backlash)) continue;
                
                    delta -= delta.FastNormalized() * backlash;
                    delta *= time.Delta * smooth;
                
                    if (delta.IsEmpty()) continue;
                
                    position -= delta;
                    onMove(position);
                }
            }
        }
        
        public virtual Vector2 GetOffset() {
            return offset;
        }

        public Discretor GetDiscretor() {
            return new Discretor() {
                camera = this
            };
        }

        public Texture2D TakePicture(int width, int height) {
            RenderTexture render = new RenderTexture(width, height, 24);

            camera.targetTexture = render;
            camera.Render();

            RenderTexture.active = render;

            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(render);

            return result;
        }

        #region Sides
        public float Bottom {
            get => position.y - viewSizeVertical;
            set => position = new Vector2(position.x, value + viewSizeVertical);
        }

        public float Top {
            get => position.y + viewSizeVertical;
            set => position = new Vector2(position.x, value - viewSizeVertical);
        }

        public float Right {
            get => position.x + viewSizeHorizontal;
            set => position = new Vector2(value - viewSizeHorizontal, position.y);
        }

        public float Left {
            get => position.x - viewSizeHorizontal;
            set => position = new Vector2(value + viewSizeHorizontal, position.y);
        }
        #endregion

        public class Discretor {
            internal SpaceCamera camera;
            float period;
            internal Discretor() {
                period = 0.05f;
            }

            float scaledPeriod => Mathf.Ceil(camera.viewSizeVertical * period);

            public float GetValue(float source) {
                return scaledPeriod / Mathf.Round(scaledPeriod / source);
            }
        }
    }
}