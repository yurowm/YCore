using UnityEngine;
using Yurowm.Spaces;
using Yurowm.Utilities;

namespace Yurowm {
    public class RandomRotation : BaseBehaviour, SpaceTime.ISensitiveComponent {
        
        public FloatRange speedRange = 90;
        public bool randomizeOnEnable = false;
        
        float speed;

        Vector3 asix;
        
        void Awake() {
            Randomize();
        }

        void OnEnable() {
            if (randomizeOnEnable)
                Randomize();
        }

        public void Randomize() {
            asix = Quaternion.Euler(
                       YRandom.main.Range(0, 360),
                       YRandom.main.Range(0, 360),
                       YRandom.main.Range(0, 360)) * Vector3.right;
            speed = YRandom.main.Range(speedRange);
        }
        
        void Update() {
            transform.rotation *= Quaternion.AngleAxis(speed * (time?.Delta ?? Time.deltaTime), asix);
        }

        SpaceTime time;
        
        public void OnChangeTime(SpaceTime time) {
            this.time = time;
        }
    }
}