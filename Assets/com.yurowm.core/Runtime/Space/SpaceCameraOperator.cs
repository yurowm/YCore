using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Controls;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Jobs;
using Yurowm.Utilities;

namespace Yurowm.Spaces {
    public class CameraOperator : SpacePhysicalItem, ISelfUpdate, ITouchControlProvider {
        public TouchControl controls;
        CameraOperatorLimiter _limiter;
        public CameraOperatorLimiter limiter {
            get => _limiter;
            set {
                _limiter = value;
                if (_limiter != null)
                    _limiter.cameraOperator = this;
            }
        }
        public SpaceCamera camera = null;

        public bool allowToRotate = true;
        public bool allowToZoom = true;
        public bool allowToMove = true;
        public bool allowToControl = true;
        
        
        public override void OnAddToSpace(Space space) {
            base.OnAddToSpace(space);
            limiter?.Initialize(space);
            context.Catch<SpaceCamera>(CatchCamera);
        }

        bool CatchCamera(SpaceCamera camera) {
            this.camera = camera;
            controls = new TouchControl() {
                controlCamera = camera.camera.GetComponent<Camera>()
            };
            context.SetArgument(controls);
            controls.ControlRoutine(this).Forget();
            if (limiter != null) {
                limiter.SetupCamera(camera);
                Zoom(camera.viewSizeVertical);
                CropPosition();
                camera.position = position;
            }
            return true;
        }

        bool crop;
        
        public void Crop() {
            if (allowToMove) {
                CropPosition();
                camera.position = position;
            }
            crop = false;
        }
        
        public void UpdateFrame(Updater updater) {
            DebugPanel.Log("Camera Velocity", "Space", velocity);
            if (!drag) {
                if (limiter != null) {
                    Vector2 offset = limiter.GetOffset(position, GetViewSize());
                    if (offset != Vector2.zero)
                        velocity = Vector2.MoveTowards(velocity, -offset.normalized * (4f -  offset.magnitude), 10 * Time.deltaTime);

                    float zoom = limiter.GetZoomTarget(camera.viewSizeVertical);
                    if (camera.viewSizeVertical != zoom) {
                        Zoom(Mathf.MoveTowards(camera.viewSizeVertical, zoom, Mathf.Abs(camera.viewSizeVertical - zoom) * 4 * Time.deltaTime));
                        crop = true;
                    }
                }
                if (velocity != Vector2.zero) {
                    velocity = velocity.normalized * Mathf.Max(0, velocity.magnitude - Time.deltaTime * 3); 
                    position += velocity * space.time.Delta;
                    crop = true;
                }
                
            }
            if (crop) 
                Crop();
        }

        Vector2 GetViewSize() {
            return new Vector2(camera.viewSizeHorizontal * 2, camera.viewSizeVertical * 2);
        }

        bool drag = false;
        float beginCameraSize = 1;
        float beginZoom = 1;
        
        public void Control(List<TouchStory> touches) {
            if (touches.Count == 1) {
                var touch = touches[0];
                if (touch.IsBegan && !touch.IsOverUI) {
                    velocity = Vector2.zero;
                    drag = true;
                }

                if (drag) {
                    if (allowToMove) {
                        position -= touch.deltaWorldPosition;
                        touch.currentWorldPosition -= touch.deltaWorldPosition;
                    }

                    crop = true;

                    if (touch.IsComplete) {
                        drag = false;
                        if (allowToMove) 
                            velocity = -touch.deltaWorldPosition * .5f / Time.deltaTime;
                        if (touch.DeltaTime < 0.2f && 
                            !touch.IsOverUI &&
                            touch.completeDeltaScreenPosition.Value.MagnitudeIsLessThan(Screen.height * .05f))
                            space.clickables.Click(touch.currentWorldPosition);
                    }
                }
            }

            if (touches.Count >= 2) {
                var touchA = touches[0];
                var touchB = touches[1];

                if ((touchA.IsBegan && !touchA.IsOverUI) || (touchB.IsBegan && !touchB.IsOverUI)) {
                    velocity = Vector2.zero;
                    beginZoom = (touchB.currentScreenPosition - touchA.currentScreenPosition).magnitude;
                    beginCameraSize = controls.controlCamera.orthographicSize;
                    drag = true;
                } else if (drag) {
                    float deltaRotation = 0;
                    float zoom = 0;
                    float deltaZoom = 0;
                    if (allowToRotate) {
                        deltaRotation = (touchB.currentScreenPosition - touchB.deltaScreenPosition 
                            - touchA.currentScreenPosition + touchA.deltaScreenPosition).Angle()
                            - (touchB.currentScreenPosition - touchA.currentScreenPosition).Angle();
                    }
                    if (allowToZoom) {
                        zoom = beginZoom / (touchB.currentScreenPosition - touchA.currentScreenPosition).magnitude;
                        deltaZoom = (touchB.currentScreenPosition - touchB.deltaScreenPosition 
                            - touchA.currentScreenPosition + touchA.deltaScreenPosition).magnitude
                            / (touchB.currentScreenPosition - touchA.currentScreenPosition).magnitude;
                    }

                    camera.direction += deltaRotation;
                    controls.controlCamera.transform.Rotate(0, 0, deltaRotation);

                    if (allowToZoom && Zoom(beginCameraSize * zoom))
                        DebugPanel.Log("Scene Zoom", "UI", zoom * beginCameraSize);

                    if (allowToMove) {
                        position -= (touchA.deltaWorldPosition + touchB.deltaWorldPosition) / 2;
                        position = (touchA.currentWorldPosition + touchB.currentWorldPosition) / 2 
                            + (position - (touchA.currentWorldPosition + touchB.currentWorldPosition) / 2).Rotate(deltaRotation) * deltaZoom;
                        
                        CropPosition();

                        controls.controlCamera.transform.localPosition = position;
                    }

                    touchA.currentWorldPosition = controls.ScreenToWorldPosition(touchA.currentScreenPosition);
                    touchB.currentWorldPosition = controls.ScreenToWorldPosition(touchB.currentScreenPosition);

                    if (touchA.IsComplete && touchB.IsComplete) 
                        drag = false;
                }
            }
        }

        public void OnReleaseControl() {
            drag = false;
            NormalizeCamera().Forget();
        }

        public void ScrollControl(float scroll) {
            if (allowToZoom) {
                Zoom(camera.viewSizeVertical + scroll);
                crop = true;
                DebugPanel.Log("Scene ZoomH", "UI", camera.viewSizeVertical);
                DebugPanel.Log("Scene ZoomW", "UI", camera.viewSizeHorizontal);
            }
        }

        bool Zoom(float zoom) {
            return camera.Zoom(limiter?.CropZoom(zoom) ?? zoom);
        }

        async UniTask NormalizeCamera() {
            //float target = camera.viewSize;
            //if (limiter != null)
            //    target = limiter.CropZoom(target);

            //float Zspeed = Mathf.Abs(camera.viewSize - target);
            float Rspeed = Mathf.Abs(Mathf.DeltaAngle(camera.direction, 0));
            const float duration = .25f;

            while ((camera.direction != 0 || !allowToRotate) && !drag) {
                //camera.Zoom(Mathf.MoveTowards(camera.viewSize, target, Zspeed * Time.deltaTime / duration));
                if (allowToRotate)
                    camera.direction = Mathf.MoveTowardsAngle(camera.direction, 0, Rspeed * Time.deltaTime / duration);
                await UniTask.Yield();
            }
        }

        public bool IsAliveControls() {
            return true;
        }

        public bool IsControl() {
            return enabled && allowToControl;
        }

        void CropPosition() {
            if (limiter == null) return;

            if (!limiter.CropPosition(position, out var cropped, GetViewSize(), 2)) return;
            
            if (position.x > cropped.x && velocity.x > 0) velocity *= Vector2.up;
            if (position.x < cropped.x && velocity.x < 0) velocity *= Vector2.up;
            if (position.y > cropped.y && velocity.y > 0) velocity *= Vector2.right;
            if (position.y < cropped.y && velocity.y < 0) velocity *= Vector2.right;
            
            position = cropped;
        }

        #region Show Position

        public async UniTask ShowPositionLogic(Vector2 position, float zoom = -1, float duration = .25f, EasingFunctions.Easing easing = EasingFunctions.Easing.InOutCubic) {
            velocity = Vector2.zero;
            
            duration = duration.ClampMin(1f / 100000);

            var zoomCurrent = camera.viewSizeVertical;
            
            var zoomTarget = zoom;
            if (zoomTarget == -1) zoomTarget = camera.viewSizeVertical;
            if (limiter != null) zoomTarget = limiter.CropZoom(zoomTarget);

            var positionStart = this.position;
            
            for (var t = 0f; t < 1; t += time.Delta / duration) {
                var eT = t.Ease(easing);
                camera.Zoom(Mathf.Lerp(zoomCurrent, zoomTarget, eT));
                this.position = Vector2.Lerp(positionStart, position, eT);
                await UniTask.Yield();
            }
            
            camera.Zoom(zoomTarget);
            this.position = position;
        }
        #endregion
    }

    public abstract class CameraOperatorLimiter {        
        public CameraOperator cameraOperator = null;
        public SpaceCamera camera {
            get {
                if (cameraOperator != null)
                    return cameraOperator.camera;
                return null;
            }
        }

        public virtual void Initialize(Space space) { }

        public abstract bool CropPosition(Vector2 position, out Vector2 cropped, Vector2 viewSize, float allowedOffset = 0);
        public abstract Vector2 GetOffset(Vector2 position, Vector2 viewSize);

        public abstract float CropZoom(float zoom);
        public abstract float GetZoomTarget(float zoom);

        public abstract void SetupCamera(SpaceCamera camera);
    }
}