using System;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Spaces;
using Yurowm.Utilities;

namespace Yurowm {
    public class LinearAnimator : BaseBehaviour, SpaceTime.ISensitiveComponent {

        public bool localTime = false;
        public bool randomizeTime = false;
        public bool unscaledTime = true;
        public bool resetTimeOnEnable = false;
        float _time = 0;
        float _timeOffset = 0;

        public bool rotZ = false;
        public float rotZampl = 0;
        public float rotZfreq = 0;
        Quaternion rotOffset;
        public float rotZphase = 0;
        public float rotZvelocity = 0;
        public float rotZoffset = 0;
        public Vector3 rotAsix = new Vector3(0, 0, 1);

        public bool sizeX = false;
        public float sizeXampl = 0;
        public float sizeXfreq = 0;
        float sizeXoffset = 1;

        public bool sizeY = false;
        public float sizeYampl = 0;
        public float sizeYfreq = 0;
        float sizeYoffset = 1;

        public bool posX = false;
        public float posXampl = 0;
        public float posXfreq = 0;
        public float posXphase = 0;
        float posXoffset = 1;
        public float posXvelocity = 0;

        public bool posY = false;
        public float posYampl = 0;
        public float posYfreq = 0;
        public float posYphase = 0;
        float posYoffset = 1;
        public float posYvelocity = 0;

        public bool alpha = false;
        public float alphaAmpl = 0;
        public float alphaFreq = 0;
        float alphaOffset = 0;
        public float alphaPhase = 0;

        Vector3 z;
        Graphic graphic;
        Color color;

        void Start() {
            if (alpha) {
                graphic = GetComponent<Graphic>();
            }
            
            if (randomizeTime) {
                if (localTime)
                    _time =  YRandom.main.Range(-1000f, 0f);
                else
                    _timeOffset =  YRandom.main.Range(-1000f, 0f);
            }
        
            Recalculate();
        }

        public void Recalculate() {
            sizeXoffset = transform.localScale.x;
            sizeYoffset = transform.localScale.y;
            rotOffset = transform.localRotation;
            posXoffset = transform.localPosition.x;
            posYoffset = transform.localPosition.y;
            if (alpha) {
                if (spriteRenderer) alphaOffset = spriteRenderer.color.a;
                else if (graphic) alphaOffset = graphic.color.a;
            }
        }

        protected override void OnAutomateAction() {
            base.OnAutomateAction();
            
            rotZ = true;
            rotZampl = YRandom.main.Range(1f, 2f);
            rotZfreq = YRandom.main.Range(1f, 6f);
            rotZphase = YRandom.main.Range(0f, Mathf.PI);
            
            sizeX = false;
            sizeXampl = YRandom.main.Range(0, 0);
            sizeXfreq = YRandom.main.Range(0, 0);
            
            sizeY = false;
            sizeYampl = YRandom.main.Range(0, 0);
            sizeYfreq = YRandom.main.Range(0, 0);
            
        }

        void OnDrawGizmosSelected() {
            if (rotZ) {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.TransformPoint(rotAsix));
            }    
        }
        
        float DeltaTime => spaceTime?.Delta ?? (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        float AbsoluteTime => spaceTime?.AbsoluteTime ?? (unscaledTime ? Time.unscaledTime : Time.time);

        void OnEnable() {
            if (resetTimeOnEnable)
                _time = 0;
        }

        void Update() {
            if (localTime)
                _time += DeltaTime;
            else 
                _time = _timeOffset + AbsoluteTime; 
            
            if (rotZ) {
                var angle = Mathf.Sin(rotZfreq * (rotZphase + _time)) * rotZampl + rotZvelocity * _time + rotZoffset;
                transform.localRotation = rotOffset * Quaternion.AngleAxis(angle, rotAsix);
            }

            if (sizeX || sizeY) {
                z = transform.localScale;

                if (sizeX)
                    z.x = sizeXoffset + Mathf.Sin(sizeXfreq * _time) * sizeXampl;
                if (sizeY)
                    z.y = sizeYoffset + Mathf.Sin(sizeYfreq * _time) * sizeYampl;

                transform.localScale = z;
            }

            if (posX || posY) {
                z = transform.localPosition;

                if (posX)
                    z.x = posXoffset + Mathf.Sin(posXphase + posXfreq * _time) * posXampl;
                if (posY)
                    z.y = posYoffset + Mathf.Sin(posYphase + posYfreq * _time) * posYampl;

                transform.localPosition = z;
            }

            if (alpha) {
                float a = (alphaOffset + Mathf.Sin(alphaFreq * (alphaPhase + _time)) * alphaAmpl);
                if (spriteRenderer) {
                    color = spriteRenderer.color;
                    color.a = a;
                    spriteRenderer.color = color;
                }
                if (graphic) {
                    color = graphic.color;
                    color.a = a;
                    graphic.color = color;
                }
            }

        }

        SpaceTime spaceTime;
        public void OnChangeTime(SpaceTime time) {
            this.spaceTime = time;
        }
    }
}